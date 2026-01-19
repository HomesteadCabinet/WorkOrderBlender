using System;

namespace WorkOrderBlender
{
  // Shared helpers for cleaning up part names before display/use.
  internal static class PartNameUtils
  {
    // Clean up a part name string for display and downstream logic.
    public static string CleanPartName(string name)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(name)) return name;

        // Remove the [NODRAW] marker if present (case-insensitive).
        var cleaned = RemoveAllIgnoreCase(name, "[NODRAW]");

        // Normalize whitespace after removals.
        cleaned = CollapseWhitespace(cleaned).Trim();

        return cleaned;
      }
      catch (Exception ex)
      {
        // Never fail name cleanup; log and fall back to original value.
        Program.Log($"PartNameUtils.CleanPartName error for '{name}'", ex);
        return name;
      }
    }

    // Remove all occurrences of token, ignoring case.
    private static string RemoveAllIgnoreCase(string input, string token)
    {
      if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(token)) return input;

      var result = input;
      while (true)
      {
        var idx = result.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) break;
        result = result.Remove(idx, token.Length);
      }
      return result;
    }

    // Collapse repeating whitespace to single spaces.
    private static string CollapseWhitespace(string input)
    {
      if (string.IsNullOrEmpty(input)) return input;

      // Keep it simple: repeatedly replace double spaces. This is fast enough for typical part names.
      var s = input;
      while (s.Contains("  "))
      {
        s = s.Replace("  ", " ");
      }
      return s;
    }
  }
}
