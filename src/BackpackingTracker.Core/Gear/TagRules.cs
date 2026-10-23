namespace BackpackingTracker.Core.Gear;

/// <summary>
/// How Tags are compared and edited: trimmed, never blank, and matched ignoring case. Gear Types follow
/// the same rules (Comparer for spelling, Suggest for suggestions).
/// </summary>
public static class TagRules
{
    public static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>The Tags with one more added, trimmed; a blank or repeated Tag leaves them as they were.</summary>
    public static IReadOnlyList<string> Add(IReadOnlyList<string> tags, string tag)
    {
        var trimmed = tag.Trim();
        return trimmed.Length == 0 || tags.Contains(trimmed, Comparer) ? tags : [.. tags, trimmed];
    }

    /// <summary>Tags in use that aren't chosen yet and contain what has been typed so far.</summary>
    public static IReadOnlyList<string> Suggest(IEnumerable<string> tagsInUse, IReadOnlyList<string> chosen, string? typed)
    {
        var text = typed?.Trim() ?? "";
        return tagsInUse
            .Where(tag => !chosen.Contains(tag, Comparer))
            .Where(tag => tag.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
