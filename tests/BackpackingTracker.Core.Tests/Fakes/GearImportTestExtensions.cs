using BackpackingTracker.Core.Import;

namespace BackpackingTracker.Core.Tests.Fakes;

/// <summary>For tests of how rows are read and matched, where the Gear List and Default Categories options don't matter.</summary>
public static class GearImportTestExtensions
{
    /// <summary>Previews the file and imports it with both options off, returning the preview.</summary>
    public static async Task<ImportPreview> ImportCsvAsync(this GearImport import, string csvText)
    {
        var preview = await import.PreviewCsvAsync(csvText);
        await import.ImportAsync(preview, new ImportOptions());
        return preview;
    }
}
