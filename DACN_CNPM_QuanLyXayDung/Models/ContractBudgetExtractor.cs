using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace DACN_CNPM_QuanLyXayDung.Models;

public static class ContractBudgetExtractor
{
    public static async Task<decimal?> TryExtractProjectBudgetAsync(IFormFile contractFile, CancellationToken cancellationToken = default)
    {
        return await ExtractMoneyWithPatternAsync(contractFile, @"tổng\s*chi\s*phí\s*dự\s*án\s*[^0-9]{0,30}([0-9][0-9\.\,]*)", cancellationToken);
    }

    public static async Task<decimal?> TryExtractStageBudgetAsync(IFormFile contractFile, string stageName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stageName)) return null;
        var escapedName = Regex.Escape(stageName);
        return await ExtractMoneyWithPatternAsync(contractFile, @"chi\s*phí\s*giai\s*đoạn\s*" + escapedName + @"\s*[^0-9]{0,30}([0-9][0-9\.\,]*)", cancellationToken);
    }

    public static async Task<decimal?> TryExtractMaterialRequestTotalAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        return await ExtractMoneyWithPatternAsync(file, @"tổng\s*chi\s*phí\s*[^0-9]{0,30}([0-9][0-9\.\,]*)", cancellationToken);
    }

    public static decimal? TryExtractMaterialRequestTotal(byte[] fileBytes)
    {
        return ExtractMoneyWithPattern(fileBytes, @"tổng\s*chi\s*phí\s*[^0-9]{0,30}([0-9][0-9\.\,]*)");
    }

    private static decimal? ExtractMoneyWithPattern(byte[] fileBytes, string pattern)
    {
        if (fileBytes is null || fileBytes.Length == 0)
        {
            return null;
        }

        using var stream = new MemoryStream(fileBytes);
        using var pdf = PdfDocument.Open(stream);
        var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success) return null;

        var numberText = match.Groups[1].Value;
        if (TryParseMoney(numberText, out var money) && money > 0)
        {
            return money;
        }

        return null;
    }

    private static async Task<decimal?> ExtractMoneyWithPatternAsync(IFormFile contractFile, string pattern, CancellationToken cancellationToken = default)
    {
        if (contractFile is null || contractFile.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(contractFile.FileName)?.ToLowerInvariant();
        if (extension != ".pdf")
        {
            return null;
        }

        await using var stream = contractFile.OpenReadStream();

        using var pdf = PdfDocument.Open(stream);
        var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success) return null;

        var numberText = match.Groups[1].Value;
        if (TryParseMoney(numberText, out var money) && money > 0)
        {
            return money;
        }

        return null;
    }

    private static bool TryParseMoney(string raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        // Keep digits + separators only.
        var s = Regex.Replace(raw, @"[^\d\.,]", string.Empty);
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        var lastDot = s.LastIndexOf('.');
        var lastComma = s.LastIndexOf(',');

        char? decimalSep = null;
        char? thousandSep = null;

        if (lastDot >= 0 && lastComma >= 0)
        {
            if (lastDot > lastComma)
            {
                decimalSep = '.';
                thousandSep = ',';
            }
            else
            {
                decimalSep = ',';
                thousandSep = '.';
            }
        }
        else if (lastDot >= 0)
        {
            // If '.' appears multiple times, it's usually thousand separators in VN.
            var dotCount = s.Count(c => c == '.');
            if (dotCount > 1)
            {
                thousandSep = '.';
            }
            else
            {
                decimalSep = '.';
            }
        }
        else if (lastComma >= 0)
        {
            var commaCount = s.Count(c => c == ',');
            if (commaCount > 1)
            {
                thousandSep = ',';
            }
            else
            {
                decimalSep = ',';
            }
        }

        if (thousandSep is not null)
        {
            s = s.Replace(thousandSep.Value.ToString(), string.Empty);
        }

        if (decimalSep is not null && decimalSep.Value != '.')
        {
            s = s.Replace(decimalSep.Value.ToString(), ".");
        }

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public static async Task<List<ExtractedStageDto>> ExtractStagesAsync(IFormFile contractFile, CancellationToken cancellationToken = default)
    {
        var stages = new List<ExtractedStageDto>();

        if (contractFile is null || contractFile.Length == 0)
        {
            return stages;
        }

        var extension = Path.GetExtension(contractFile.FileName)?.ToLowerInvariant();
        if (extension != ".pdf")
        {
            return stages;
        }

        await using var stream = contractFile.OpenReadStream();

        using var pdf = PdfDocument.Open(stream);
        var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        if (string.IsNullOrWhiteSpace(text))
        {
            return stages;
        }

        var projectIdMatch = Regex.Match(text, @"Mã\s*dự\s*án\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        int? projectId = null;
        if (projectIdMatch.Success && int.TryParse(projectIdMatch.Groups[1].Value, out var pid))
        {
            projectId = pid;
        }

        // Split text by "Tên giai đoạn:" to get each stage block
        // Refined regex to stop at known headers or newlines
        string pattern = @"Tên\s*giai\s*đoạn\s*:\s*([^\r\n\|]+?)(?=\s*(?:Mã\s*nhân\s*viên|Chi\s*phí|Bắt\s*đầu|Kết\s*thúc)|[\r\n|]|$)";
        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);

        // We will process the text block from the start of current match to the start of the next one
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var nextIndex = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var block = text.Substring(match.Index, nextIndex - match.Index);

            var stage = new ExtractedStageDto { ProjectId = projectId };

            stage.StageName = match.Groups[1].Value.Trim();

            var userIdMatch = Regex.Match(block, @"Mã\s*nhân\s*viên\s*:\s*(\d+)", RegexOptions.IgnoreCase);
            if (userIdMatch.Success && int.TryParse(userIdMatch.Groups[1].Value, out var uid))
            {
                stage.AssignedUserId = uid;
            }

            var budgetMatch = Regex.Match(block, @"Chi\s*phí\s*(?:giai\s*đoạn)?\s*:\s*([0-9][0-9\.\,]*)", RegexOptions.IgnoreCase);
            if (budgetMatch.Success)
            {
                if (TryParseMoney(budgetMatch.Groups[1].Value, out var money))
                {
                    stage.Budget = money;
                }
            }

            var startMatch = Regex.Match(block, @"Bắt\s*đầu\s*:\s*(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase);
            if (startMatch.Success && DateOnly.TryParseExact(startMatch.Groups[1].Value, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var start))
            {
                stage.StartDate = start;
            }

            var endMatch = Regex.Match(block, @"Kết\s*thúc\s*:\s*(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase);
            if (endMatch.Success && DateOnly.TryParseExact(endMatch.Groups[1].Value, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var end))
            {
                stage.EndDate = end;
            }

            if (!string.IsNullOrWhiteSpace(stage.StageName))
            {
                stages.Add(stage);
            }
        }


        return stages;
    }
}

