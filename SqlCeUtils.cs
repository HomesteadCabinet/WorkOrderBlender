using System;
using System.Data.SqlServerCe;
using System.IO;

namespace WorkOrderBlender
{
  internal static class SqlCeUtils
  {
    private static string GetTempDirectory()
    {
      return Path.Combine(Path.GetTempPath(), "WOB_SQLCE");
    }

    public static SqlCeConnection CreateReadOnlyConnection(string dataSourcePath)
    {
      string tempDir = GetTempDirectory();
      try { Directory.CreateDirectory(tempDir); } catch { }
      return new SqlCeConnection($"Data Source={dataSourcePath};Mode=Read Only;Temp File Directory={tempDir}");
    }

    public static SqlCeConnection OpenWithFallback(string sourcePath, out string tempCopyPath)
    {
      tempCopyPath = null;
      try
      {
        var ro = CreateReadOnlyConnection(sourcePath);
        ro.Open();
        return ro;
      }
      catch
      {
        try
        {
          string tempDir = GetTempDirectory();
          Directory.CreateDirectory(tempDir);
          string temp = Path.Combine(tempDir, Path.GetFileName(sourcePath) + "." + Guid.NewGuid().ToString("N") + ".sdf");
          File.Copy(sourcePath, temp, true);
          var rw = new SqlCeConnection($"Data Source={temp};");
          rw.Open();
          tempCopyPath = temp;
          return rw;
        }
        catch
        {
          throw;
        }
      }
    }
  }
}
