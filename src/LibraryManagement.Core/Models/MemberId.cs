namespace LibraryManagement.Models;

/// <summary>
/// Member IDs look like <c>BTS-12-345</c>, but people type them (and scanners send them)
/// in many shapes: <c>bts12345</c>, <c>12-345</c>, <c>BTS 12 345</c>. Comparing the
/// canonical key makes all of those match the same member.
/// </summary>
public static class MemberId
{
    private const string Prefix = "BTS";

    /// <summary>Upper-case letters and digits only, without the <c>BTS</c> prefix.</summary>
    public static string CanonicalKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var key = new string(input.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return key.StartsWith(Prefix, StringComparison.Ordinal) ? key[Prefix.Length..] : key;
    }

    public static bool Matches(string? left, string? right)
    {
        var key = CanonicalKey(left);
        return key.Length > 0 && key == CanonicalKey(right);
    }
}
