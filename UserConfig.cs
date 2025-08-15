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
    public string WorkOrderName { get; set; }

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

    [Serializable]
    public sealed class ColumnVisibilityEntry
    {
      public string TableName { get; set; }
      public string ColumnName { get; set; }
      public bool IsVisible { get; set; }
    }

    public List<ColumnVisibilityEntry> ColumnVisibilities { get; set; } = new List<ColumnVisibilityEntry>();

    [Serializable]
    public sealed class SpecialColumnDef
    {
      public string TableName { get; set; }
      public string ColumnName { get; set; }
      public string TargetTableName { get; set; }
      public string LocalKeyColumn { get; set; }
      public string TargetKeyColumn { get; set; }
      public string TargetValueColumn { get; set; }
    }

    public List<SpecialColumnDef> SpecialColumns { get; set; } = new List<SpecialColumnDef>();

    // Seed defaults when loading config if none present
    private void EnsureDefaultMetricsLayout()
    {
      if (ColumnOrders == null) ColumnOrders = new List<ColumnOrderEntry>();
      if (ColumnWidths == null) ColumnWidths = new List<ColumnWidthEntry>();
      if (ColumnVisibilities == null) ColumnVisibilities = new List<ColumnVisibilityEntry>();
      if (SpecialColumns == null) SpecialColumns = new List<SpecialColumnDef>();

      // Only add if missing to avoid overwriting user's preferences
      bool HasOrder(string table) => ColumnOrders.Exists(e => string.Equals(e.TableName, table, StringComparison.OrdinalIgnoreCase));
      bool HasWidth(string table, string col) => ColumnWidths.Exists(e => string.Equals(e.TableName, table, StringComparison.OrdinalIgnoreCase) && string.Equals(e.ColumnName, col, StringComparison.OrdinalIgnoreCase));

      // Column orders
      if (!HasOrder("Products"))
      {
        ColumnOrders.Add(new ColumnOrderEntry
        {
          TableName = "Products",
          Columns = new List<string>
          {
            "RoomName","Name","ItemNumber","Quantity","Width","Height","Depth","Comments","ProductSpecGroupName",
            "LinkID","ActivityPath","LinkIDWorkOrder","Angle","Comments1","Comments2","Comments3","CopiedLinkIDList",
            "DateShipped","DrawIndex","Extruded","ID","IsBuyOut","JPegName","JPegStream","LinkIDCategory","LinkIDLibrary",
            "LinkIDLocation","LinkIDProductGroup","LinkIDProject","LinkIDRelease","LinkIDSpecificationGroup","LinkIDWall",
            "Modified","PerfectGrainChar","PrintFlag","PrintFlags","QuantityShipped","QuoteName","RoomComponentType","Row_ID",
            "ScanCode","ShippingTicketName","TiffName","TiffStream","Type","UITreeFilter","WMFName","WMFStream","WorkBook",
            "WorkOrderName","X","Y","Z","ActivityPathShort","AncorType"
          }
        });
      }

      if (!HasOrder("Parts"))
      {
        ColumnOrders.Add(new ColumnOrderEntry
        {
          TableName = "Parts",
          Columns = new List<string>
          {
            "Name","Quantity","TotalQuantity","Width","Length","Thickness","MaterialThickness","MaterialName","MaterialXData1",
            "AdjustedCutPartWidth","AdjustedCutPartLength","LinkID","LinkIDWorkOrder","Barcode","LinkIDProduct","LinkIDSubAssembly",
            "BasePoint","BasePointX","BasePointY","BasePointZ","Code","CodeFormula","Comments","Comments1","Comments2","Comments3",
            "CutPartLength","CutPartWidth","DontIncludeRoutesinNestBorder","DrawToken2DElv","DrawToken3D","DXFFileName","EdgeNameBottom",
            "EdgeNameBottomWMF","EdgeNameLeft","EdgeNameLeftWMF","EdgeNameRight","EdgeNameRightWMF","EdgeNameTop","EdgeNameTopWMF",
            "EdgeSequence","Face6Barcode","Face6FileName","FileName","FinishPriority","FullFace6FileName","FullFileName","Grain",
            "GrainFormula","HandlingCode","HandlingCodeFormula","HatchType","HboreBarcodeLeft","HboreBarcodeLower","HboreBarcodeRight",
            "HboreBarcodeUpper","HboreFileNameLeft","HboreFileNameLower","HboreFileNameRight","HboreFileNameUpper","ID","Index",
            "InventoryAvailableQty","InventoryCurrentQty","InventoryMinQty","IrregularShape","IsFormulaMaterial","JPegStream","LabelPosition",
            "LinkIDBottomFaceRendering","LinkIDCategory","LinkIDCoreRendering","LinkIDDefaultVendor","LinkIDEQPart","LinkIDMaterial",
            "LinkIDParentSubAssembly","LinkIDProcessingStations","LinkIDProject","LinkIDSheetSize","LinkIDTopFaceRendering","LinkIDVendor",
            "Location1","Location2","MachinePoint","MarkUp","MarkUpFormula","MaterialCode","MaterialComments","MaterialCommentsFormula",
            "MaterialFlipSetting","MaterialLaborValue","MaterialLaborValueFormula","MaterialType","MaterialXData1Formula","MaterialXData2",
            "MaterialXData2Formula","MaterialXData3","MaterialXData3Formula","Modified","OverProductionQuantity","OverridePartCutLength",
            "OverridePartCutThickness","OverridePartCutWidth","Par1","Par2","Par3","PartType","PerfectGrainCaption","PrintFlag","Region",
            "RemoveRoutesOutsideBorder","RotationX","RotationY","RotationZ","Row_ID","RunFieldNameFace5","RunFieldNameFace6","ScanCode",
            "SkipSpreadsheetSync","TiffStream","Type","UDID","UnderProductionQuantity","UnitType","VendorCost","WasteFactor",
            "WasteFactorFormula","WMFName","WMFNameFace6","WMFStream","WMFStreamDimensioned","WMFStreamFace6","WMFStreamFace6Dimensioned",
            "XD01","XD02","XD03","XD04","XD05","XD06","XD07","XD08","XD09","XD10","XD11","XD12","XD13","XD14","XD15","XD16",
            "XD17","XD18"
          }
        });
      }

      // Column widths
      void AddWidth(string table, string col, int width)
      {
        if (!HasWidth(table, col)) ColumnWidths.Add(new ColumnWidthEntry { TableName = table, ColumnName = col, Width = width });
      }
      AddWidth("Products", "RoomName", 194);
      AddWidth("Products", "Name", 229);
      AddWidth("Products", "ProductSpecGroupName", 155);
      AddWidth("Products", "Comments", 208);
      AddWidth("Products", "Quantity", 58);
      AddWidth("Products", "ItemNumber", 59);
      AddWidth("Parts", "Code", 55);
      AddWidth("Parts", "Name", 374);
      AddWidth("Parts", "MaterialName", 171);
    }

    public List<SpecialColumnDef> GetSpecialColumnsForTable(string tableName)
    {
      if (string.IsNullOrWhiteSpace(tableName)) return new List<SpecialColumnDef>();
      return SpecialColumns.FindAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
    }

    public void UpsertSpecialColumn(SpecialColumnDef def)
    {
      if (def == null) return;
      if (string.IsNullOrWhiteSpace(def.TableName) || string.IsNullOrWhiteSpace(def.ColumnName)) return;
      var existing = SpecialColumns.Find(e => string.Equals(e.TableName, def.TableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, def.ColumnName, StringComparison.OrdinalIgnoreCase));
      if (existing == null)
      {
        SpecialColumns.Add(def);
      }
      else
      {
        existing.TargetTableName = def.TargetTableName;
        existing.LocalKeyColumn = def.LocalKeyColumn;
        existing.TargetKeyColumn = def.TargetKeyColumn;
        existing.TargetValueColumn = def.TargetValueColumn;
      }
    }

    public void RemoveSpecialColumn(string tableName, string columnName)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      SpecialColumns.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
    }

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

    public bool? TryGetColumnVisibility(string tableName, string columnName)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return null;
      var entry = ColumnVisibilities.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      return entry?.IsVisible;
    }

    public void SetColumnVisibility(string tableName, string columnName, bool isVisible)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      var entry = ColumnVisibilities.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      if (entry == null)
      {
        entry = new ColumnVisibilityEntry { TableName = tableName, ColumnName = columnName, IsVisible = isVisible };
        ColumnVisibilities.Add(entry);
      }
      else
      {
        entry.IsVisible = isVisible;
      }
    }

    private static bool IsDirectoryWritable(string directoryPath)
    {
      try
      {
        if (!Directory.Exists(directoryPath))
        {
          Directory.CreateDirectory(directoryPath);
        }

        // Try to create a temp file to test write permissions
        string tempFile = Path.Combine(directoryPath, Path.GetRandomFileName());
        using (File.Create(tempFile))
        {
          // File created successfully
        }
        File.Delete(tempFile);
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static string GetUserConfigDirectory()
    {
      // Use AppData\Local for user-specific config
      var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      return Path.Combine(appDataPath, "WorkOrderBlender");
    }

    private static string GetOriginalConfigDirectory()
    {
      // Use the directory where the executable is located
      var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      return exeDir ?? Environment.CurrentDirectory;
    }

    private static string GetConfigDirectory()
    {
      var originalDir = GetOriginalConfigDirectory();

      // If original directory is writable, use it
      if (IsDirectoryWritable(originalDir))
      {
        return originalDir;
      }

      // Otherwise, use user AppData directory
      return GetUserConfigDirectory();
    }

    private static void EnsureConfigCopiedToUserLocation()
    {
      var originalDir = GetOriginalConfigDirectory();
      var userDir = GetUserConfigDirectory();
      var originalConfigPath = Path.Combine(originalDir, "settings.xml");
      var userConfigPath = Path.Combine(userDir, "settings.xml");

      // If original is not writable and has a config file, copy it to user location
      if (!IsDirectoryWritable(originalDir) && File.Exists(originalConfigPath) && !File.Exists(userConfigPath))
      {
        try
        {
          if (!Directory.Exists(userDir))
          {
            Directory.CreateDirectory(userDir);
          }
          File.Copy(originalConfigPath, userConfigPath);
        }
        catch (Exception ex)
        {
          Program.Log("Failed to copy config to user directory", ex);
        }
      }
    }

    private static string GetConfigPath()
    {
      EnsureConfigCopiedToUserLocation();
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
            cfg.EnsureDefaultMetricsLayout();
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
            var cfg = (UserConfig)ser.Deserialize(fs);
            cfg.EnsureDefaultMetricsLayout();
            return cfg;
          }
        }
      }
      catch { }
      var created = new UserConfig();
      created.EnsureDefaultMetricsLayout();
      return created;
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
