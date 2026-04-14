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
        return await ExtractMoneyWithPatternAsync(contractFile, @"(?:tổng|tổng)\s*(?:chi|kinh)\s*phí\s*(?:dự\s*án)?\s*[:\-]?\s*[^0-9]{0,20}([0-9][0-9\.\,]*)", cancellationToken);
    }

    public static async Task<ExtractedStageDto?> TryExtractStageDetailsAsync(IFormFile contractFile, string stageName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stageName)) return null;
        var allStages = await ExtractStagesAsync(contractFile, cancellationToken);
        // Khớp tên giai đoạn không phân biệt hoa thường và loại bỏ khoảng trắng dư thừa
        return allStages.FirstOrDefault(s => string.Equals(s.StageName.Trim(), stageName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<decimal?> TryExtractStageBudgetAsync(IFormFile contractFile, string stageName, CancellationToken cancellationToken = default)
    {
        var details = await TryExtractStageDetailsAsync(contractFile, stageName, cancellationToken);
        return details?.Budget;
    }

    public static async Task<decimal?> TryExtractMaterialRequestTotalAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        return await ExtractMoneyWithPatternAsync(file, @"(?:tổng|tổng)\s*(?:chi|kinh)\s*phí\s*[:\-]?\s*[^0-9]{0,20}([0-9][0-9\.\,]*)", cancellationToken);
    }

    public static decimal? TryExtractMaterialRequestTotal(byte[] fileBytes)
    {
        return ExtractMoneyWithPattern(fileBytes, @"(?:tổng|tổng)\s*(?:chi|kinh)\s*phí\s*[:\-]?\s*[^0-9]{0,20}([0-9][0-9\.\,]*)");
    }

    private static decimal? ExtractMoneyWithPattern(byte[] fileBytes, string pattern)
    {
        if (fileBytes is null || fileBytes.Length == 0) return null;

        using var stream = new MemoryStream(fileBytes);
        using var pdf = PdfDocument.Open(stream);
        var text = string.Join(" \n ", pdf.GetPages().Select(p => p.Text));
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (!match.Success) return null;

        var numberText = match.Groups[1].Value;
        if (TryParseMoney(numberText, out var money) && money > 0) return money;

        return null;
    }

    private static async Task<decimal?> ExtractMoneyWithPatternAsync(IFormFile contractFile, string pattern, CancellationToken cancellationToken = default)
    {
        if (contractFile is null || contractFile.Length == 0) return null;

        var extension = Path.GetExtension(contractFile.FileName)?.ToLowerInvariant();
        if (extension != ".pdf") return null;

        await using var stream = contractFile.OpenReadStream();
        using var pdf = PdfDocument.Open(stream);
        var text = string.Join(" \n ", pdf.GetPages().Select(p => p.Text));
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (!match.Success) return null;

        var numberText = match.Groups[1].Value;
        if (TryParseMoney(numberText, out var money) && money > 0) return money;

        return null;
    }

    private static bool TryParseMoney(string raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var s = Regex.Replace(raw, @"[^\d\.,]", string.Empty);
        if (string.IsNullOrWhiteSpace(s)) return false;

        var lastDot = s.LastIndexOf('.');
        var lastComma = s.LastIndexOf(',');

        char? decimalSep = null;
        char? thousandSep = null;

        if (lastDot >= 0 && lastComma >= 0)
        {
            if (lastDot > lastComma) { decimalSep = '.'; thousandSep = ','; }
            else { decimalSep = ','; thousandSep = '.'; }
        }
        else if (lastDot >= 0)
        {
            var dotCount = s.Count(c => c == '.');
            if (dotCount > 1) thousandSep = '.';
            else decimalSep = '.';
        }
        else if (lastComma >= 0)
        {
            var commaCount = s.Count(c => c == ',');
            if (commaCount > 1) thousandSep = ',';
            else decimalSep = ',';
        }

        if (thousandSep is not null) s = s.Replace(thousandSep.Value.ToString(), string.Empty);
        if (decimalSep is not null && decimalSep.Value != '.') s = s.Replace(decimalSep.Value.ToString(), ".");

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public static async Task<List<ExtractedStageDto>> ExtractStagesAsync(IFormFile contractFile, CancellationToken cancellationToken = default)
    {
        var stages = new List<ExtractedStageDto>();
        if (contractFile is null || contractFile.Length == 0) return stages;

        await using var stream = contractFile.OpenReadStream();
        using var pdf = PdfDocument.Open(stream);
        var text = string.Join(" \n ", pdf.GetPages().Select(p => p.Text));
        if (string.IsNullOrWhiteSpace(text)) return stages;

        // Chuẩn hóa văn bản bị dính chữ cái từ PDF (ví dụ: San lấpMã nhân viên -> San lấp Mã nhân viên)
        text = Regex.Replace(text, @"(?:Mã|Mã)\s*(?:nhân|nhân)\s*(?:viên|viên)", " Mã nhân viên ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?:Chi|Chi)\s*(?:phí|phí)", " Chi phí ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?:Bắt|Bắt)\s*(?:đầu|đầu)", " Bắt đầu ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?:Kết|Kết)\s*(?:thúc|thúc)", " Kết thúc ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?:Tên|Ten)\s*(?:giai|giải)\s*(?:đoạn|đoạn)", " Tên giai đoạn ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?:Mã|Mã)\s*(?:dự|dự)\s*(?:án|án)", " Mã dự án ", RegexOptions.IgnoreCase);

        var projectIdMatch = Regex.Match(text, @"Mã dự án\s*[:\-]?\s*(\d+)", RegexOptions.IgnoreCase);
        int? projectId = null;
        if (projectIdMatch.Success && int.TryParse(projectIdMatch.Groups[1].Value, out var pid)) projectId = pid;

        // Cơ chế tách khối dựa trên vị trí của "Tên giai đoạn"
        var markerPattern = @"Tên giai đoạn\s*[:\-]?";
        var matches = Regex.Matches(text, markerPattern, RegexOptions.IgnoreCase);
        var matchIndices = matches.Cast<Match>().Select(m => m.Index).ToList();

        for (int i = 0; i < matchIndices.Count; i++)
        {
            int start = matchIndices[i];
            int end = (i + 1 < matchIndices.Count) ? matchIndices[i + 1] : text.Length;
            var block = text.Substring(start, end - start);

            var stage = new ExtractedStageDto { ProjectId = projectId };

            // 1. Tên giai đoạn (Lấy mọi thứ cho đến khi gặp keyword tiếp theo hoặc hết khối)
            var nameMatch = Regex.Match(block, markerPattern + @"\s*(.*?)(?=\s*Mã nhân viên|\s*Chi phí|\s*Bắt đầu|\s*Kết thúc|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (nameMatch.Success) stage.StageName = nameMatch.Groups[1].Value.Trim();

            // 2. Mã nhân viên (Cắt chính xác mã, không dính chữ)
            var userIdMatch = Regex.Match(block, @"Mã nhân viên\s*[:\-]?\s*([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
            if (userIdMatch.Success) stage.AssignedUserId = userIdMatch.Groups[1].Value.Trim();

            // 3. Chi phí
            var budgetMatch = Regex.Match(block, @"Chi phí(?:\s*giai\s*đoạn)?\s*[:\-]?\s*([0-9][0-9\.\,]*)", RegexOptions.IgnoreCase);
            if (budgetMatch.Success && TryParseMoney(budgetMatch.Groups[1].Value, out var money)) stage.Budget = money;

            // 4. Ngày bắt đầu/kết thúc
            var startMatch = Regex.Match(block, @"Bắt đầu\s*[:\-]?\s*(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase);
            if (startMatch.Success && DateOnly.TryParseExact(startMatch.Groups[1].Value, "dd/MM/yyyy", null, DateTimeStyles.None, out var startDate))
                stage.StartDate = startDate;

            var endMatch = Regex.Match(block, @"Kết thúc\s*[:\-]?\s*(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase);
            if (endMatch.Success && DateOnly.TryParseExact(endMatch.Groups[1].Value, "dd/MM/yyyy", null, DateTimeStyles.None, out var endDate))
                stage.EndDate = endDate;

            if (!string.IsNullOrWhiteSpace(stage.StageName)) stages.Add(stage);
        }

        return stages;
    }
}
