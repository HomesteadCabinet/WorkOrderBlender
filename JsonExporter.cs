using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WorkOrderBlender
{
  // Exports selected tables from a SQL CE (.sdf) work order database into one consolidated JSON file.
  // Output shape:
  //   { "source": "...", "exportedAt": "...", "tables": { "Parts": [ {col: val, ...}, ... ], ... } }
  // Rows are streamed straight to the writer so large tables don't need to fit in memory.
  internal static class JsonExporter
  {
    public sealed class ExportResult
    {
      public Dictionary<string, int> RowCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    public static List<string> ListTables(string sdfPath)
    {
      string tempCopy;
      using (var conn = SqlCeUtils.OpenWithFallback(sdfPath, out tempCopy))
      {
        try { return ListTables(conn); }
        finally { TryDelete(tempCopy); }
      }
    }

    private static List<string> ListTables(SqlCeConnection conn)
    {
      var names = new List<string>();
      using (var cmd = new SqlCeCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'TABLE'", conn))
      using (var r = cmd.ExecuteReader())
      {
        while (r.Read()) names.Add(r.GetString(0));
      }
      names.Sort(StringComparer.OrdinalIgnoreCase);
      return names;
    }

    // tables: requested names (case-insensitive). Null/empty = export every table.
    // Throws ArgumentException if any requested table doesn't exist in the database.
    public static ExportResult Export(string sdfPath, IList<string> tables, string outputPath, Action<string, int> progress = null)
    {
      string tempCopy;
      using (var conn = SqlCeUtils.OpenWithFallback(sdfPath, out tempCopy))
      {
        try
        {
          var available = ListTables(conn);
          List<string> selected;
          if (tables == null || tables.Count == 0)
          {
            selected = available;
          }
          else
          {
            selected = new List<string>();
            var missing = new List<string>();
            foreach (var t in tables)
            {
              var match = available.FirstOrDefault(a => string.Equals(a, t, StringComparison.OrdinalIgnoreCase));
              if (match == null) missing.Add(t);
              else if (!selected.Contains(match)) selected.Add(match);
            }
            if (missing.Count > 0)
            {
              throw new ArgumentException(
                "Table(s) not found: " + string.Join(", ", missing) +
                ". Available: " + string.Join(", ", available));
            }
          }

          var result = new ExportResult();
          var tmpOut = outputPath == null ? null : outputPath + ".tmp";
          var stream = outputPath == null ? Console.OpenStandardOutput() : (Stream)File.Create(tmpOut);
          try
          {
            using (var w = new StreamWriter(stream, new UTF8Encoding(false), 65536))
            {
              w.Write("{\n  \"source\": ");
              WriteString(w, Path.GetFullPath(sdfPath));
              w.Write(",\n  \"exportedAt\": ");
              WriteString(w, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
              w.Write(",\n  \"tables\": {");

              bool firstTable = true;
              foreach (var table in selected)
              {
                if (!firstTable) w.Write(",");
                firstTable = false;
                w.Write("\n    ");
                WriteString(w, table);
                w.Write(": [");
                int count = WriteTableRows(conn, table, w);
                w.Write(count > 0 ? "\n    ]" : "]");
                result.RowCounts[table] = count;
                progress?.Invoke(table, count);
              }
              w.Write("\n  }\n}\n");
            }
          }
          catch
          {
            if (tmpOut != null) TryDelete(tmpOut);
            throw;
          }
          if (tmpOut != null)
          {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            File.Move(tmpOut, outputPath);
          }
          return result;
        }
        finally { TryDelete(tempCopy); }
      }
    }

    private static int WriteTableRows(SqlCeConnection conn, string table, TextWriter w)
    {
      int count = 0;
      using (var cmd = new SqlCeCommand("SELECT * FROM [" + table.Replace("]", "]]") + "]", conn))
      using (var r = cmd.ExecuteReader())
      {
        int n = r.FieldCount;
        var names = new string[n];
        for (int i = 0; i < n; i++) names[i] = r.GetName(i);

        while (r.Read())
        {
          w.Write(count == 0 ? "\n      {" : ",\n      {");
          for (int i = 0; i < n; i++)
          {
            if (i > 0) w.Write(", ");
            WriteString(w, names[i]);
            w.Write(": ");
            WriteValue(w, r.IsDBNull(i) ? null : r.GetValue(i));
          }
          w.Write("}");
          count++;
        }
      }
      return count;
    }

    private static void WriteValue(TextWriter w, object v)
    {
      switch (v)
      {
        case null: w.Write("null"); return;
        case bool b: w.Write(b ? "true" : "false"); return;
        case string s: WriteString(w, s); return;
        case byte[] bytes: WriteString(w, Convert.ToBase64String(bytes)); return;
        case DateTime dt: WriteString(w, dt.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture)); return;
        case Guid g: WriteString(w, g.ToString()); return;
        case double d:
          if (double.IsNaN(d) || double.IsInfinity(d)) w.Write("null");
          else w.Write(d.ToString("R", CultureInfo.InvariantCulture));
          return;
        case float f:
          if (float.IsNaN(f) || float.IsInfinity(f)) w.Write("null");
          else w.Write(f.ToString("R", CultureInfo.InvariantCulture));
          return;
        case decimal m: w.Write(m.ToString(CultureInfo.InvariantCulture)); return;
        case byte _:
        case sbyte _:
        case short _:
        case ushort _:
        case int _:
        case uint _:
        case long _:
        case ulong _:
          w.Write(Convert.ToString(v, CultureInfo.InvariantCulture)); return;
        default: WriteString(w, Convert.ToString(v, CultureInfo.InvariantCulture)); return;
      }
    }

    private static void WriteString(TextWriter w, string s)
    {
      w.Write('"');
      foreach (var c in s)
      {
        switch (c)
        {
          case '"': w.Write("\\\""); break;
          case '\\': w.Write("\\\\"); break;
          case '\n': w.Write("\\n"); break;
          case '\r': w.Write("\\r"); break;
          case '\t': w.Write("\\t"); break;
          case '\b': w.Write("\\b"); break;
          case '\f': w.Write("\\f"); break;
          default:
            if (c < 0x20) w.Write("\\u" + ((int)c).ToString("x4"));
            else w.Write(c);
            break;
        }
      }
      w.Write('"');
    }

    private static void TryDelete(string path)
    {
      try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); } catch { }
    }
  }
}
