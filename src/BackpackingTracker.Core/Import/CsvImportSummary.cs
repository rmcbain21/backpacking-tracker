namespace BackpackingTracker.Core.Import;

public sealed record CsvImportSummary(IReadOnlyList<CsvRowResult> Rows)
{
    public IReadOnlyList<CsvRowResult> Created => RowsWith(CsvRowOutcome.Created);

    public IReadOnlyList<CsvRowResult> Reused => RowsWith(CsvRowOutcome.Reused);

    public IReadOnlyList<CsvRowResult> Flagged => RowsWith(CsvRowOutcome.Flagged);

    public IReadOnlyList<CsvRowResult> Rejected => RowsWith(CsvRowOutcome.Rejected);

    private IReadOnlyList<CsvRowResult> RowsWith(CsvRowOutcome outcome) => Rows.Where(row => row.Outcome == outcome).ToList();
}
