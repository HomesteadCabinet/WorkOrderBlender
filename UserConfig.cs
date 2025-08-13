using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace WorkOrderBlender
{
  [Serializable]
  public sealed class UserConfig
  {
    public string DefaultRoot { get; set; }
    public string DefaultOutput { get; set; }
    public string SdfFileName { get; set; }

    [Serializable]
    public sealed class ColumnWidthEntry
    {
      public string TableName { get; set; }
      public string ColumnName { get; set; }
      public int Width { get; set; }
    }

    public List<ColumnWidthEntry> ColumnWidths { get; set; } = new List<ColumnWidthEntry>();

    [Serializable]
    public sealed class ColumnOrderEntry
    {
      public string TableName { get; set; }
      public List<string> Columns { get; set; } = new List<string>();
    }

    public List<ColumnOrderEntry> ColumnOrders { get; set; } = new List<ColumnOrderEntry>();

    public int? TryGetColumnWidth(string tableName, string columnName)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return null;
      var entry = ColumnWidths.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      return entry != null && entry.Width > 0 ? (int?)entry.Width : null;
    }

    public void SetColumnWidth(string tableName, string columnName, int width)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName) || width <= 0) return;
      var entry = ColumnWidths.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      if (entry == null)
      {
        entry = new ColumnWidthEntry { TableName = tableName, ColumnName = columnName, Width = width };
        ColumnWidths.Add(entry);
      }
      else
      {
        entry.Width = width;
      }
    }

    public List<string> TryGetColumnOrder(string tableName)
    {
      if (string.IsNullOrWhiteSpace(tableName)) return null;
      var entry = ColumnOrders.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
      return entry != null && entry.Columns != null && entry.Columns.Count > 0 ? new List<string>(entry.Columns) : null;
    }

    public void SetColumnOrder(string tableName, IEnumerable<string> columnNames)
    {
      if (string.IsNullOrWhiteSpace(tableName) || columnNames == null) return;
      var list = new List<string>(columnNames);
      var entry = ColumnOrders.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
      if (entry == null)
      {
        entry = new ColumnOrderEntry { TableName = tableName, Columns = list };
        ColumnOrders.Add(entry);
      }
      else
      {
        entry.Columns = list;
      }
    }

    private static string GetConfigDirectory()
    {
      var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WorkOrderBlender");
      return dir;
    }

    private static string GetConfigPath()
    {
      return Path.Combine(GetConfigDirectory(), "settings.xml");
    }

    public static UserConfig Load(string fallbackRoot, string fallbackOutput, string fallbackSdf)
    {
      try
      {
        var path = GetConfigPath();
        if (File.Exists(path))
        {
          var ser = new XmlSerializer(typeof(UserConfig));
          using (var fs = File.OpenRead(path))
          {
            var cfg = (UserConfig)ser.Deserialize(fs);
            // Fill in any missing values with fallbacks
            cfg.DefaultRoot = string.IsNullOrWhiteSpace(cfg.DefaultRoot) ? fallbackRoot : cfg.DefaultRoot;
            cfg.DefaultOutput = string.IsNullOrWhiteSpace(cfg.DefaultOutput) ? fallbackOutput : cfg.DefaultOutput;
            cfg.SdfFileName = string.IsNullOrWhiteSpace(cfg.SdfFileName) ? fallbackSdf : cfg.SdfFileName;
            return cfg;
          }
        }
      }
      catch { }

      return new UserConfig
      {
        DefaultRoot = fallbackRoot,
        DefaultOutput = fallbackOutput,
        SdfFileName = fallbackSdf,
      };
    }

    public static UserConfig LoadOrDefault()
    {
      try
      {
        var path = GetConfigPath();
        if (File.Exists(path))
        {
          var ser = new XmlSerializer(typeof(UserConfig));
          using (var fs = File.OpenRead(path))
          {
            return (UserConfig)ser.Deserialize(fs);
          }
        }
      }
      catch { }
      return new UserConfig();
    }

    public void Save()
    {
      try
      {
        var dir = GetConfigDirectory();
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var path = GetConfigPath();
        var ser = new XmlSerializer(typeof(UserConfig));
        using (var fs = File.Create(path))
        {
          ser.Serialize(fs, this);
        }
      }
      catch (Exception ex)
      {
        Program.Log("Saving UserConfig failed", ex);
      }
    }
  }
}
