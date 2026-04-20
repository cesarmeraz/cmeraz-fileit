// This is where the actual work happens.
// We take the raw GL Account CSV file, parse every row,
// group the accounts by company code and account group,
// count how many accounts are in each group,
// and produce a clean summary CSV as the output.
// No Azure resources, no SSIS, pure C#.
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.DataFlow.App.Transform;

public interface ITransformGlAccounts
{
    Task<string> RunAsync(Stream csvStream, string correlationId, CancellationToken cancellationToken = default);
}

public class TransformGlAccounts : ITransformGlAccounts
{
    private readonly ILogger<TransformGlAccounts> _logger;

    public TransformGlAccounts(ILogger<TransformGlAccounts> logger)
    {
        _logger = logger;
    }

    public async Task<string> RunAsync(Stream csvStream, string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            DataFlowEvents.DataFlowTransform.Id,
            "Starting GL Account transform for correlation {CorrelationId}",
            correlationId
        );

        // Read all the lines from the CSV stream
        var lines = new List<string>();
        using var reader = new StreamReader(csvStream);
        string? csvLine;
        while ((csvLine = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lines.Add(csvLine);
        }

        // First line is the header — skip it but use it to find our column positions
        var headers = lines[0].Split(',');
        int companyCodeIndex = Array.IndexOf(headers, " COMPANYCODE");
        int accountGroupIndex = Array.IndexOf(headers, " GLACCOUNTGROUP");
        int balanceSheetIndex = Array.IndexOf(headers, " ISBALANCESHEETACCOUNT");

        // Group the rows by company code and account group and count them
        // We use a dictionary where the key is "COMPANYCODE|GLACCOUNTGROUP"
        var summary = new Dictionary<string, (int Count, int BalanceSheetCount)>();

        foreach (var row in lines.Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fields = row.Split(',');

            if (fields.Length <= Math.Max(companyCodeIndex, accountGroupIndex))
                continue;

            string companyCode = fields[companyCodeIndex].Trim();
            string accountGroup = fields[accountGroupIndex].Trim();
            string isBalanceSheet = fields[balanceSheetIndex].Trim();

            string key = $"{companyCode}|{accountGroup}";

            if (!summary.ContainsKey(key))
                summary[key] = (0, 0);

            var current = summary[key];
            int balanceSheetIncrement = isBalanceSheet == "X" ? 1 : 0;
            summary[key] = (current.Count + 1, current.BalanceSheetCount + balanceSheetIncrement);
        }

        // Build the output CSV
        var outputLines = new List<string>
        {
            "CompanyCode,AccountGroup,TotalAccounts,BalanceSheetAccounts,NonBalanceSheetAccounts"
        };

        foreach (var kvp in summary.OrderBy(k => k.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parts = kvp.Key.Split('|');
            string companyCode = parts[0];
            string accountGroup = parts[1];
            int total = kvp.Value.Count;
            int balanceSheet = kvp.Value.BalanceSheetCount;
            int nonBalanceSheet = total - balanceSheet;

            outputLines.Add($"{companyCode},{accountGroup},{total},{balanceSheet},{nonBalanceSheet}");
        }

        _logger.LogInformation(
            DataFlowEvents.DataFlowTransformCompleted.Id,
            "GL Account transform complete. {GroupCount} groups produced for correlation {CorrelationId}",
            summary.Count,
            correlationId
        );

        return string.Join(Environment.NewLine, outputLines);
    }
}
