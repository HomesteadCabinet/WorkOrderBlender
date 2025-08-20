
## AI Prompt: Create Advanced DataGrid Management System with Virtual Columns, Staging, and User Configuration

Create a comprehensive data grid management system for Windows Forms applications that provides advanced features including virtual columns, data staging, user configuration persistence, and sophisticated column management. The system should integrate seamlessly with SQL Server CE databases and provide a professional user experience.

### Core System Architecture

**1. Main DataGrid Management**
- Create a `MetricsGrid` class that extends DataGridView with advanced functionality
- Implement comprehensive event handling for column operations
- Support both read-only and edit modes with seamless transitions
- Handle data binding with BindingSource and DataTable integration
- Provide context-sensitive right-click menus for different grid areas

**2. Virtual Column System**
- Implement a `VirtualColumnsDialog` for managing virtual column definitions
- Support two types of virtual columns:
  - **Lookup Columns**: Display data from related tables using foreign key relationships
  - **Action Columns**: Provide interactive buttons for row-specific operations
- Create `VirtualColumnDef` class with properties:
  - ColumnName, ColumnType, TableName
  - LocalKeyColumn, TargetTableName, TargetKeyColumn, TargetValueColumn
  - ActionType, ButtonText, ButtonIcon
  - IsBuiltInColumn, IsLookupColumn, IsActionColumn flags

**3. Column Management System**
- **Resizing**: Track and persist user-adjusted column widths
- **Reordering**: Support drag-and-drop column reordering with persistence
- **Visibility**: Toggle column visibility with context menu options
- **Styling**: Apply distinct visual styling for different column types
- **Context Menus**: Right-click column headers for quick operations

### Styling System with Variables

**4. Centralized Styling Configuration**
- Create a `GridStylingConfig` class for centralized style management
- Define all colors, fonts, and visual properties as static constants
- Support theme switching and customization
- Implement consistent styling across all grid components

**5. Color Scheme Variables**
```csharp
public static class GridStylingConfig
{
    // Primary color scheme
    public static readonly Color PrimaryBackground = Color.FromArgb(248, 248, 255);
    public static readonly Color PrimaryForeground = Color.FromArgb(30, 30, 30);
    public static readonly Color PrimaryBorder = Color.FromArgb(200, 200, 200);

    // Virtual column styling
    public static readonly Color VirtualLookupBackground = Color.FromArgb(255, 255, 224); // Light yellow
    public static readonly Color VirtualLookupForeground = Color.FromArgb(47, 84, 150);   // Dark slate gray
    public static readonly Color VirtualActionBackground = Color.FromArgb(230, 240, 255); // Light blue
    public static readonly Color VirtualActionForeground = Color.FromArgb(0, 0, 139);     // Dark blue

    // Status and state colors
    public static readonly Color ModifiedCellBackground = Color.FromArgb(255, 255, 200); // Light yellow
    public static readonly Color ReadOnlyBackground = Color.FromArgb(245, 245, 245);      // Light gray
    public static readonly Color SelectedRowBackground = Color.FromArgb(0, 120, 215);     // Windows blue
    public static readonly Color SelectedRowForeground = Color.White;

    // Header styling
    public static readonly Color HeaderBackground = Color.FromArgb(240, 240, 240);
    public static readonly Color HeaderForeground = Color.FromArgb(0, 0, 0);
    public static readonly Color HeaderBorder = Color.FromArgb(180, 180, 180);

    // Grid lines and borders
    public static readonly Color GridLineColor = Color.FromArgb(220, 220, 220);
    public static readonly Color CellBorderColor = Color.FromArgb(200, 200, 200);
    public static readonly Color FocusBorderColor = Color.FromArgb(0, 120, 215);
}
```

**6. Font Configuration Variables**
```csharp
public static class GridStylingConfig
{
    // Font definitions
    public static readonly Font DefaultFont = new Font("Segoe UI", 9F, FontStyle.Regular);
    public static readonly Font HeaderFont = new Font("Segoe UI", 9F, FontStyle.Bold);
    public static readonly Font VirtualColumnFont = new Font("Segoe UI", 9F, FontStyle.Italic);
    public static readonly Font ActionButtonFont = new Font("Segoe UI", 8F, FontStyle.Bold);

    // Font sizes for different elements
    public static readonly float DefaultFontSize = 9F;
    public static readonly float HeaderFontSize = 9F;
    public static readonly float VirtualColumnFontSize = 9F;
    public static readonly float ActionButtonFontSize = 8F;

    // Font styles
    public static readonly FontStyle DefaultFontStyle = FontStyle.Regular;
    public static readonly FontStyle HeaderFontStyle = FontStyle.Bold;
    public static readonly FontStyle VirtualColumnFontStyle = FontStyle.Italic;
    public static readonly FontStyle ActionButtonFontStyle = FontStyle.Bold;
}
```

**7. Spacing and Layout Variables**
```csharp
public static class GridStylingConfig
{
    // Padding and margins
    public static readonly Padding CellPadding = new Padding(4, 2, 4, 2);
    public static readonly Padding HeaderPadding = new Padding(6, 4, 6, 4);
    public static readonly Padding ActionButtonPadding = new Padding(8, 4, 8, 4);

    // Border widths
    public static readonly int CellBorderWidth = 1;
    public static readonly int HeaderBorderWidth = 1;
    public static readonly int FocusBorderWidth = 2;

    // Minimum dimensions
    public static readonly int MinimumColumnWidth = 50;
    public static readonly int MinimumRowHeight = 20;
    public static readonly int DefaultColumnWidth = 120;
    public static readonly int DefaultRowHeight = 24;
}
```

**8. Style Application Methods**
```csharp
public static class GridStylingConfig
{
    // Apply consistent styling to DataGridView
    public static void ApplyDefaultStyling(DataGridView grid)
    {
        grid.BackgroundColor = PrimaryBackground;
        grid.ForegroundColor = PrimaryForeground;
        grid.GridColor = GridLineColor;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        // Apply default fonts
        grid.DefaultCellStyle.Font = DefaultFont;
        grid.ColumnHeadersDefaultCellStyle.Font = HeaderFont;
        grid.RowHeadersDefaultCellStyle.Font = DefaultFont;
    }

    // Style virtual columns consistently
    public static void StyleVirtualColumn(DataGridViewColumn column, bool isActionColumn)
    {
        if (isActionColumn)
        {
            column.DefaultCellStyle.BackColor = VirtualActionBackground;
            column.DefaultCellStyle.ForeColor = VirtualActionForeground;
            column.DefaultCellStyle.Font = VirtualColumnFont;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
        else
        {
            column.DefaultCellStyle.BackColor = VirtualLookupBackground;
            column.DefaultCellStyle.ForeColor = VirtualLookupForeground;
            column.DefaultCellStyle.Font = VirtualColumnFont;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        column.ReadOnly = true;
    }

    // Style modified cells
    public static void StyleModifiedCell(DataGridViewCell cell)
    {
        cell.Style.BackColor = ModifiedCellBackground;
        cell.Style.Font = new Font(DefaultFont, FontStyle.Bold);
    }
}
```

**9. Theme Support and Customization**
```csharp
public static class GridStylingConfig
{
    // Theme definitions
    public enum Theme
    {
        Default,
        Dark,
        HighContrast,
        Custom
    }

    private static Theme currentTheme = Theme.Default;

    // Theme switching
    public static void ApplyTheme(Theme theme, DataGridView grid)
    {
        currentTheme = theme;
        switch (theme)
        {
            case Theme.Dark:
                ApplyDarkTheme(grid);
                break;
            case Theme.HighContrast:
                ApplyHighContrastTheme(grid);
                break;
            case Theme.Custom:
                ApplyCustomTheme(grid);
                break;
            default:
                ApplyDefaultTheme(grid);
                break;
        }
    }

    // Custom theme support
    public static void SetCustomColors(Color primaryBg, Color primaryFg, Color virtualLookupBg, Color virtualActionBg)
    {
        // Update color variables for custom theme
        PrimaryBackground = primaryBg;
        PrimaryForeground = primaryFg;
        VirtualLookupBackground = virtualLookupBg;
        VirtualActionBackground = virtualActionBg;
    }
}
```

**10. Style Persistence and User Preferences**
```csharp
public static class GridStylingConfig
{
    // Save user's style preferences
    public static void SaveStylePreferences(UserConfig config)
    {
        config.GridTheme = currentTheme.ToString();
        config.CustomPrimaryBackground = PrimaryBackground.ToArgb();
        config.CustomPrimaryForeground = PrimaryForeground.ToArgb();
        config.CustomVirtualLookupBackground = VirtualLookupBackground.ToArgb();
        config.CustomVirtualActionBackground = VirtualActionBackground.ToArgb();
        config.Save();
    }

    // Load user's style preferences
    public static void LoadStylePreferences(UserConfig config)
    {
        if (Enum.TryParse<Theme>(config.GridTheme, out var theme))
        {
            currentTheme = theme;
        }

        if (theme == Theme.Custom)
        {
            PrimaryBackground = Color.FromArgb(config.CustomPrimaryBackground);
            PrimaryForeground = Color.FromArgb(config.CustomPrimaryForeground);
            VirtualLookupBackground = Color.FromArgb(config.CustomVirtualLookupBackground);
            VirtualActionBackground = Color.FromArgb(config.CustomVirtualActionBackground);
        }
    }
}
```

**11. Dynamic Style Updates**
```csharp
public static class GridStylingConfig
{
    // Update all grids when theme changes
    private static readonly List<DataGridView> registeredGrids = new List<DataGridView>();

    public static void RegisterGrid(DataGridView grid)
    {
        if (!registeredGrids.Contains(grid))
        {
            registeredGrids.Add(grid);
            ApplyCurrentTheme(grid);
        }
    }

    public static void UnregisterGrid(DataGridView grid)
    {
        registeredGrids.Remove(grid);
    }

    public static void RefreshAllGrids()
    {
        foreach (var grid in registeredGrids)
        {
            if (grid != null && !grid.IsDisposed)
            {
                ApplyCurrentTheme(grid);
            }
        }
    }
}
```

### Advanced Features

**12. Data Staging and Change Management**
- Implement `InMemoryEditStore` for staging changes before database commit
- Support cell-level change tracking with original vs. modified value comparison
- Provide visual indicators for modified cells using styling variables
- Handle both single-cell and bulk save operations
- Support rollback of staged changes before commit

**13. User Configuration Persistence**
- Create `UserConfig` class with XML serialization
- Persist user preferences in `settings.xml`:
  - Column widths per table/column combination
  - Column order sequences per table
  - Column visibility settings per table/column
  - Virtual column definitions and configurations
  - Style preferences and theme selections
- Implement automatic loading and saving of configurations
- Provide default configurations for new tables

**14. Smart Column Positioning**
- Automatically position virtual columns after LinkID columns when present
- Handle new virtual columns by inserting them at logical positions
- Maintain column order consistency across sessions
- Support manual column positioning with "Move to Index" functionality

### Technical Implementation

**15. Event Handling and Persistence**
```csharp
// Column width changes
private void MetricsGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
{
    if (e?.Column == null || isApplyingLayout) return;
    var key = !string.IsNullOrEmpty(e.Column.DataPropertyName) ?
        e.Column.DataPropertyName : e.Column.Name;
    var cfg = UserConfig.LoadOrDefault();
    cfg.SetColumnWidth(currentSelectedTable, key, e.Column.Width);
    cfg.Save();
}

// Column order changes with debouncing
private void MetricsGrid_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
{
    if (isApplyingLayout) return;
    lastOrderChangeUtc = DateTime.UtcNow;
    if (orderPersistTimer != null && !orderPersistTimer.Enabled)
        orderPersistTimer.Start();
}
```

**16. Virtual Column Integration**
- Dynamically add virtual columns to DataTable during data loading
- Build lookup caches for efficient data retrieval
- Apply distinct styling for virtual columns using styling variables
- Handle virtual column updates when definitions change

**17. Context Menu System**
- **Header Context Menu** (right-click column headers):
  - Move to First/Last
  - Move to Index (with numeric input dialog)
  - Hide/Show columns
  - Column-specific operations
- **Body Context Menu** (right-click data cells):
  - Edit cell value
  - Copy cell content
  - Row-specific actions
  - Virtual column interactions

### Configuration Management

**18. UserConfig Class Structure**
```csharp
[Serializable]
public sealed class UserConfig
{
    public List<ColumnWidthEntry> ColumnWidths { get; set; }
    public List<ColumnOrderEntry> ColumnOrders { get; set; }
    public List<ColumnVisibilityEntry> ColumnVisibilities { get; set; }
    public List<VirtualColumnDef> VirtualColumns { get; set; }

    // Style preferences
    public string GridTheme { get; set; } = "Default";
    public int CustomPrimaryBackground { get; set; }
    public int CustomPrimaryForeground { get; set; }
    public int CustomVirtualLookupBackground { get; set; }
    public int CustomVirtualActionBackground { get; set; }

    // Methods for managing configurations
    public int? TryGetColumnWidth(string tableName, string columnName);
    public void SetColumnWidth(string tableName, string columnName, int width);
    public List<string> TryGetColumnOrder(string tableName);
    public void SetColumnOrder(string tableName, IEnumerable<string> columnNames);
    public bool? TryGetColumnVisibility(string tableName, string columnName);
    public void SetColumnVisibility(string tableName, string columnName, bool isVisible);
}
```

**19. Configuration Persistence**
- Automatic saving of user changes to `settings.xml`
- Load configurations on application startup
- Apply saved configurations when switching between tables
- Handle missing configurations with sensible defaults
- Validate configuration data integrity

### Virtual Column Management

**20. VirtualColumnsDialog Implementation**
- Grid-based interface for managing virtual column definitions
- Add/Edit/Remove virtual column operations
- Validation of column definitions
- Preview of virtual column configurations
- Integration with database schema information

**21. EditVirtualColumnDialog**
- Form-based editor for virtual column properties
- Dynamic UI based on column type (lookup vs. action)
- Dropdown selection for available tables and columns
- Real-time validation of configuration
- Support for both database and preloaded column lists

### Data Staging System

**22. Change Tracking**
- Monitor cell value changes in real-time
- Store original values for comparison
- Visual feedback for modified cells using styling variables
- Support for both immediate and staged saving
- Handle database connection states

**23. Edit Mode Management**
- Toggle between read-only and edit modes
- Update UI indicators for current mode
- Handle edit mode transitions gracefully
- Support for keyboard shortcuts (F2 for edit)
- Maintain edit state across table switches

### Integration Requirements

**24. Database Integration**
- Support SQL Server CE connections
- Handle both consolidated and source databases
- Manage connection states and error handling
- Support for multiple database contexts
- Efficient data loading and caching

**25. MainForm Integration**
- Seamless integration with main application window
- Support for multiple table types (Products, Parts, Subassemblies, Sheets)
- Handle table switching with configuration persistence
- Integrate with application-wide edit store
- Support for both standalone and embedded grid modes

### Advanced Features

**26. Performance Optimization**
- Debounced persistence to avoid excessive file I/O
- Efficient change tracking with minimal memory overhead
- Smart caching of virtual column data
- Lazy loading of configuration data
- Background processing for heavy operations

**27. Error Handling and Recovery**
- Graceful handling of configuration file corruption
- Fallback to default configurations when needed
- Validation of virtual column definitions
- Recovery from failed database operations
- Comprehensive logging for debugging

**28. User Experience Enhancements**
- Smooth animations for column operations
- Visual feedback for all user actions using styling variables
- Consistent styling across different column types
- Intuitive context menu organization
- Keyboard shortcuts for common operations

### Example Usage Patterns

**29. Basic Grid Setup with Styling**
```csharp
private void SetupMetricsGrid()
{
    metricsGrid.AllowUserToResizeColumns = true;
    metricsGrid.AllowUserToReorderColumns = true;
    metricsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    metricsGrid.MultiSelect = false;

    // Apply centralized styling
    GridStylingConfig.ApplyDefaultStyling(metricsGrid);
    GridStylingConfig.RegisterGrid(metricsGrid);

    // Wire up events
    metricsGrid.ColumnWidthChanged += MetricsGrid_ColumnWidthChanged;
    metricsGrid.ColumnDisplayIndexChanged += MetricsGrid_ColumnDisplayIndexChanged;
    metricsGrid.MouseDown += MetricsGrid_MouseDown;

    // Apply user configuration
    ApplyUserConfigToMetricsGrid(currentSelectedTable);
}
```

**30. Virtual Column Management with Styling**
```csharp
public void OpenVirtualColumnsDialog()
{
    using (var dlg = new VirtualColumnsDialog(currentSelectedTable, connection, false, breakdownSchemaColumns))
    {
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            LoadVirtualColumnDefinitions(currentSelectedTable);
            BuildVirtualLookupCaches(currentSelectedTable);
            RebuildVirtualColumns(currentSelectedTable, data);
            ApplyUserConfigToMetricsGrid(currentSelectedTable);

            // Apply consistent styling to new virtual columns
            foreach (DataGridViewColumn col in metricsGrid.Columns)
            {
                if (virtualColumnNames.Contains(col.Name))
                {
                    bool isAction = virtualColumnDefs.Any(vc =>
                        vc.ColumnName == col.Name && vc.IsActionColumn);
                    GridStylingConfig.StyleVirtualColumn(col, isAction);
                }
            }

            metricsGrid?.Refresh();
        }
    }
}
```

### Configuration File Structure

**31. settings.xml Example with Styling**
```xml
<?xml version="1.0"?>
<UserConfig>
  <ColumnWidths>
    <ColumnWidthEntry>
      <TableName>Products</TableName>
      <ColumnName>RoomName</ColumnName>
      <Width>194</Width>
    </ColumnWidthEntry>
  </ColumnWidths>
  <ColumnOrders>
    <ColumnOrderEntry>
      <TableName>Products</TableName>
      <Columns>
        <string>_SourceFile</string>
        <string>RoomName</string>
        <string>Name</string>
      </Columns>
    </ColumnOrderEntry>
  </ColumnOrders>
  <VirtualColumns>
    <VirtualColumnDef>
      <TableName>Products</TableName>
      <ColumnName>MaterialInfo</ColumnName>
      <ColumnType>Lookup</ColumnType>
      <LocalKeyColumn>LinkIDMaterial</LocalKeyColumn>
      <TargetTableName>Materials</TargetTableName>
      <TargetKeyColumn>ID</TargetKeyColumn>
      <TargetValueColumn>Name</TargetValueColumn>
    </VirtualColumnDef>
  </VirtualColumns>
  <!-- Style preferences -->
  <GridTheme>Default</GridTheme>
  <CustomPrimaryBackground>-16777216</CustomPrimaryBackground>
  <CustomPrimaryForeground>-16777216</CustomPrimaryForeground>
  <CustomVirtualLookupBackground>-16711681</CustomVirtualLookupBackground>
  <CustomVirtualActionBackground>-16711681</CustomVirtualActionBackground>
</UserConfig>
```

### Testing and Validation

**32. Testing Requirements**
- Test with various table structures and data types
- Validate configuration persistence across application restarts
- Test virtual column functionality with different database schemas
- Verify edit mode transitions and data staging
- Test error handling and recovery scenarios
- Validate performance with large datasets
- Test theme switching and custom styling

**33. Quality Assurance**
- Comprehensive error handling for all operations
- Input validation for user configurations
- Memory leak prevention in event handlers
- Thread safety for configuration operations
- Performance profiling for large datasets
- Accessibility compliance for all UI elements
- Consistent styling across all grid components

This system should provide a professional, feature-rich data grid experience that rivals commercial applications while maintaining the flexibility and extensibility needed for enterprise use cases. The virtual column system should be particularly powerful, allowing users to create complex data relationships without modifying the underlying database schema. The centralized styling system ensures consistency and makes customization easy for both developers and end users.
```

I've added a comprehensive **"Styling System with Variables"** section (sections 4-11) that provides:

1. **Centralized Styling Configuration** - A `GridStylingConfig` class for managing all visual properties
2. **Color Scheme Variables** - Predefined color constants for consistent theming
3. **Font Configuration Variables** - Typography settings for different grid elements
4. **Spacing and Layout Variables** - Consistent padding, margins, and dimensions
5. **Style Application Methods** - Helper methods to apply styling consistently
6. **Theme Support and Customization** - Multiple themes with easy switching
7. **Style Persistence and User Preferences** - Save/load user style choices
8. **Dynamic Style Updates** - Real-time styling updates across all grids

The styling system integrates seamlessly with the existing functionality and provides:
- Easy customization through simple variable changes
- Theme switching capabilities
- User preference persistence
- Consistent styling across all grid components
- Professional appearance that can be easily modified
