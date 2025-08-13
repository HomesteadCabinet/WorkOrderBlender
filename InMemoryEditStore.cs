using System;
using System.Collections.Generic;

namespace WorkOrderBlender
{
  internal sealed class InMemoryEditStore
  {
    private readonly object sync = new object();
    // Table -> LinkID(string) -> ColumnName -> Value
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, object>>> tableToEdits
      = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
    // Table -> Deleted LinkIDs
    private readonly Dictionary<string, HashSet<string>> tableToDeleted
      = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    public void UpsertOverride(string tableName, string linkId, string columnName, object value)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      if (linkId == null) linkId = string.Empty;
      lock (sync)
      {
        if (!tableToEdits.TryGetValue(tableName, out var linkMap))
        {
          linkMap = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
          tableToEdits[tableName] = linkMap;
        }
        if (!linkMap.TryGetValue(linkId, out var colMap))
        {
          colMap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
          linkMap[linkId] = colMap;
        }
        colMap[columnName] = value;
      }
    }

    public bool TryGetRowOverrides(string tableName, string linkId, out Dictionary<string, object> overrides)
    {
      lock (sync)
      {
        overrides = null;
        if (tableToEdits.TryGetValue(tableName, out var linkMap))
        {
          if (linkMap.TryGetValue(linkId, out var colMap))
          {
            overrides = new Dictionary<string, object>(colMap, StringComparer.OrdinalIgnoreCase);
            return true;
          }
        }
        return false;
      }
    }

    public Dictionary<string, Dictionary<string, object>> SnapshotTable(string tableName)
    {
      lock (sync)
      {
        var result = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        if (tableToEdits.TryGetValue(tableName, out var linkMap))
        {
          foreach (var kvp in linkMap)
          {
            result[kvp.Key] = new Dictionary<string, object>(kvp.Value, StringComparer.OrdinalIgnoreCase);
          }
        }
        return result;
      }
    }

    public void MarkDeleted(string tableName, string linkId)
    {
      if (string.IsNullOrWhiteSpace(tableName)) return;
      if (linkId == null) linkId = string.Empty;
      lock (sync)
      {
        if (!tableToDeleted.TryGetValue(tableName, out var set))
        {
          set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          tableToDeleted[tableName] = set;
        }
        set.Add(linkId);
      }
    }

    public bool IsDeleted(string tableName, string linkId)
    {
      lock (sync)
      {
        if (tableToDeleted.TryGetValue(tableName, out var set))
        {
          return set.Contains(linkId ?? string.Empty);
        }
        return false;
      }
    }

    public HashSet<string> SnapshotDeleted(string tableName)
    {
      lock (sync)
      {
        if (tableToDeleted.TryGetValue(tableName, out var set))
        {
          return new HashSet<string>(set, StringComparer.OrdinalIgnoreCase);
        }
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      }
    }
  }
}
