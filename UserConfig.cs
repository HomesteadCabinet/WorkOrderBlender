using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace WorkOrderBlender
{
  [Serializable]
  public sealed class UserConfig
  {
    public string DefaultRoot { get; set; }
    public string DefaultOutput { get; set; }
    public string SdfFileName { get; set; }
    public string WorkOrderName { get; set; }
    public int MainSplitterDistance { get; set; } = 300; // persisted left panel width
    public int MainWindowWidth { get; set; } = 0;  // 0 = not set
    public int MainWindowHeight { get; set; } = 0; // 0 = not set
    public int MainWindowX { get; set; } = -1; // -1 = center/start default
    public int MainWindowY { get; set; } = -1; // -1 = center/start default
    public bool HidePurchasing { get; set; } = true; // default to true
    public bool DynamicSheetCosts { get; set; } = false; // default to false

    // MSSQL connection settings for Microvellum database
    public string MssqlServer { get; set; } = "SERVER\\SQL";
    public string MssqlDatabase { get; set; } = "MicrovellumData";
    public string MssqlUsername { get; set; } = "user";
    public string MssqlPassword { get; set; } = "password";
    public bool MssqlEnabled { get; set; } = true; // Enable/disable MSSQL validation

    // Configurable front filter keywords for parts filtering
    [XmlArray("FrontFilterKeywords")]
    [XmlArrayItem("string")]
    public List<string> FrontFilterKeywords
    {
        get => _frontFilterKeywords ?? (_frontFilterKeywords = new List<string> { "Slab", "Drawer Front" });
        set => _frontFilterKeywords = value?.Distinct().ToList() ?? new List<string> { "Slab", "Drawer Front" };
    }
    private List<string> _frontFilterKeywords;

    // Configurable subassembly filter keywords for subassembly filtering
    [XmlArray("SubassemblyFilterKeywords")]
    [XmlArrayItem("string")]
    public List<string> SubassemblyFilterKeywords
    {
        get => _subassemblyFilterKeywords ?? (_subassemblyFilterKeywords = new List<string> { "Door", "Drawer Front", "RPE" });
        set => _subassemblyFilterKeywords = value?.Distinct().ToList() ?? new List<string> { "Door", "Drawer Front", "RPE" };
    }
    private List<string> _subassemblyFilterKeywords;

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
    public sealed class ColumnHeaderEntry
    {
      public string TableName { get; set; }
      public string ColumnName { get; set; }
      public string HeaderText { get; set; }
      public string ToolTip { get; set; }
    }

    public List<ColumnHeaderEntry> ColumnHeaders { get; set; } = new List<ColumnHeaderEntry>();

    [Serializable]
    public sealed class ColumnColorEntry
    {
      public string TableName { get; set; }
      public string ColumnName { get; set; }
      public int BackColorArgb { get; set; } // Store as ARGB integer for serialization
      public int ForeColorArgb { get; set; } // Store as ARGB integer for serialization
    }

    public List<ColumnColorEntry> ColumnColors { get; set; } = new List<ColumnColorEntry>();

    [Serializable]
    public sealed class VirtualColumnDef
    {
      public string TableName { get; set; }
      public string ColumnName { get; set; }

      // Lookup column properties (for display-only virtual columns)
      public string TargetTableName { get; set; }
      public string LocalKeyColumn { get; set; }
      public string TargetKeyColumn { get; set; }
      public string TargetValueColumn { get; set; }

      // Action column properties (for interactive virtual columns)
      public string ColumnType { get; set; } = "Lookup"; // "Lookup" or "Action"
      public string ActionType { get; set; } // "3DViewer", "WebLink", "Export", etc.
      public string ButtonText { get; set; } // Text to display on button
      public string ButtonIcon { get; set; } // Optional icon for button

      // Helper properties
      public bool IsActionColumn => string.Equals(ColumnType, "Action", StringComparison.OrdinalIgnoreCase);
      public bool IsLookupColumn => string.Equals(ColumnType, "Lookup", StringComparison.OrdinalIgnoreCase);
      public bool IsBuiltInColumn => string.Equals(ColumnType, "BuiltIn", StringComparison.OrdinalIgnoreCase);
    }

    // Keep XML element name as "VirtualColumns" for backward compatibility with existing settings files
    [XmlArray("VirtualColumns")]
    [XmlArrayItem("VirtualColumnDef")]
    public List<VirtualColumnDef> VirtualColumns { get; set; } = new List<VirtualColumnDef>();

    // Saw Queue directories
    public string StagingDir { get; set; } = @"P:\CadLinkPTX\staging";
    public string ReleaseDir { get; set; } = @"P:\CadLinkPTX\release";

    // Update management
    public string SkippedVersion { get; set; } = string.Empty;
    public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

    // Seed defaults when loading config if none present
    private void EnsureDefaultMetricsLayout()
    {
      if (ColumnOrders == null) ColumnOrders = new List<ColumnOrderEntry>();

      // Ensure built-in source file virtual column exists for all breakdown tables
      EnsureBuiltInSourceFileColumn();
      if (ColumnWidths == null) ColumnWidths = new List<ColumnWidthEntry>();
      if (ColumnVisibilities == null) ColumnVisibilities = new List<ColumnVisibilityEntry>();
      if (ColumnHeaders == null) ColumnHeaders = new List<ColumnHeaderEntry>();
      if (VirtualColumns == null) VirtualColumns = new List<VirtualColumnDef>();

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

      // Set default width for source file column
      AddWidth("Products", "_SourceFile", 120);
      AddWidth("Parts", "_SourceFile", 120);
      AddWidth("Subassemblies", "_SourceFile", 120);
    }

    private static void CleanupDuplicatesAfterDeserialization(UserConfig cfg)
    {
      bool hasDuplicates = false;

      // Clean up FrontFilterKeywords duplicates in the backing field
      if (cfg._frontFilterKeywords != null && cfg._frontFilterKeywords.Count != cfg._frontFilterKeywords.Distinct().Count())
      {
        hasDuplicates = true;
        cfg._frontFilterKeywords = cfg._frontFilterKeywords.Distinct().ToList();
        Program.Log("UserConfig: Cleaned up duplicate FrontFilterKeywords after XML deserialization");
      }

      // Clean up SubassemblyFilterKeywords duplicates in the backing field
      if (cfg._subassemblyFilterKeywords != null && cfg._subassemblyFilterKeywords.Count != cfg._subassemblyFilterKeywords.Distinct().Count())
      {
        hasDuplicates = true;
        cfg._subassemblyFilterKeywords = cfg._subassemblyFilterKeywords.Distinct().ToList();
        Program.Log("UserConfig: Cleaned up duplicate SubassemblyFilterKeywords after XML deserialization");
      }

      // Save the cleaned configuration if duplicates were found
      if (hasDuplicates)
      {
        try
        {
          cfg.Save();
          Program.Log("UserConfig: Saved cleaned configuration to remove duplicates after deserialization");
        }
        catch (IOException ex) when (ex.Message.Contains("being used by another process"))
        {
          Program.Log("UserConfig: Could not save cleaned configuration immediately due to file lock, will retry later");
          // Schedule a delayed save to retry when the file is no longer locked
          System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
          {
            try
            {
              cfg.Save();
              Program.Log("UserConfig: Successfully saved cleaned configuration on retry");
            }
            catch (Exception retryEx)
            {
              Program.Log("UserConfig: Retry save also failed", retryEx);
            }
          });
        }
        catch (Exception ex)
        {
          Program.Log("UserConfig: Failed to save cleaned configuration", ex);
        }
      }
    }


    private void EnsureBuiltInSourceFileColumn()
    {
      var breakdownTables = new[] { "Products", "Parts", "Subassemblies", "Sheets" };

      foreach (var tableName in breakdownTables)
      {
        // Check if source file virtual column already exists for this table
        var existing = VirtualColumns.Find(vc =>
          string.Equals(vc.TableName, tableName, StringComparison.OrdinalIgnoreCase) &&
          string.Equals(vc.ColumnName, "_SourceFile", StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
          // Add built-in source file virtual column
          var sourceFileColumn = new VirtualColumnDef
          {
            TableName = tableName,
            ColumnName = "_SourceFile",
            ColumnType = "BuiltIn", // Special type for built-in columns
            ButtonText = "Work Order",
            ActionType = "SourceFile"
          };

          VirtualColumns.Add(sourceFileColumn);

          // Ensure it appears as the first column in the order
          EnsureSourceFileColumnFirst(tableName);
        }
      }
    }

    private void EnsureSourceFileColumnFirst(string tableName)
    {
      var orderEntry = ColumnOrders.Find(co => string.Equals(co.TableName, tableName, StringComparison.OrdinalIgnoreCase));

      if (orderEntry != null && orderEntry.Columns != null)
      {
        // Remove _SourceFile if it exists elsewhere in the list
        orderEntry.Columns.RemoveAll(col => string.Equals(col, "_SourceFile", StringComparison.OrdinalIgnoreCase));

        // Insert at the beginning
        orderEntry.Columns.Insert(0, "_SourceFile");
      }
      else
      {
        // Create new order entry with _SourceFile as first column
        if (orderEntry == null)
        {
          orderEntry = new ColumnOrderEntry { TableName = tableName, Columns = new List<string>() };
          ColumnOrders.Add(orderEntry);
        }

        if (orderEntry.Columns == null)
        {
          orderEntry.Columns = new List<string>();
        }

        orderEntry.Columns.Insert(0, "_SourceFile");
      }
    }

    public List<VirtualColumnDef> GetVirtualColumnsForTable(string tableName)
    {
      if (string.IsNullOrWhiteSpace(tableName)) return new List<VirtualColumnDef>();
      return VirtualColumns.FindAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
    }

    public void UpsertVirtualColumn(VirtualColumnDef def)
    {
      if (def == null) return;
      if (string.IsNullOrWhiteSpace(def.TableName) || string.IsNullOrWhiteSpace(def.ColumnName)) return;
      var existing = VirtualColumns.Find(e => string.Equals(e.TableName, def.TableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, def.ColumnName, StringComparison.OrdinalIgnoreCase));
      if (existing == null)
      {
        VirtualColumns.Add(def);
      }
      else
      {
        // Update all properties for both lookup and action columns
        existing.ColumnType = def.ColumnType;
        existing.TargetTableName = def.TargetTableName;
        existing.LocalKeyColumn = def.LocalKeyColumn;
        existing.TargetKeyColumn = def.TargetKeyColumn;
        existing.TargetValueColumn = def.TargetValueColumn;
        existing.ActionType = def.ActionType;
        existing.ButtonText = def.ButtonText;
        existing.ButtonIcon = def.ButtonIcon;
      }
    }

    public void RemoveVirtualColumn(string tableName, string columnName)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      VirtualColumns.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
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

      if (isVisible)
      {
        // Only store hidden columns to prevent excessive data - remove visible column entries
        if (entry != null)
        {
          ColumnVisibilities.Remove(entry);
        }
      }
      else
      {
        // Store hidden columns
        if (entry == null)
        {
          entry = new ColumnVisibilityEntry { TableName = tableName, ColumnName = columnName, IsVisible = false };
          ColumnVisibilities.Add(entry);
        }
        else
        {
          entry.IsVisible = false;
        }
      }
    }

    public string TryGetColumnHeaderText(string tableName, string columnName)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return null;
      var entry = ColumnHeaders.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      var text = entry?.HeaderText;
      return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public void SetColumnHeaderText(string tableName, string columnName, string headerText)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      var entry = ColumnHeaders.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

      if (string.IsNullOrWhiteSpace(headerText))
      {
        // Remove entry if no header text is set to keep settings.xml clean
        if (entry != null)
        {
          ColumnHeaders.Remove(entry);
        }
      }
      else
      {
        if (entry == null)
        {
          entry = new ColumnHeaderEntry { TableName = tableName, ColumnName = columnName };
          ColumnHeaders.Add(entry);
        }
        entry.HeaderText = headerText;
      }
    }

    public string TryGetColumnHeaderToolTip(string tableName, string columnName)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return null;
      var entry = ColumnHeaders.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      var tip = entry?.ToolTip;
      return string.IsNullOrWhiteSpace(tip) ? null : tip;
    }

    public void SetColumnHeaderToolTip(string tableName, string columnName, string toolTip)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      var entry = ColumnHeaders.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

      if (string.IsNullOrWhiteSpace(toolTip))
      {
        // Remove entry if no tooltip is set to keep settings.xml clean
        if (entry != null)
        {
          ColumnHeaders.Remove(entry);
        }
      }
      else
      {
        if (entry == null)
        {
          entry = new ColumnHeaderEntry { TableName = tableName, ColumnName = columnName };
          ColumnHeaders.Add(entry);
        }
        entry.ToolTip = toolTip;
      }
    }

    // Column color management methods
    public System.Drawing.Color? TryGetColumnBackColor(string tableName, string columnName)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return null;
      var entry = ColumnColors.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      if (entry != null && entry.BackColorArgb != 0)
      {
        return System.Drawing.Color.FromArgb(entry.BackColorArgb);
      }
      return null;
    }

    public System.Drawing.Color? TryGetColumnForeColor(string tableName, string columnName)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return null;
      var entry = ColumnColors.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      if (entry != null && entry.ForeColorArgb != 0)
      {
        return System.Drawing.Color.FromArgb(entry.ForeColorArgb);
      }
      return null;
    }

    public void SetColumnBackColor(string tableName, string columnName, System.Drawing.Color? backColor)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      var entry = ColumnColors.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      if (entry == null)
      {
        entry = new ColumnColorEntry { TableName = tableName, ColumnName = columnName };
        ColumnColors.Add(entry);
      }
      entry.BackColorArgb = backColor.HasValue ? backColor.Value.ToArgb() : 0;

      // Remove entry if no colors are set to keep settings.xml clean
      if (entry.BackColorArgb == 0 && entry.ForeColorArgb == 0)
      {
        ColumnColors.Remove(entry);
      }
    }

    public void SetColumnForeColor(string tableName, string columnName, System.Drawing.Color? foreColor)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      var entry = ColumnColors.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      if (entry == null)
      {
        entry = new ColumnColorEntry { TableName = tableName, ColumnName = columnName };
        ColumnColors.Add(entry);
      }
      entry.ForeColorArgb = foreColor.HasValue ? foreColor.Value.ToArgb() : 0;

      // Remove entry if no colors are set to keep settings.xml clean
      if (entry.BackColorArgb == 0 && entry.ForeColorArgb == 0)
      {
        ColumnColors.Remove(entry);
      }
    }

    public void SetColumnColors(string tableName, string columnName, System.Drawing.Color? backColor, System.Drawing.Color? foreColor)
    {
      if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return;
      var entry = ColumnColors.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(e.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
      if (entry == null)
      {
        entry = new ColumnColorEntry { TableName = tableName, ColumnName = columnName };
        ColumnColors.Add(entry);
      }
      entry.BackColorArgb = backColor.HasValue ? backColor.Value.ToArgb() : 0;
      entry.ForeColorArgb = foreColor.HasValue ? foreColor.Value.ToArgb() : 0;

      // Remove entry if no colors are set to keep settings.xml clean
      if (entry.BackColorArgb == 0 && entry.ForeColorArgb == 0)
      {
        ColumnColors.Remove(entry);
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

    // Path to a preserved default baseline settings file
    private static string GetDefaultConfigPath()
    {
      try
      {
        var userDir = GetUserConfigDirectory();
        if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
      }
      catch { }
      return Path.Combine(GetUserConfigDirectory(), "settings.default.xml");
    }

    // Ensure we have a copy of the original default settings to revert to
    public static void EnsureDefaultBaseline()
    {
      lock (configFileLock)
      {
        try
        {
          var baselinePath = GetDefaultConfigPath();
          if (File.Exists(baselinePath)) return; // already preserved

          // Prefer the shipped settings.xml alongside the executable
          var originalPath = Path.Combine(GetOriginalConfigDirectory(), "settings.xml");
          if (File.Exists(originalPath))
          {
            File.Copy(originalPath, baselinePath, true);
            Program.Log("EnsureDefaultBaseline: copied shipped settings.xml to baseline");
            return;
          }

          // Fallback: create a baseline from hardcoded defaults
          Program.Log("EnsureDefaultBaseline: creating baseline from default layout");
          var def = new UserConfig();
          def.EnsureDefaultMetricsLayout();
          var ser = new XmlSerializer(typeof(UserConfig));
          using (var fs = File.Create(baselinePath))
          {
            ser.Serialize(fs, def);
          }
        }
        catch (Exception ex)
        {
          Program.Log("EnsureDefaultBaseline failed", ex);
        }
      }
    }

    // Load the preserved default baseline configuration
    public static UserConfig LoadDefaultBaseline()
    {
      try
      {
        EnsureDefaultBaseline();
        var baselinePath = GetDefaultConfigPath();
        if (File.Exists(baselinePath))
        {
          var ser = new XmlSerializer(typeof(UserConfig));
          using (var fs = File.OpenRead(baselinePath))
          {
            var cfg = (UserConfig)ser.Deserialize(fs);
            cfg.EnsureDefaultMetricsLayout();
            return cfg;
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("LoadDefaultBaseline failed", ex);
      }
      var fallback = new UserConfig();
      fallback.EnsureDefaultMetricsLayout();
      return fallback;
    }

    // Static cached instance to avoid repeated loading
    private static UserConfig cachedInstance;
    private static readonly object cacheLock = new object();

    public static UserConfig Load(string fallbackRoot, string fallbackOutput, string fallbackSdf)
    {
      lock (cacheLock)
      {
        if (cachedInstance != null)
        {
          return cachedInstance;
        }

        lock (configFileLock)
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
                cachedInstance = cfg;
                return cfg;
              }
            }
          }
          catch { }

          var newConfig = new UserConfig
          {
            DefaultRoot = fallbackRoot,
            DefaultOutput = fallbackOutput,
            SdfFileName = fallbackSdf,
          };
          cachedInstance = newConfig;
          return newConfig;
        }
      }
    }

    private static readonly object configFileLock = new object();

    public static UserConfig LoadOrDefault()
    {
      lock (cacheLock)
      {
        if (cachedInstance != null)
        {
          return cachedInstance;
        }

        lock (configFileLock)
        {
          try
          {
            // Ensure we have a baseline default file preserved for resets
            EnsureDefaultBaseline();
            var path = GetConfigPath();
            if (File.Exists(path))
            {
              // Check if file is empty or corrupted
              var fileInfo = new FileInfo(path);
              if (fileInfo.Length == 0)
              {
                Program.Log("Settings file is empty, recreating with defaults");
                File.Delete(path);
              }
              else
              {
                try
                {
                  var ser = new XmlSerializer(typeof(UserConfig));
                  using (var fs = File.OpenRead(path))
                  {
                    var cfg = (UserConfig)ser.Deserialize(fs);
                    cfg.EnsureDefaultMetricsLayout();

                    // Clean up any duplicates that may have been introduced during XML deserialization
                    CleanupDuplicatesAfterDeserialization(cfg);

                    cachedInstance = cfg;
                    return cfg;
                  }
                }
                catch (Exception xmlEx)
                {
                  Program.Log("XML parsing failed, recreating corrupted settings.xml", xmlEx);
                  try
                  {
                    File.Delete(path);
                  }
                  catch { }
                }
              }
            }

            // Create default settings file if it doesn't exist or was corrupted
            Program.Log("Creating default settings.xml");
            var defaultConfig = new UserConfig();
            defaultConfig.EnsureDefaultMetricsLayout();
            defaultConfig.Save(); // Create the file with defaults
            return defaultConfig;
          }
          catch (Exception ex)
          {
            Program.Log("Error loading UserConfig", ex);
          }
          var created = new UserConfig();
          created.EnsureDefaultMetricsLayout();
          cachedInstance = created;
          return created;
        }
      }
    }

    // Reset only the column-related preferences for a single table to defaults
    public void ResetColumnsToDefaultsForTable(string tableName)
    {
      if (string.IsNullOrWhiteSpace(tableName)) return;
      try
      {
        var def = LoadDefaultBaseline();

        // Replace widths for this table
        this.ColumnWidths.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        this.ColumnWidths.AddRange(def.ColumnWidths.FindAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)));

        // Replace order for this table
        this.ColumnOrders.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        var defOrder = def.ColumnOrders.Find(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        if (defOrder != null)
        {
          this.ColumnOrders.Add(new ColumnOrderEntry
          {
            TableName = defOrder.TableName,
            Columns = new List<string>(defOrder.Columns ?? new List<string>())
          });
        }

        // Visibility: clear to default (only hidden columns are persisted)
        this.ColumnVisibilities.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));

        // Headers: replace with defaults for this table
        this.ColumnHeaders.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        this.ColumnHeaders.AddRange(def.ColumnHeaders.FindAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)));

        // Colors: replace with defaults for this table
        this.ColumnColors.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        this.ColumnColors.AddRange(def.ColumnColors.FindAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase)));
      }
      catch (Exception ex)
      {
        Program.Log("ResetColumnsToDefaultsForTable failed", ex);
      }
    }

    // Reset column-related preferences for all tables to defaults
    public void ResetColumnsToDefaultsAllTables()
    {
      try
      {
        var def = LoadDefaultBaseline();
        this.ColumnWidths = new List<ColumnWidthEntry>(def.ColumnWidths ?? new List<ColumnWidthEntry>());
        this.ColumnOrders = new List<ColumnOrderEntry>(def.ColumnOrders ?? new List<ColumnOrderEntry>());
        this.ColumnVisibilities = new List<ColumnVisibilityEntry>(def.ColumnVisibilities ?? new List<ColumnVisibilityEntry>());
        this.ColumnHeaders = new List<ColumnHeaderEntry>(def.ColumnHeaders ?? new List<ColumnHeaderEntry>());
        this.ColumnColors = new List<ColumnColorEntry>(def.ColumnColors ?? new List<ColumnColorEntry>());
      }
      catch (Exception ex)
      {
        Program.Log("ResetColumnsToDefaultsAllTables failed", ex);
      }
    }

    // Method to clear the cached instance (useful for testing or when config changes externally)
    public static void ClearCache()
    {
      lock (cacheLock)
      {
        cachedInstance = null;
      }
    }

    // Update management methods
    public void SetSkippedVersion(string version)
    {
      SkippedVersion = version ?? string.Empty;
      LastUpdateCheck = DateTime.Now;
    }

    public bool IsVersionSkipped(string version)
    {
      return !string.IsNullOrEmpty(SkippedVersion) &&
             string.Equals(SkippedVersion, version, StringComparison.OrdinalIgnoreCase);
    }

    public bool ShouldCheckForUpdates()
    {
      // Check for updates at most once per day
      return (DateTime.Now - LastUpdateCheck).TotalDays >= 1.0;
    }

    public void Save()
    {
      lock (configFileLock)
      {
        try
        {
          var dir = GetConfigDirectory();
          if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
          var path = GetConfigPath();
          var ser = new XmlSerializer(typeof(UserConfig));

          // Use a temporary file to prevent corruption during write
          var tempPath = path + ".tmp";
          using (var fs = File.Create(tempPath))
          {
            ser.Serialize(fs, this);
          }

          // Atomic move to replace the original file
          if (File.Exists(path))
          {
            File.Delete(path);
          }
          File.Move(tempPath, path);

          // Clear cache after saving to ensure fresh data on next load
          lock (cacheLock)
          {
            cachedInstance = null;
          }
        }
        catch (Exception ex)
        {
          Program.Log("Saving UserConfig failed", ex);
          // Clean up temp file if it exists
          try
          {
            var tempPath = GetConfigPath() + ".tmp";
            if (File.Exists(tempPath))
              File.Delete(tempPath);
          }
          catch { }
        }
      }
    }
  }
}
