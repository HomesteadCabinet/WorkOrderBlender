using System;
using System.Collections.Generic;

namespace WorkOrderBlender
{
  internal sealed class InMemoryEditStore
  {
    public event EventHandler ChangesUpdated;

    private readonly object sync = new object();
    // Table -> LinkID(string) -> ColumnName -> Value
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, object>>> tableToEdits
      = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
    // Table -> Deleted LinkIDs
    private readonly Dictionary<string, HashSet<string>> tableToDeleted
      = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    private void OnChangesUpdated()
    {
      ChangesUpdated?.Invoke(this, EventArgs.Empty);
    }

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
      OnChangesUpdated();
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
        // Remove any existing column edits for this row since it's being deleted
        if (tableToEdits.TryGetValue(tableName, out var linkMap))
        {
          linkMap.Remove(linkId);
          // Remove the table entry if no more edits exist
          if (linkMap.Count == 0)
          {
            tableToEdits.Remove(tableName);
          }
        }

        // Mark the row as deleted
        if (!tableToDeleted.TryGetValue(tableName, out var set))
        {
          set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          tableToDeleted[tableName] = set;
        }
        set.Add(linkId);
      }
      OnChangesUpdated();
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

    public void UnmarkDeleted(string tableName, string linkId)
    {
      if (string.IsNullOrWhiteSpace(tableName)) return;
      if (linkId == null) linkId = string.Empty;
      lock (sync)
      {
        if (tableToDeleted.TryGetValue(tableName, out var set))
        {
          set.Remove(linkId);
          // Remove the table entry if no more deletions exist
          if (set.Count == 0)
          {
            tableToDeleted.Remove(tableName);
          }
        }
      }
      OnChangesUpdated();
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

    public HashSet<string> GetAllTablesWithEdits()
    {
      lock (sync)
      {
        return new HashSet<string>(tableToEdits.Keys, StringComparer.OrdinalIgnoreCase);
      }
    }

    public HashSet<string> GetAllTablesWithDeletions()
    {
      lock (sync)
      {
        return new HashSet<string>(tableToDeleted.Keys, StringComparer.OrdinalIgnoreCase);
      }
    }

    public bool HasAnyChanges()
    {
      lock (sync)
      {
        return tableToEdits.Count > 0 || tableToDeleted.Count > 0;
      }
    }

    public int GetTotalChangeCount()
    {
      lock (sync)
      {
        int editCount = 0;
        foreach (var tableEdits in tableToEdits.Values)
        {
          foreach (var rowEdits in tableEdits.Values)
          {
            editCount += rowEdits.Count;
          }
        }

        int deleteCount = 0;
        foreach (var deletedSet in tableToDeleted.Values)
        {
          deleteCount += deletedSet.Count;
        }

        return editCount + deleteCount;
      }
    }

    public void ClearAllChanges()
    {
      lock (sync)
      {
        tableToEdits.Clear();
        tableToDeleted.Clear();
      }
      OnChangesUpdated();
    }

    public void ClearTableChanges(string tableName)
    {
      if (string.IsNullOrWhiteSpace(tableName)) return;
      lock (sync)
      {
        tableToEdits.Remove(tableName);
        tableToDeleted.Remove(tableName);
      }
      OnChangesUpdated();
    }
  }
}
