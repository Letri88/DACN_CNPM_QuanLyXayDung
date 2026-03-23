using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace DACN_CNPM_QuanLyXayDung.Models;

public static class ContractBudgetExtractor
{
    // Try to extract "total cost"/"total amount"/"tổng chi phí" from contract PDF text.
    // Assumption: PDF has selectable text (not scanned image). OCR is out of scope.
    public static async Task<decimal?> TryExtractTotalCostAsync(IFormFile contractFile, CancellationToken cancellationToken = default)
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

        // Vietnamese-first, then English fallbacks.
        var patterns = new[]
        {
            // Examples: "Tổng chi phí: 12.345.678,00 VNĐ"
            @"tổng\s*(?:chi\s*phí|cộng|giá\s*trị|tiền|thành\s*tiền)\s*[^0-9]{0,30}([0-9][0-9\.\,]*)",
            // Examples: "Tổng giá trị hợp đồng: 12.345.678,00"
            @"tổng\s*giá\s*(?:trị|trị\s*hợp\s*đồng)\s*[^0-9]{0,30}([0-9][0-9\.\,]*)",
            // English: "Total cost/amount/value"
            @"total\s*(?:cost|amount|value)\s*[^0-9]{0,30}([0-9][0-9\.\,]*)",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) continue;

            var numberText = match.Groups[1].Value;
            if (TryParseMoney(numberText, out var money) && money > 0)
            {
                return money;
            }
        }

        // Last fallback: look for a number followed by VNĐ.
        var vnDMatch = Regex.Match(text, @"([0-9][0-9\.\,]*)\s*vnđ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (vnDMatch.Success && TryParseMoney(vnDMatch.Groups[1].Value, out var vnDMoney) && vnDMoney > 0)
        {
            return vnDMoney;
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
}

