using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// 3D Viewer with comprehensive debug logging for easier issue identification and troubleshooting
// Logging covers: initialization, data loading, component building, rendering, user interactions,
// database operations, view transformations, component visibility, and error conditions.
// All log entries are prefixed with "[3DViewer]" for easy filtering.

namespace WorkOrderBlender
{
  public sealed class Product3DViewer : Form
  {
    private readonly string productId;
    private readonly string databasePath;
    private readonly string sourceTableName;
    private SqlCeConnection connection;

    // 3D Scene Data
    private Product3DData productData;
    private readonly List<Component3D> allComponents = new List<Component3D>();

    // 3D Rendering State
    private float rotationX = -20f; // Start with slight downward angle
    private float rotationY = 30f;  // Start with slight side angle
    private float rotationZ = 0f;
    private float zoomFactor = 1f;
    private PointF panOffset = new PointF(0, 0);
    private Point lastMousePos;
    private bool isDragging;

    // UI Controls
    private Panel viewport3D;
    private TreeView hierarchyTree;
    private Panel controlPanel;
    private ToolStripStatusLabel statusLabel;
    private CheckBox showProductsCheck, showSubassembliesCheck, showPartsCheck, showHardwareCheck;

    public Product3DViewer(string productId, string databasePath, string sourceTableName)
    {
      Program.Log($"[3DViewer] Constructor called - ProductID: {productId}, Database: {databasePath}, SourceTable: {sourceTableName}");

      this.productId = productId;
      this.databasePath = databasePath;
      this.sourceTableName = sourceTableName;

      Program.Log("[3DViewer] Starting InitializeComponent...");
      InitializeComponent();
      Program.Log("[3DViewer] InitializeComponent completed");

      Program.Log("[3DViewer] Starting LoadProductData...");
      LoadProductData();
      Program.Log("[3DViewer] LoadProductData completed");
    }

    private void InitializeComponent()
    {
      Program.Log("[3DViewer] InitializeComponent - Setting up form properties");
      Text = GetWindowTitle(productId, sourceTableName);
      StartPosition = FormStartPosition.CenterParent;
      FormBorderStyle = FormBorderStyle.Sizable;
      Width = 1200;
      Height = 800;
      MinimumSize = new Size(800, 600);

      Program.Log("[3DViewer] InitializeComponent - Creating main split container");
      // Create main split container
      var mainSplit = new SplitContainer
      {
        Dock = DockStyle.Fill,
        SplitterDistance = 300,
        FixedPanel = FixedPanel.Panel1
      };
      Controls.Add(mainSplit);

      Program.Log("[3DViewer] InitializeComponent - Creating left panel and hierarchy tree");
      // === LEFT PANEL: Hierarchy Tree and Controls ===
      var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
      mainSplit.Panel1.Controls.Add(leftPanel);

      // Hierarchy Tree
      hierarchyTree = new TreeView
      {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 9F),
        ShowLines = true,
        ShowPlusMinus = true,
        ShowRootLines = true,
        FullRowSelect = true,
        HideSelection = false
      };
      hierarchyTree.AfterSelect += HierarchyTree_AfterSelect;
      leftPanel.Controls.Add(hierarchyTree);

      Program.Log("[3DViewer] InitializeComponent - Creating control panel");
      // Control Panel
      controlPanel = new Panel
      {
        Dock = DockStyle.Top,
        Height = 180,
        BackColor = Color.LightGray,
        BorderStyle = BorderStyle.Fixed3D
      };
      leftPanel.Controls.Add(controlPanel);

      CreateControlPanel();

      Program.Log("[3DViewer] InitializeComponent - Creating right panel and 3D viewport");
      // === RIGHT PANEL: 3D Viewport ===
      var rightPanel = new Panel { Dock = DockStyle.Fill };
      mainSplit.Panel2.Controls.Add(rightPanel);

      // 3D Viewport
      viewport3D = new Panel
      {
        Dock = DockStyle.Fill,
        BackColor = Color.Black,
        BorderStyle = BorderStyle.Fixed3D
      };
      viewport3D.Paint += Viewport3D_Paint;
      viewport3D.MouseDown += Viewport3D_MouseDown;
      viewport3D.MouseMove += Viewport3D_MouseMove;
      viewport3D.MouseUp += Viewport3D_MouseUp;
      viewport3D.MouseWheel += Viewport3D_MouseWheel;
      rightPanel.Controls.Add(viewport3D);

      Program.Log("[3DViewer] InitializeComponent - Creating toolbar with buttons");
      // Toolbar
      var toolbar = new ToolStrip
      {
        Dock = DockStyle.Top,
        BackColor = Color.LightSteelBlue
      };
      rightPanel.Controls.Add(toolbar);

      // Add toolbar buttons
      toolbar.Items.Add(CreateToolButton("🔄", "Reset View", ResetView));
      toolbar.Items.Add(CreateToolButton("🔍", "Fit to Screen", FitToScreen));
      toolbar.Items.Add(new ToolStripSeparator());
      toolbar.Items.Add(CreateToolButton("📦", "Wireframe", ToggleWireframe));
      toolbar.Items.Add(CreateToolButton("🎨", "Solid", ToggleSolid));
      toolbar.Items.Add(new ToolStripSeparator());
      toolbar.Items.Add(CreateToolButton("📷", "Export Image", ExportImage));

      Program.Log("[3DViewer] InitializeComponent - Creating status bar");
      // === STATUS BAR ===
      var statusBar = new StatusStrip { BackColor = Color.LightGray };
      Controls.Add(statusBar);

      statusLabel = new ToolStripStatusLabel($"Loading Product {productId}...");
      statusBar.Items.Add(statusLabel);

      AcceptButton = null;
      CancelButton = null;
      KeyPreview = true;
      KeyDown += Product3DViewer_KeyDown;

      Program.Log("[3DViewer] InitializeComponent - All UI components created successfully");
    }

    private void LoadProductData()
    {
      Program.Log("[3DViewer] LoadProductData - Starting data load process");
      try
      {
        statusLabel.Text = "Loading product data...";
        Application.DoEvents();

        Program.Log($"[3DViewer] LoadProductData - Validating database path: {databasePath}");
        // Create and open database connection
        if (string.IsNullOrWhiteSpace(databasePath) || !System.IO.File.Exists(databasePath))
        {
          Program.Log($"[3DViewer] LoadProductData - ERROR: Database file not found at path: {databasePath}");
          statusLabel.Text = "Database file not found";
          MessageBox.Show("Database file not found or path is empty.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }

        Program.Log("[3DViewer] LoadProductData - Opening database connection");
        connection = new SqlCeConnection($"Data Source={databasePath};");
        connection.Open();
        Program.Log($"[3DViewer] LoadProductData - Database connection opened successfully: {databasePath}");

        // Load the product hierarchy
        Program.Log("[3DViewer] LoadProductData - Starting LoadProduct3DData");
        productData = LoadProduct3DData(productId);
        Program.Log($"[3DViewer] LoadProductData - Product data loaded: {productData.Name} (ID: {productData.ProductID})");

        Program.Log("[3DViewer] LoadProductData - Building components list");
        BuildComponentsList();
        Program.Log($"[3DViewer] LoadProductData - Components list built with {allComponents.Count} total components");

        Program.Log("[3DViewer] LoadProductData - Building hierarchy tree");
        BuildHierarchyTree();
        Program.Log("[3DViewer] LoadProductData - Hierarchy tree built successfully");

        statusLabel.Text = $"Loaded: {allComponents.Count} components - {productData.Subassemblies.Count} subassemblies, {productData.Parts.Count} parts, {productData.Hardware.Count} hardware, {productData.DrillHoles.Count} drill holes, {productData.Routes.Count} routes";

        // Update window title with loaded data
        Program.Log("[3DViewer] LoadProductData - Updating window title");
        UpdateWindowTitleWithData();

        // Initial view setup
        Program.Log("[3DViewer] LoadProductData - Setting up initial 3D view");
        FitToScreen(null, EventArgs.Empty);
        viewport3D.Invalidate();

        Program.Log($"[3DViewer] LoadProductData - COMPLETED: Product ID: {productId} with {allComponents.Count} components");
      }
      catch (Exception ex)
      {
        Program.Log("[3DViewer] LoadProductData - CRITICAL ERROR during data loading", ex);
        statusLabel.Text = "Error loading data";
        MessageBox.Show($"Error loading product data: {ex.Message}\n\nDatabase: {databasePath}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
    }

    private Product3DData LoadProduct3DData(string productId)
    {
      Program.Log($"[3DViewer] LoadProduct3DData - Starting for ProductID: {productId}, SourceTable: {sourceTableName}");
      var product = new Product3DData { ProductID = productId };

      // Load product details - handle different ID contexts based on source table
      if (sourceTableName.Equals("Products", StringComparison.OrdinalIgnoreCase))
      {
        Program.Log("[3DViewer] LoadProduct3DData - Loading from Products table, productId is actual product LinkID");
        // For Products table: productId is the actual product LinkID
        LoadProductInfo(product, productId);
      }
      else
      {
        Program.Log("[3DViewer] LoadProduct3DData - Loading from non-Products table, productId is component LinkID");
        // For Parts/Subassemblies tables: productId is part/subassembly LinkID
        // Product info will be loaded within the respective Load methods
        product.Name = "Loading..."; // Placeholder
      }

      // Load data based on source table context
      if (sourceTableName.Equals("Products", StringComparison.OrdinalIgnoreCase))
      {
        Program.Log("[3DViewer] LoadProduct3DData - Products context: Loading direct parts and subassemblies");
        // From Products table: focus on direct product-to-parts relationship
        LoadProductDirectParts(product, productId);
        LoadProductSubassemblies(product, productId); // Load subassemblies but with less emphasis
      }
      else if (sourceTableName.Equals("Parts", StringComparison.OrdinalIgnoreCase))
      {
        Program.Log("[3DViewer] LoadProduct3DData - Parts context: Loading single part with drill holes and routes");
        // From Parts table: show only the specific part
        LoadSinglePart(product, productId);
        // Load advanced features for individual parts
        LoadPartDrillHoles(product, productId);
        LoadPartRoutes(product, productId);
      }
      else if (sourceTableName.Equals("Subassemblies", StringComparison.OrdinalIgnoreCase))
      {
        Program.Log("[3DViewer] LoadProduct3DData - Subassemblies context: Loading subassembly and components");
        // From Subassemblies table: show the subassembly and its components
        LoadSubassemblyAndComponents(product, productId);
      }
      else
      {
        Program.Log("[3DViewer] LoadProduct3DData - Default context: Loading full hierarchy");
        // For other tables, load full hierarchy
        LoadProductSubassemblies(product, productId);
        LoadProductParts(product, productId);
        LoadProductHardware(product, productId);
      }

      Program.Log($"[3DViewer] LoadProduct3DData - COMPLETED: {product.Subassemblies.Count} subassemblies, {product.Parts.Count} parts, {product.Hardware.Count} hardware, {product.DrillHoles.Count} drill holes, {product.Routes.Count} routes");
      return product;
    }

    private void LoadProductDirectParts(Product3DData product, string productId)
    {
      Program.Log($"[3DViewer] LoadProductDirectParts - Starting for ProductID: {productId}");
      var initialCount = product.Parts.Count;

      // Focus on parts directly linked to the product via LinkIDProduct
      var partsQuery = @"
        SELECT *,
               CASE WHEN LinkIDSubAssembly IS NOT NULL THEN LinkIDSubAssembly
                    WHEN LinkIDParentSubAssembly IS NOT NULL THEN LinkIDParentSubAssembly
                    ELSE NULL END as ParentSubID
        FROM Parts
        WHERE LinkIDProduct = @productId
        ORDER BY
          CASE WHEN LinkIDSubAssembly IS NULL AND LinkIDParentSubAssembly IS NULL THEN 0 ELSE 1 END,
          Name";

      try
      {
        using (var cmd = new SqlCeCommand(partsQuery, connection))
        {
          cmd.Parameters.AddWithValue("@productId", productId);
          Program.Log($"[3DViewer] LoadProductDirectParts - Executing query for direct parts");

          using (var reader = cmd.ExecuteReader())
          {
            int partCount = 0;
            while (reader.Read())
            {
              var parentSubID = reader["ParentSubID"]?.ToString();
              var isDirectPart = string.IsNullOrEmpty(parentSubID);

              var part = new Part3D
              {
                ID = reader["LinkID"]?.ToString(),
                Name = reader["Name"]?.ToString() ?? "Unknown Part",
                X = GetFloatValue(reader, "BasePointX"),
                Y = GetFloatValue(reader, "BasePointY"),
                Z = GetFloatValue(reader, "BasePointZ"),
                Length = GetFloatValue(reader, "Length"),
                Width = GetFloatValue(reader, "Width"),
                Thickness = GetFloatValue(reader, "Thickness"),
                RotationX = GetFloatValue(reader, "RotationX"),
                RotationY = GetFloatValue(reader, "RotationY"),
                RotationZ = GetFloatValue(reader, "RotationZ"),
                ParentSubassemblyID = parentSubID,
                MaterialName = reader["MaterialName"]?.ToString(),
                IsDirectToProduct = isDirectPart
              };

              product.Parts.Add(part);
              partCount++;

              Program.Log($"[3DViewer] LoadProductDirectParts - Part {partCount}: {part.Name} at ({part.X:F1}, {part.Y:F1}, {part.Z:F1}) size: {part.Length:F1}x{part.Width:F1}x{part.Thickness:F1} {(isDirectPart ? "(DIRECT)" : "(via subassembly)")}");
            }

            Program.Log($"[3DViewer] LoadProductDirectParts - COMPLETED: Loaded {partCount} parts for product {productId}");
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log($"[3DViewer] LoadProductDirectParts - ERROR loading parts for product {productId}", ex);
        throw;
      }
    }

    private void LoadProductSubassemblies(Product3DData product, string productId)
    {
      var subassemblyQuery = "SELECT * FROM Subassemblies WHERE LinkIDParentProduct = @productId ORDER BY Name";
      using (var cmd = new SqlCeCommand(subassemblyQuery, connection))
      {
        cmd.Parameters.AddWithValue("@productId", productId);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            var sub = new Subassembly3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Subassembly",
              X = GetFloatValue(reader, "X"),
              Y = GetFloatValue(reader, "Y"),
              Z = GetFloatValue(reader, "Z"),
              Width = GetFloatValue(reader, "Width"),
              Height = GetFloatValue(reader, "Height"),
              Depth = GetFloatValue(reader, "Depth"),
              Angle = GetFloatValue(reader, "Angle"),
              ParentProductID = productId
            };

            product.Subassemblies.Add(sub);
            Program.Log($"Loaded subassembly: {sub.Name} at ({sub.X}, {sub.Y}, {sub.Z}) size: {sub.Width}x{sub.Height}x{sub.Depth}");
          }
        }
      }
    }

    private void LoadProductParts(Product3DData product, string productId)
    {
      // Legacy method for full parts loading (when not from Products table)
      var partsQuery = @"
        SELECT *,
               CASE WHEN LinkIDSubAssembly IS NOT NULL THEN LinkIDSubAssembly
                    WHEN LinkIDParentSubAssembly IS NOT NULL THEN LinkIDParentSubAssembly
                    ELSE NULL END as ParentSubID
        FROM Parts
        WHERE LinkIDProduct = @productId
        ORDER BY Name";

      using (var cmd = new SqlCeCommand(partsQuery, connection))
      {
        cmd.Parameters.AddWithValue("@productId", productId);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            product.Parts.Add(new Part3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Part",
              X = GetFloatValue(reader, "BasePointX"),
              Y = GetFloatValue(reader, "BasePointY"),
              Z = GetFloatValue(reader, "BasePointZ"),
              Length = GetFloatValue(reader, "Length"),
              Width = GetFloatValue(reader, "Width"),
              Thickness = GetFloatValue(reader, "Thickness"),
              RotationX = GetFloatValue(reader, "RotationX"),
              RotationY = GetFloatValue(reader, "RotationY"),
              RotationZ = GetFloatValue(reader, "RotationZ"),
              ParentSubassemblyID = reader["ParentSubID"]?.ToString(),
              MaterialName = reader["MaterialName"]?.ToString()
            });
          }
        }
      }
    }

        private void LoadProductHardware(Product3DData product, string productId)
    {
      var hardwareQuery = @"
        SELECT *,
               CASE WHEN LinkIDSubAssembly IS NOT NULL THEN LinkIDSubAssembly
                    WHEN LinkIDParentSubAssembly IS NOT NULL THEN LinkIDParentSubAssembly
                    ELSE NULL END as ParentSubID
        FROM Hardware
        WHERE LinkIDProduct = @productId
        ORDER BY Name";

      using (var cmd = new SqlCeCommand(hardwareQuery, connection))
      {
        cmd.Parameters.AddWithValue("@productId", productId);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            product.Hardware.Add(new Hardware3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Hardware",
              X = GetFloatValue(reader, "BasePointX"),
              Y = GetFloatValue(reader, "BasePointY"),
              Z = GetFloatValue(reader, "BasePointZ"),
              Width = GetFloatValue(reader, "Width"),
              Height = GetFloatValue(reader, "Height"),
              Depth = GetFloatValue(reader, "Depth"),
              RotationX = GetFloatValue(reader, "RotationX"),
              RotationY = GetFloatValue(reader, "RotationY"),
              RotationZ = GetFloatValue(reader, "RotationZ"),
              ParentSubassemblyID = reader["ParentSubID"]?.ToString()
            });
          }
        }
      }
    }

    private void LoadSinglePart(Product3DData product, string partId)
    {
      Program.Log($"[3DViewer] LoadSinglePart - Starting for PartID: {partId}");

      // When opened from Parts table, partId is actually the part's LinkID, not the product's LinkID
      var partQuery = @"
        SELECT *,
               CASE WHEN LinkIDSubAssembly IS NOT NULL THEN LinkIDSubAssembly
                    WHEN LinkIDParentSubAssembly IS NOT NULL THEN LinkIDParentSubAssembly
                    ELSE NULL END as ParentSubID
        FROM Parts
        WHERE LinkID = @partId";

      try
      {
        using (var cmd = new SqlCeCommand(partQuery, connection))
        {
          cmd.Parameters.AddWithValue("@partId", partId);
          Program.Log($"[3DViewer] LoadSinglePart - Executing query for part {partId}");

          using (var reader = cmd.ExecuteReader())
          {
            if (reader.Read())
            {
              var parentSubID = reader["ParentSubID"]?.ToString();
              var isDirectPart = string.IsNullOrEmpty(parentSubID);
              var actualProductId = reader["LinkIDProduct"]?.ToString();

              Program.Log($"[3DViewer] LoadSinglePart - Found part, belongs to ProductID: {actualProductId}, ParentSubID: {parentSubID ?? "None"}");

              // Update the product info with the actual product this part belongs to
              if (!string.IsNullOrEmpty(actualProductId))
              {
                product.ProductID = actualProductId;
                Program.Log($"[3DViewer] LoadSinglePart - Loading parent product info for ProductID: {actualProductId}");
                LoadProductInfo(product, actualProductId);
              }

              var part = new Part3D
              {
                ID = reader["LinkID"]?.ToString(),
                Name = reader["Name"]?.ToString() ?? "Unknown Part",
                X = GetFloatValue(reader, "BasePointX"),
                Y = GetFloatValue(reader, "BasePointY"),
                Z = GetFloatValue(reader, "BasePointZ"),
                Length = GetFloatValue(reader, "Length"),
                Width = GetFloatValue(reader, "Width"),
                Thickness = GetFloatValue(reader, "Thickness"),
                RotationX = GetFloatValue(reader, "RotationX"),
                RotationY = GetFloatValue(reader, "RotationY"),
                RotationZ = GetFloatValue(reader, "RotationZ"),
                ParentSubassemblyID = parentSubID,
                MaterialName = reader["MaterialName"]?.ToString(),
                IsDirectToProduct = isDirectPart
              };

              product.Parts.Add(part);

              Program.Log($"[3DViewer] LoadSinglePart - COMPLETED: Loaded part '{part.Name}' at ({part.X:F1}, {part.Y:F1}, {part.Z:F1}) size: {part.Length:F1}x{part.Width:F1}x{part.Thickness:F1}, Material: {part.MaterialName ?? "None"}");
            }
            else
            {
              Program.Log($"[3DViewer] LoadSinglePart - WARNING: Part with ID {partId} not found in Parts table");
            }
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log($"[3DViewer] LoadSinglePart - ERROR loading part {partId}", ex);
        throw;
      }
    }

    private void LoadSubassemblyAndComponents(Product3DData product, string subassemblyId)
    {
      // When opened from Subassemblies table, load the subassembly and its components
      var subQuery = "SELECT * FROM Subassemblies WHERE LinkID = @subId";
      using (var cmd = new SqlCeCommand(subQuery, connection))
      {
        cmd.Parameters.AddWithValue("@subId", subassemblyId);
        using (var reader = cmd.ExecuteReader())
        {
          if (reader.Read())
          {
            var actualProductId = reader["LinkIDParentProduct"]?.ToString();

            // Update the product info
            if (!string.IsNullOrEmpty(actualProductId))
            {
              product.ProductID = actualProductId;
              LoadProductInfo(product, actualProductId);
            }

            var sub = new Subassembly3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Subassembly",
              X = GetFloatValue(reader, "X"),
              Y = GetFloatValue(reader, "Y"),
              Z = GetFloatValue(reader, "Z"),
              Width = GetFloatValue(reader, "Width"),
              Height = GetFloatValue(reader, "Height"),
              Depth = GetFloatValue(reader, "Depth"),
              Angle = GetFloatValue(reader, "Angle"),
              ParentProductID = actualProductId
            };

            product.Subassemblies.Add(sub);
            Program.Log($"Loaded subassembly: {sub.Name} at ({sub.X}, {sub.Y}, {sub.Z}) size: {sub.Width}x{sub.Height}x{sub.Depth}");

            // Load parts belonging to this subassembly
            LoadSubassemblyParts(product, subassemblyId);

            // Load hardware belonging to this subassembly
            LoadSubassemblyHardware(product, subassemblyId);
          }
          else
          {
            Program.Log($"Warning: Subassembly with ID {subassemblyId} not found in Subassemblies table");
          }
        }
      }
    }

    private void LoadProductInfo(Product3DData product, string productId)
    {
      var productQuery = "SELECT * FROM Products WHERE LinkID = @productId";
      using (var cmd = new SqlCeCommand(productQuery, connection))
      {
        cmd.Parameters.AddWithValue("@productId", productId);
        using (var reader = cmd.ExecuteReader())
        {
          if (reader.Read())
          {
            product.Name = reader["Name"]?.ToString() ?? "Unknown Product";
            product.X = GetFloatValue(reader, "X");
            product.Y = GetFloatValue(reader, "Y");
            product.Z = GetFloatValue(reader, "Z");
            product.Width = GetFloatValue(reader, "Width");
            product.Height = GetFloatValue(reader, "Height");
            product.Depth = GetFloatValue(reader, "Depth");
            product.Angle = GetFloatValue(reader, "Angle");
            Program.Log($"Loaded parent product info: {product.Name}");
          }
        }
      }
    }

        private void LoadSubassemblyParts(Product3DData product, string subassemblyId)
    {
      // Corrected relationship: parts.LinkIDParentSubAssembly = subassemblies.LinkID
      var partsQuery = @"
        SELECT *
        FROM Parts
        WHERE LinkIDParentSubAssembly = @subId
        ORDER BY Name";

      using (var cmd = new SqlCeCommand(partsQuery, connection))
      {
        cmd.Parameters.AddWithValue("@subId", subassemblyId);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            var part = new Part3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Part",
              X = GetFloatValue(reader, "BasePointX"),
              Y = GetFloatValue(reader, "BasePointY"),
              Z = GetFloatValue(reader, "BasePointZ"),
              Length = GetFloatValue(reader, "Length"),
              Width = GetFloatValue(reader, "Width"),
              Thickness = GetFloatValue(reader, "Thickness"),
              RotationX = GetFloatValue(reader, "RotationX"),
              RotationY = GetFloatValue(reader, "RotationY"),
              RotationZ = GetFloatValue(reader, "RotationZ"),
              ParentSubassemblyID = subassemblyId,
              MaterialName = reader["MaterialName"]?.ToString(),
              IsDirectToProduct = false
            };

            product.Parts.Add(part);
            Program.Log($"Loaded subassembly part: {part.Name}");
          }
        }
      }

      // Also load nested subassemblies: subassemblies.LinkIDParentSubassembly = subassemblies.LinkID
      LoadNestedSubassemblies(product, subassemblyId);
    }

    private void LoadNestedSubassemblies(Product3DData product, string parentSubassemblyId)
    {
      var nestedSubQuery = "SELECT * FROM Subassemblies WHERE LinkIDParentSubassembly = @parentSubId ORDER BY Name";
      using (var cmd = new SqlCeCommand(nestedSubQuery, connection))
      {
        cmd.Parameters.AddWithValue("@parentSubId", parentSubassemblyId);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            var nestedSub = new Subassembly3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Subassembly",
              X = GetFloatValue(reader, "X"),
              Y = GetFloatValue(reader, "Y"),
              Z = GetFloatValue(reader, "Z"),
              Width = GetFloatValue(reader, "Width"),
              Height = GetFloatValue(reader, "Height"),
              Depth = GetFloatValue(reader, "Depth"),
              Angle = GetFloatValue(reader, "Angle"),
              ParentProductID = reader["LinkIDParentProduct"]?.ToString(),
              ParentSubassemblyID = parentSubassemblyId
            };

            product.Subassemblies.Add(nestedSub);
            Program.Log($"Loaded nested subassembly: {nestedSub.Name} under parent {parentSubassemblyId}");

            // Recursively load parts and subassemblies for this nested subassembly
            LoadSubassemblyParts(product, nestedSub.ID);
            LoadSubassemblyHardware(product, nestedSub.ID);
          }
        }
      }
    }

    private void LoadSubassemblyHardware(Product3DData product, string subassemblyId)
    {
      var hardwareQuery = @"
        SELECT *,
               CASE WHEN LinkIDSubAssembly IS NOT NULL THEN LinkIDSubAssembly
                    WHEN LinkIDParentSubAssembly IS NOT NULL THEN LinkIDParentSubAssembly
                    ELSE NULL END as ParentSubID
        FROM Hardware
        WHERE LinkIDSubAssembly = @subId OR LinkIDParentSubAssembly = @subId
        ORDER BY Name";

      using (var cmd = new SqlCeCommand(hardwareQuery, connection))
      {
        cmd.Parameters.AddWithValue("@subId", subassemblyId);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            var hw = new Hardware3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Hardware",
              X = GetFloatValue(reader, "BasePointX"),
              Y = GetFloatValue(reader, "BasePointY"),
              Z = GetFloatValue(reader, "BasePointZ"),
              Width = GetFloatValue(reader, "Width"),
              Height = GetFloatValue(reader, "Height"),
              Depth = GetFloatValue(reader, "Depth"),
              RotationX = GetFloatValue(reader, "RotationX"),
              RotationY = GetFloatValue(reader, "RotationY"),
              RotationZ = GetFloatValue(reader, "RotationZ"),
              ParentSubassemblyID = subassemblyId
            };

            product.Hardware.Add(hw);
            Program.Log($"Loaded subassembly hardware: {hw.Name}");
          }
        }
      }
    }

    private void LoadPartDrillHoles(Product3DData product, string partId)
    {
      // Load drill holes for the specific part: DrillsVertical.LinkIDPart = part.LinkID
      var drillQuery = @"
        SELECT *
        FROM DrillsVertical
        WHERE LinkIDPart = @partId
        ORDER BY Sequence, Name";

      using (var cmd = new SqlCeCommand(drillQuery, connection))
      {
        cmd.Parameters.AddWithValue("@partId", partId);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            var drill = new DrillHole3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Drill",
              PartID = partId,
              X = GetFloatValue(reader, "X"),
              Y = GetFloatValue(reader, "Y"),
              Z = GetFloatValue(reader, "Z"),
              Diameter = GetFloatValue(reader, "Diameter"),
              Depth = GetFloatValue(reader, "Z"), // Depth is typically the Z value
              DrillType = reader["DrillBitType"]?.ToString(),
              ToolName = reader["ActualToolName"]?.ToString(),
              Face = GetIntValue(reader, "Face")
            };

            product.DrillHoles.Add(drill);
            Program.Log($"Loaded drill hole: {drill.Name} at ({drill.X:F1}, {drill.Y:F1}, {drill.Z:F1}) Ø{drill.Diameter:F1}");
          }
        }
      }
    }

    private void LoadPartRoutes(Product3DData product, string partId)
    {
      // Load routes for the specific part: Routes.LinkIDPart = part.LinkID
      var routeQuery = @"
        SELECT *
        FROM Routes
        WHERE LinkIDPart = @partId
        ORDER BY Sequence, Name";

      using (var cmd = new SqlCeCommand(routeQuery, connection))
      {
        cmd.Parameters.AddWithValue("@partId", partId);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            var route = new Route3D
            {
              ID = reader["LinkID"]?.ToString(),
              Name = reader["Name"]?.ToString() ?? "Unknown Route",
              PartID = partId,
              Diameter = GetFloatValue(reader, "Diameter"),
              Depth = GetFloatValue(reader, "LayerDepth"),
              ToolName = reader["ActualToolName"]?.ToString(),
              Face = GetIntValue(reader, "Face")
            };

            // Load vectors for this route: Vectors.LinkIDRoute = Routes.LinkID
            LoadRouteVectors(route);

            product.Routes.Add(route);
            Program.Log($"Loaded route: {route.Name} with {route.Vectors.Count} vectors, tool Ø{route.Diameter:F1}");
          }
        }
      }
    }

    private void LoadRouteVectors(Route3D route)
    {
      var vectorQuery = @"
        SELECT *
        FROM Vectors
        WHERE LinkIDRoute = @routeId
        ORDER BY VectorIndex, Sequence";

      using (var cmd = new SqlCeCommand(vectorQuery, connection))
      {
        cmd.Parameters.AddWithValue("@routeId", route.ID);
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            var vector = new Vector3D
            {
              ID = reader["LinkID"]?.ToString(),
              RouteID = route.ID,
              X = GetFloatValue(reader, "X"),
              Y = GetFloatValue(reader, "Y"),
              Z = GetFloatValue(reader, "Z"),
              Radius = GetFloatValue(reader, "Radius"),
              Bulge = GetFloatValue(reader, "Bulge"),
              Sequence = GetIntValue(reader, "VectorIndex")
            };

            route.Vectors.Add(vector);
          }
        }
      }
    }

    private int GetIntValue(IDataReader reader, string columnName)
    {
      try
      {
        var value = reader[columnName];
        if (value == null || value == DBNull.Value) return 0;
        return Convert.ToInt32(value);
      }
      catch
      {
        return 0;
      }
    }

    private float GetFloatValue(IDataReader reader, string columnName)
    {
      try
      {
        var value = reader[columnName];
        if (value == null || value == DBNull.Value) return 0f;
        return Convert.ToSingle(value);
      }
      catch
      {
        return 0f;
      }
    }

    private void BuildComponentsList()
    {
      Program.Log("[3DViewer] BuildComponentsList - Starting component list build");
      allComponents.Clear();

      Program.Log($"[3DViewer] BuildComponentsList - Adding main product: {productData.Name}");
      // Add the main product as a component
      allComponents.Add(new Component3D
      {
        ID = productData.ProductID,
        Name = productData.Name,
        Type = ComponentType.Product,
        Position = new Point3D(productData.X, productData.Y, productData.Z),
        Dimensions = new Size3D(productData.Width, productData.Height, productData.Depth),
        Rotation = new Rotation3D(0, 0, productData.Angle),
        Color = Color.DarkBlue,
        IsVisible = showProductsCheck?.Checked ?? true
      });

      // Add subassemblies
      Program.Log($"[3DViewer] BuildComponentsList - Adding {productData.Subassemblies.Count} subassemblies");
      foreach (var sub in productData.Subassemblies)
      {
        allComponents.Add(new Component3D
        {
          ID = sub.ID,
          Name = sub.Name,
          Type = ComponentType.Subassembly,
          Position = new Point3D(sub.X, sub.Y, sub.Z),
          Dimensions = new Size3D(sub.Width, sub.Height, sub.Depth),
          Rotation = new Rotation3D(0, 0, sub.Angle),
          Color = Color.Green,
          ParentID = productData.ProductID,
          IsVisible = showSubassembliesCheck?.Checked ?? true
        });
        Program.Log($"[3DViewer] BuildComponentsList - Added subassembly: {sub.Name} at ({sub.X:F1}, {sub.Y:F1}, {sub.Z:F1})");
      }

      // Add parts
      Program.Log($"[3DViewer] BuildComponentsList - Adding {productData.Parts.Count} parts");
      int directParts = 0;
      foreach (var part in productData.Parts)
      {
        if (part.IsDirectToProduct) directParts++;

        var partColor = GetPartColor(part.MaterialName, part.IsDirectToProduct);
        allComponents.Add(new Component3D
        {
          ID = part.ID,
          Name = part.Name,
          Type = ComponentType.Part,
          Position = new Point3D(part.X, part.Y, part.Z),
          Dimensions = new Size3D(part.Length, part.Width, part.Thickness),
          Rotation = new Rotation3D(part.RotationX, part.RotationY, part.RotationZ),
          Color = partColor,
          ParentID = part.ParentSubassemblyID ?? productData.ProductID,
          IsVisible = showPartsCheck?.Checked ?? true,
          MaterialName = part.MaterialName,
          IsDirectToProduct = part.IsDirectToProduct
        });
        Program.Log($"[3DViewer] BuildComponentsList - Added part: {part.Name} at ({part.X:F1}, {part.Y:F1}, {part.Z:F1}) {(part.IsDirectToProduct ? "(DIRECT)" : "")}");
      }
      Program.Log($"[3DViewer] BuildComponentsList - Parts summary: {directParts} direct to product, {productData.Parts.Count - directParts} via subassembly");

      // Add hardware
      Program.Log($"[3DViewer] BuildComponentsList - Adding {productData.Hardware.Count} hardware components");
      foreach (var hw in productData.Hardware)
      {
        allComponents.Add(new Component3D
        {
          ID = hw.ID,
          Name = hw.Name,
          Type = ComponentType.Hardware,
          Position = new Point3D(hw.X, hw.Y, hw.Z),
          Dimensions = new Size3D(hw.Width, hw.Height, hw.Depth),
          Rotation = new Rotation3D(hw.RotationX, hw.RotationY, hw.RotationZ),
          Color = Color.Orange,
          ParentID = hw.ParentSubassemblyID ?? productData.ProductID,
          IsVisible = showHardwareCheck?.Checked ?? true
        });
        Program.Log($"[3DViewer] BuildComponentsList - Added hardware: {hw.Name} at ({hw.X:F1}, {hw.Y:F1}, {hw.Z:F1})");
      }

      // Add drill holes
      Program.Log($"[3DViewer] BuildComponentsList - Adding {productData.DrillHoles.Count} drill holes");
      foreach (var drill in productData.DrillHoles)
      {
        allComponents.Add(new Component3D
        {
          ID = drill.ID,
          Name = drill.Name,
          Type = ComponentType.DrillHole,
          Position = new Point3D(drill.X, drill.Y, drill.Z),
          Dimensions = new Size3D(drill.Diameter, drill.Diameter, drill.Depth),
          Color = Color.Red,
          ParentID = drill.PartID,
          IsVisible = true // Always show drill holes when loaded
        });
      }

      // Add routes
      Program.Log($"[3DViewer] BuildComponentsList - Adding {productData.Routes.Count} CNC routes");
      foreach (var route in productData.Routes)
      {
        allComponents.Add(new Component3D
        {
          ID = route.ID,
          Name = route.Name,
          Type = ComponentType.Route,
          Position = new Point3D(0, 0, 0), // Routes are path-based, not position-based
          Dimensions = new Size3D(route.Diameter, route.Diameter, route.Depth),
          Color = Color.Orange,
          ParentID = route.PartID,
          IsVisible = true // Always show routes when loaded
        });
      }

      Program.Log($"[3DViewer] BuildComponentsList - COMPLETED: Total {allComponents.Count} components added to scene");
    }

    private Color GetPartColor(string materialName, bool isDirectToProduct = false)
    {
      Color baseColor;

      if (string.IsNullOrEmpty(materialName))
      {
        baseColor = Color.LightBlue;
      }
      else
      {
        // Color coding based on material name
        var material = materialName.ToLowerInvariant();
        if (material.Contains("wood") || material.Contains("mdf")) baseColor = Color.SandyBrown;
        else if (material.Contains("metal") || material.Contains("steel")) baseColor = Color.Silver;
        else if (material.Contains("plastic")) baseColor = Color.LightGray;
        else if (material.Contains("glass")) baseColor = Color.LightCyan;
        else baseColor = Color.LightBlue;
      }

      // When opened from Products table, highlight direct parts with more vibrant colors
      if (sourceTableName.Equals("Products", StringComparison.OrdinalIgnoreCase) && isDirectToProduct)
      {
        // Make direct parts more vibrant/saturated to emphasize the direct product-to-part relationship
        var hsb = ColorToHSB(baseColor);
        hsb.Saturation = Math.Min(1.0f, hsb.Saturation * 1.4f); // Increase saturation
        hsb.Brightness = Math.Min(1.0f, hsb.Brightness * 1.1f); // Slightly brighter
        return HSBToColor(hsb);
      }

      return baseColor;
    }

    private HSBColor ColorToHSB(Color color)
    {
      float r = color.R / 255f;
      float g = color.G / 255f;
      float b = color.B / 255f;

      float max = Math.Max(r, Math.Max(g, b));
      float min = Math.Min(r, Math.Min(g, b));
      float diff = max - min;

      float hue = 0;
      if (diff != 0)
      {
        if (max == r) hue = (g - b) / diff;
        else if (max == g) hue = (b - r) / diff + 2;
        else hue = (r - g) / diff + 4;
        hue *= 60;
        if (hue < 0) hue += 360;
      }

      float saturation = max == 0 ? 0 : diff / max;
      float brightness = max;

      return new HSBColor { Hue = hue, Saturation = saturation, Brightness = brightness };
    }

    private Color HSBToColor(HSBColor hsb)
    {
      float h = hsb.Hue / 360f;
      float s = hsb.Saturation;
      float v = hsb.Brightness;

      float r, g, b;
      if (s == 0)
      {
        r = g = b = v;
      }
      else
      {
        int i = (int)(h * 6);
        float f = h * 6 - i;
        float p = v * (1 - s);
        float q = v * (1 - s * f);
        float t = v * (1 - s * (1 - f));

        switch (i % 6)
        {
          case 0: r = v; g = t; b = p; break;
          case 1: r = q; g = v; b = p; break;
          case 2: r = p; g = v; b = t; break;
          case 3: r = p; g = q; b = v; break;
          case 4: r = t; g = p; b = v; break;
          default: r = v; g = p; b = q; break;
        }
      }

      return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private struct HSBColor
    {
      public float Hue;
      public float Saturation;
      public float Brightness;
    }

    private void BuildHierarchyTree()
    {
      hierarchyTree.Nodes.Clear();

      // Root node for the product
      var productNode = new TreeNode($"📦 {productData.Name} (Product)")
      {
        Tag = productData.ProductID,
        ImageIndex = 0
      };
      hierarchyTree.Nodes.Add(productNode);

      // Build hierarchy based on source table context
      if (sourceTableName.Equals("Products", StringComparison.OrdinalIgnoreCase))
      {
        BuildProductFocusedHierarchy(productNode);
      }
      else if (sourceTableName.Equals("Parts", StringComparison.OrdinalIgnoreCase))
      {
        BuildSinglePartHierarchy(productNode);
      }
      else if (sourceTableName.Equals("Subassemblies", StringComparison.OrdinalIgnoreCase))
      {
        BuildSubassemblyFocusedHierarchy(productNode);
      }
      else
      {
        BuildStandardHierarchy(productNode);
      }

      productNode.Expand();
    }

    private void BuildProductFocusedHierarchy(TreeNode productNode)
    {
      // For Products table view: Emphasize direct product-to-parts relationships

      // Add direct parts first (these are the primary focus)
      var directParts = productData.Parts.Where(p => p.IsDirectToProduct);
      if (directParts.Any())
      {
        var directPartsNode = new TreeNode($"🎯 Direct Parts ({directParts.Count()})")
        {
          Tag = "direct_parts_group",
          ForeColor = Color.DarkBlue,
          NodeFont = new Font(hierarchyTree.Font, FontStyle.Bold)
        };
        productNode.Nodes.Add(directPartsNode);

        foreach (var part in directParts.OrderBy(p => p.Name))
        {
          var partNode = new TreeNode($"🧩 {part.Name} ({part.MaterialName ?? "Material N/A"})")
          {
            Tag = part.ID,
            ImageIndex = 2,
            ForeColor = Color.DarkBlue
          };
          directPartsNode.Nodes.Add(partNode);
        }

        directPartsNode.Expand();
      }

      // Add subassemblies with their parts
      foreach (var sub in productData.Subassemblies.OrderBy(s => s.Name))
      {
        var subParts = productData.Parts.Where(p => p.ParentSubassemblyID == sub.ID);
        var subHardware = productData.Hardware.Where(h => h.ParentSubassemblyID == sub.ID);
        var totalSubComponents = subParts.Count() + subHardware.Count();

        var subNode = new TreeNode($"🔧 {sub.Name} ({totalSubComponents} components)")
        {
          Tag = sub.ID,
          ImageIndex = 1,
          ForeColor = Color.DarkGreen
        };
        productNode.Nodes.Add(subNode);

        foreach (var part in subParts.OrderBy(p => p.Name))
        {
          var partNode = new TreeNode($"🧩 {part.Name} ({part.MaterialName ?? "Material N/A"})")
          {
            Tag = part.ID,
            ImageIndex = 2
          };
          subNode.Nodes.Add(partNode);
        }

        foreach (var hw in subHardware.OrderBy(h => h.Name))
        {
          var hwNode = new TreeNode($"⚙️ {hw.Name}")
          {
            Tag = hw.ID,
            ImageIndex = 3
          };
          subNode.Nodes.Add(hwNode);
        }
      }

      // Add any direct hardware
      var directHardware = productData.Hardware.Where(h => string.IsNullOrEmpty(h.ParentSubassemblyID));
      foreach (var hw in directHardware.OrderBy(h => h.Name))
      {
        var hwNode = new TreeNode($"⚙️ {hw.Name} (Direct)")
        {
          Tag = hw.ID,
          ImageIndex = 3
        };
        productNode.Nodes.Add(hwNode);
      }
    }

    private void BuildStandardHierarchy(TreeNode productNode)
    {
      // Standard hierarchy for other table views

      // Add subassemblies
      foreach (var sub in productData.Subassemblies)
      {
        var subNode = new TreeNode($"🔧 {sub.Name} (Subassembly)")
        {
          Tag = sub.ID,
          ImageIndex = 1
        };
        productNode.Nodes.Add(subNode);

        // Add parts belonging to this subassembly
        var subParts = productData.Parts.Where(p => p.ParentSubassemblyID == sub.ID);
        foreach (var part in subParts)
        {
          var partNode = new TreeNode($"🧩 {part.Name} (Part)")
          {
            Tag = part.ID,
            ImageIndex = 2
          };
          subNode.Nodes.Add(partNode);
        }

        // Add hardware belonging to this subassembly
        var subHardware = productData.Hardware.Where(h => h.ParentSubassemblyID == sub.ID);
        foreach (var hw in subHardware)
        {
          var hwNode = new TreeNode($"⚙️ {hw.Name} (Hardware)")
          {
            Tag = hw.ID,
            ImageIndex = 3
          };
          subNode.Nodes.Add(hwNode);
        }
      }

      // Add parts directly under the product
      var directParts = productData.Parts.Where(p => string.IsNullOrEmpty(p.ParentSubassemblyID));
      foreach (var part in directParts)
      {
        var partNode = new TreeNode($"🧩 {part.Name} (Part)")
        {
          Tag = part.ID,
          ImageIndex = 2
        };
        productNode.Nodes.Add(partNode);
      }

      // Add hardware directly under the product
      var directHardware = productData.Hardware.Where(h => string.IsNullOrEmpty(h.ParentSubassemblyID));
      foreach (var hw in directHardware)
      {
        var hwNode = new TreeNode($"⚙️ {hw.Name} (Hardware)")
        {
          Tag = hw.ID,
          ImageIndex = 3
        };
        productNode.Nodes.Add(hwNode);
      }
    }

    private void BuildSinglePartHierarchy(TreeNode productNode)
    {
      // For Parts table view: Show only the specific part
      if (productData.Parts.Count > 0)
      {
        var part = productData.Parts.First(); // Should only be one part
        var partNode = new TreeNode($"🧩 {part.Name} (Individual Part)")
        {
          Tag = part.ID,
          ImageIndex = 2,
          ForeColor = Color.DarkBlue,
          NodeFont = new Font(hierarchyTree.Font, FontStyle.Bold)
        };

        // Add material and size info
        var infoNode = new TreeNode($"📏 Size: {part.Length:F1} × {part.Width:F1} × {part.Thickness:F1}")
        {
          ForeColor = Color.Gray
        };
        partNode.Nodes.Add(infoNode);

        if (!string.IsNullOrEmpty(part.MaterialName))
        {
          var materialNode = new TreeNode($"🎨 Material: {part.MaterialName}")
          {
            ForeColor = Color.Gray
          };
          partNode.Nodes.Add(materialNode);
        }

                var positionNode = new TreeNode($"📍 Position: ({part.X:F1}, {part.Y:F1}, {part.Z:F1})")
        {
          ForeColor = Color.Gray
        };
        partNode.Nodes.Add(positionNode);

        // Add drill holes for this part
        var drillHoles = productData.DrillHoles.Where(d => d.PartID == part.ID);
        if (drillHoles.Any())
        {
          var drillNode = new TreeNode($"🔴 Drill Holes ({drillHoles.Count()})")
          {
            ForeColor = Color.DarkRed,
            NodeFont = new Font(hierarchyTree.Font, FontStyle.Bold)
          };
          partNode.Nodes.Add(drillNode);

          foreach (var drill in drillHoles.OrderBy(d => d.Name))
          {
            var drillDetailNode = new TreeNode($"⊙ {drill.Name} - Ø{drill.Diameter:F1}mm at ({drill.X:F1}, {drill.Y:F1})")
            {
              Tag = drill.ID,
              ForeColor = Color.DarkRed
            };
            drillNode.Nodes.Add(drillDetailNode);
          }
          drillNode.Expand();
        }

        // Add routes for this part
        var routes = productData.Routes.Where(r => r.PartID == part.ID);
        if (routes.Any())
        {
          var routeNode = new TreeNode($"🔶 CNC Routes ({routes.Count()})")
          {
            ForeColor = Color.DarkOrange,
            NodeFont = new Font(hierarchyTree.Font, FontStyle.Bold)
          };
          partNode.Nodes.Add(routeNode);

          foreach (var route in routes.OrderBy(r => r.Name))
          {
            var routeDetailNode = new TreeNode($"↗ {route.Name} - Ø{route.Diameter:F1}mm tool ({route.Vectors.Count} vectors)")
            {
              Tag = route.ID,
              ForeColor = Color.DarkOrange
            };
            routeNode.Nodes.Add(routeDetailNode);
          }
          routeNode.Expand();
        }

        productNode.Nodes.Add(partNode);
        partNode.Expand();

        // Select the part by default
        hierarchyTree.SelectedNode = partNode;
      }
    }

    private void BuildSubassemblyFocusedHierarchy(TreeNode productNode)
    {
      // For Subassemblies table view: Show the subassembly and its components
      if (productData.Subassemblies.Count > 0)
      {
        var subassembly = productData.Subassemblies.First(); // Should only be one subassembly
        var subNode = new TreeNode($"🔧 {subassembly.Name} (Subassembly)")
        {
          Tag = subassembly.ID,
          ImageIndex = 1,
          ForeColor = Color.DarkGreen,
          NodeFont = new Font(hierarchyTree.Font, FontStyle.Bold)
        };
        productNode.Nodes.Add(subNode);

        // Add parts belonging to this subassembly
        var subParts = productData.Parts.Where(p => p.ParentSubassemblyID == subassembly.ID);
        foreach (var part in subParts.OrderBy(p => p.Name))
        {
          var partNode = new TreeNode($"🧩 {part.Name} ({part.MaterialName ?? "Material N/A"})")
          {
            Tag = part.ID,
            ImageIndex = 2
          };
          subNode.Nodes.Add(partNode);
        }

        // Add hardware belonging to this subassembly
        var subHardware = productData.Hardware.Where(h => h.ParentSubassemblyID == subassembly.ID);
        foreach (var hw in subHardware.OrderBy(h => h.Name))
        {
          var hwNode = new TreeNode($"⚙️ {hw.Name}")
          {
            Tag = hw.ID,
            ImageIndex = 3
          };
          subNode.Nodes.Add(hwNode);
        }

        subNode.Expand();

        // Select the subassembly by default
        hierarchyTree.SelectedNode = subNode;
      }
    }

    private void CreateControlPanel()
    {
      Program.Log("[3DViewer] CreateControlPanel - Creating visibility checkboxes and controls");
      var y = 10;

      // Visibility checkboxes
      var lblVisibility = new Label { Text = "Show Components:", Location = new Point(10, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
      controlPanel.Controls.Add(lblVisibility);
      y += 25;

      showProductsCheck = new CheckBox { Text = "📦 Products", Location = new Point(10, y), AutoSize = true, Checked = true };
      showProductsCheck.CheckedChanged += (s, e) => {
        Program.Log($"[3DViewer] Products visibility changed to: {showProductsCheck.Checked}");
        UpdateComponentVisibility();
        viewport3D.Invalidate();
      };
      controlPanel.Controls.Add(showProductsCheck);
      y += 25;

      showSubassembliesCheck = new CheckBox { Text = "🔧 Subassemblies", Location = new Point(10, y), AutoSize = true, Checked = true };
      showSubassembliesCheck.CheckedChanged += (s, e) => {
        Program.Log($"[3DViewer] Subassemblies visibility changed to: {showSubassembliesCheck.Checked}");
        UpdateComponentVisibility();
        viewport3D.Invalidate();
      };
      controlPanel.Controls.Add(showSubassembliesCheck);
      y += 25;

      showPartsCheck = new CheckBox { Text = "🧩 Parts", Location = new Point(10, y), AutoSize = true, Checked = true };
      showPartsCheck.CheckedChanged += (s, e) => {
        Program.Log($"[3DViewer] Parts visibility changed to: {showPartsCheck.Checked}");
        UpdateComponentVisibility();
        viewport3D.Invalidate();
      };
      controlPanel.Controls.Add(showPartsCheck);
      y += 25;

      showHardwareCheck = new CheckBox { Text = "⚙️ Hardware", Location = new Point(10, y), AutoSize = true, Checked = true };
      showHardwareCheck.CheckedChanged += (s, e) => {
        Program.Log($"[3DViewer] Hardware visibility changed to: {showHardwareCheck.Checked}");
        UpdateComponentVisibility();
        viewport3D.Invalidate();
      };
      controlPanel.Controls.Add(showHardwareCheck);
      y += 30;

      // Instructions
      var lblInstructions = new Label
      {
        Text = "🖱️ Left drag: Rotate\n🖱️ Right drag: Pan\n🎡 Wheel: Zoom\n⌨️ R: Reset view\n⌨️ F: Fit to screen",
        Location = new Point(10, y),
        Size = new Size(280, 80),
        Font = new Font("Segoe UI", 7F)
      };
      controlPanel.Controls.Add(lblInstructions);

      Program.Log("[3DViewer] CreateControlPanel - Control panel created successfully");
    }

    private void UpdateComponentVisibility()
    {
      Program.Log("[3DViewer] UpdateComponentVisibility - Updating component visibility based on checkboxes");
      int[] counts = new int[6]; // Array to count each component type
      int[] visibleCounts = new int[6];

      foreach (var component in allComponents)
      {
        var typeIndex = (int)component.Type;
        counts[typeIndex]++;

        var wasVisible = component.IsVisible;
        switch (component.Type)
        {
          case ComponentType.Product:
            component.IsVisible = showProductsCheck.Checked;
            break;
          case ComponentType.Subassembly:
            component.IsVisible = showSubassembliesCheck.Checked;
            break;
          case ComponentType.Part:
            component.IsVisible = showPartsCheck.Checked;
            break;
          case ComponentType.Hardware:
            component.IsVisible = showHardwareCheck.Checked;
            break;
          default:
            // DrillHole and Route components are always visible
            component.IsVisible = true;
            break;
        }

        if (component.IsVisible) visibleCounts[typeIndex]++;
      }

      Program.Log($"[3DViewer] UpdateComponentVisibility - Visibility summary: Products: {visibleCounts[0]}/{counts[0]}, Subassemblies: {visibleCounts[1]}/{counts[1]}, Parts: {visibleCounts[2]}/{counts[2]}, Hardware: {visibleCounts[3]}/{counts[3]}, DrillHoles: {visibleCounts[4]}/{counts[4]}, Routes: {visibleCounts[5]}/{counts[5]}");
    }

    private ToolStripButton CreateToolButton(string text, string tooltip, EventHandler clickHandler)
    {
      var button = new ToolStripButton(text) { ToolTipText = tooltip };
      button.Click += clickHandler;
      return button;
    }

    // Event Handlers
    private void HierarchyTree_AfterSelect(object sender, TreeViewEventArgs e)
    {
      var selectedId = e.Node?.Tag?.ToString();
      Program.Log($"[3DViewer] HierarchyTree_AfterSelect - Selected node: {e.Node?.Text}, ID: {selectedId}");

      if (string.IsNullOrEmpty(selectedId)) return;

      // Highlight the selected component
      int selectedCount = 0;
      foreach (var component in allComponents)
      {
        var wasSelected = component.IsSelected;
        component.IsSelected = component.ID == selectedId;
        if (component.IsSelected && !wasSelected)
        {
          selectedCount++;
          Program.Log($"[3DViewer] HierarchyTree_AfterSelect - Selected component: {component.Name} ({component.Type})");
        }
      }

      Program.Log($"[3DViewer] HierarchyTree_AfterSelect - Total components selected: {selectedCount}");
      viewport3D.Invalidate();
    }

    private void Viewport3D_Paint(object sender, PaintEventArgs e)
    {
      try
      {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Black);

        if (productData == null || allComponents.Count == 0)
        {
          Program.Log("[3DViewer] Viewport3D_Paint - No data available, showing loading message");
          // Show loading message
          using (var font = new Font("Segoe UI", 12F))
          using (var brush = new SolidBrush(Color.White))
          {
            var text = "Loading 3D data...";
            var textSize = g.MeasureString(text, font);
            var x = (viewport3D.Width - textSize.Width) / 2;
            var y = (viewport3D.Height - textSize.Height) / 2;
            g.DrawString(text, font, brush, x, y);
          }
          return;
        }

        var visibleCount = allComponents.Count(c => c.IsVisible);
        Program.Log($"[3DViewer] Viewport3D_Paint - Rendering scene: {visibleCount}/{allComponents.Count} visible components, Zoom: {zoomFactor:F2}, Rotation: ({rotationX:F1}, {rotationY:F1}, {rotationZ:F1}), Pan: ({panOffset.X:F1}, {panOffset.Y:F1})");

        Render3DScene(g);
      }
      catch (Exception ex)
      {
        Program.Log("[3DViewer] Viewport3D_Paint - ERROR during rendering", ex);
      }
    }

    private void Render3DScene(Graphics g)
    {
      var centerX = viewport3D.Width / 2f;
      var centerY = viewport3D.Height / 2f;

      // Apply global transformations
      g.TranslateTransform(centerX + panOffset.X, centerY + panOffset.Y);
      g.ScaleTransform(zoomFactor, zoomFactor);

      // Draw coordinate axes
      DrawCoordinateAxes(g);

      // Render each visible component
      foreach (var component in allComponents.Where(c => c.IsVisible))
      {
        RenderComponent(g, component);
      }
    }

    private void DrawCoordinateAxes(Graphics g)
    {
      const float axisLength = 50f;

      // X-axis (Red)
      var xStart = Project3DTo2D(new Point3D(0, 0, 0));
      var xEnd = Project3DTo2D(new Point3D(axisLength, 0, 0));
      g.DrawLine(new Pen(Color.Red, 2), xStart, xEnd);

      // Y-axis (Green)
      var yStart = Project3DTo2D(new Point3D(0, 0, 0));
      var yEnd = Project3DTo2D(new Point3D(0, axisLength, 0));
      g.DrawLine(new Pen(Color.Green, 2), yStart, yEnd);

      // Z-axis (Blue)
      var zStart = Project3DTo2D(new Point3D(0, 0, 0));
      var zEnd = Project3DTo2D(new Point3D(0, 0, axisLength));
      g.DrawLine(new Pen(Color.Blue, 2), zStart, zEnd);
    }

        private void RenderComponent(Graphics g, Component3D component)
    {
      // Handle special rendering for drill holes and routes
      if (component.Type == ComponentType.DrillHole)
      {
        RenderDrillHole(g, component);
        return;
      }
      else if (component.Type == ComponentType.Route)
      {
        RenderRoute(g, component);
        return;
      }

      // Standard component rendering for parts, subassemblies, etc.
      var corners = GetComponentCorners(component);
      var projectedCorners = corners.Select(Project3DTo2D).ToArray();

      // Determine drawing style
      var color = component.IsSelected ? Color.Yellow : component.Color;
      var penWidth = component.IsSelected ? 3f : 1f;

      using (var pen = new Pen(color, penWidth))
      using (var brush = new SolidBrush(Color.FromArgb(30, color)))
      {
        // Draw wireframe
        DrawWireframeCube(g, projectedCorners, pen);

        // Draw filled faces (simple back-to-front sorting)
        if (component.Type != ComponentType.Product) // Don't fill product outline
        {
          DrawFilledFaces(g, projectedCorners, brush, pen);
        }
      }

      // Draw label
      if (component.IsSelected || zoomFactor > 0.4f)
      {
        DrawComponentLabel(g, component, projectedCorners);
      }
    }

    private void RenderDrillHole(Graphics g, Component3D component)
    {
      var drill = productData.DrillHoles.FirstOrDefault(d => d.ID == component.ID);
      if (drill == null) return;

      var center = Project3DTo2D(component.Position);
      var radius = Math.Max(2f, drill.Diameter * 0.3f * zoomFactor);

      var color = component.IsSelected ? Color.Yellow : Color.Red;
      var penWidth = component.IsSelected ? 3f : 2f;

      using (var pen = new Pen(color, penWidth))
      using (var brush = new SolidBrush(Color.FromArgb(60, color)))
      {
        // Draw drill hole as a circle
        var rect = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        g.FillEllipse(brush, rect);
        g.DrawEllipse(pen, rect);

        // Draw crosshairs for drill center
        var crossSize = Math.Max(1f, radius * 0.25f);
        g.DrawLine(pen, center.X - crossSize, center.Y, center.X + crossSize, center.Y);
        g.DrawLine(pen, center.X, center.Y - crossSize, center.X, center.Y + crossSize);
      }

      // Draw label for selected or zoomed drill holes
      if (component.IsSelected || zoomFactor > 0.8f)
      {
        using (var font = new Font("Segoe UI", 5F))
        using (var brush = new SolidBrush(Color.White))
        using (var backgroundBrush = new SolidBrush(Color.FromArgb(128, Color.Black)))
        {
          var text = $"{drill.Name} Ø{drill.Diameter:F1}";
          var textSize = g.MeasureString(text, font);
          var textRect = new RectangleF(
            center.X - textSize.Width / 2,
            center.Y - radius - textSize.Height - 5,
            textSize.Width,
            textSize.Height
          );

          g.FillRectangle(backgroundBrush, textRect);
          g.DrawString(text, font, brush, textRect.Location);
        }
      }
    }

    private void RenderRoute(Graphics g, Component3D component)
    {
      var route = productData.Routes.FirstOrDefault(r => r.ID == component.ID);
      if (route == null || route.Vectors.Count < 2) return;

      var color = component.IsSelected ? Color.Yellow : Color.Orange;
      var penWidth = component.IsSelected ? 3f : 2f;

      using (var pen = new Pen(color, penWidth))
      {
        // Draw route as connected line segments
        for (int i = 0; i < route.Vectors.Count - 1; i++)
        {
          var start = Project3DTo2D(new Point3D(route.Vectors[i].X, route.Vectors[i].Y, route.Vectors[i].Z));
          var end = Project3DTo2D(new Point3D(route.Vectors[i + 1].X, route.Vectors[i + 1].Y, route.Vectors[i + 1].Z));

          // Check if this is an arc (bulge != 0)
          if (Math.Abs(route.Vectors[i].Bulge) > 0.001f)
          {
            DrawArcSegment(g, pen, start, end, route.Vectors[i].Bulge);
          }
          else
          {
            // Straight line
            g.DrawLine(pen, start, end);
          }
        }

        // Draw start and end points
        if (route.Vectors.Count > 0)
        {
          var startPoint = Project3DTo2D(new Point3D(route.Vectors[0].X, route.Vectors[0].Y, route.Vectors[0].Z));
          var endPoint = Project3DTo2D(new Point3D(route.Vectors.Last().X, route.Vectors.Last().Y, route.Vectors.Last().Z));

          var pointSize = Math.Max(2f, 3f * zoomFactor);
          using (var startBrush = new SolidBrush(Color.Green))
          using (var endBrush = new SolidBrush(Color.Red))
          {
            g.FillEllipse(startBrush, startPoint.X - pointSize / 2, startPoint.Y - pointSize / 2, pointSize, pointSize);
            g.FillEllipse(endBrush, endPoint.X - pointSize / 2, endPoint.Y - pointSize / 2, pointSize, pointSize);
          }
        }
      }

      // Draw label for selected routes
      if (component.IsSelected && route.Vectors.Count > 0)
      {
        var centerVector = route.Vectors[route.Vectors.Count / 2];
        var labelPos = Project3DTo2D(new Point3D(centerVector.X, centerVector.Y, centerVector.Z));

        using (var font = new Font("Segoe UI", 5F))
        using (var brush = new SolidBrush(Color.White))
        using (var backgroundBrush = new SolidBrush(Color.FromArgb(128, Color.Black)))
        {
          var text = $"{route.Name} Ø{route.Diameter:F1}";
          var textSize = g.MeasureString(text, font);
          var textRect = new RectangleF(
            labelPos.X - textSize.Width / 2,
            labelPos.Y - textSize.Height / 2,
            textSize.Width,
            textSize.Height
          );

          g.FillRectangle(backgroundBrush, textRect);
          g.DrawString(text, font, brush, textRect.Location);
        }
      }
    }

    private void DrawArcSegment(Graphics g, Pen pen, PointF start, PointF end, float bulge)
    {
      // Simple arc approximation - for production use, implement proper arc geometry
      var midX = (start.X + end.X) / 2 + bulge * (end.Y - start.Y) / 4;
      var midY = (start.Y + end.Y) / 2 + bulge * (start.X - end.X) / 4;
      var mid = new PointF(midX, midY);

      // Draw as a curve through three points
      var points = new[] { start, mid, end };
      try
      {
        g.DrawCurve(pen, points);
      }
      catch
      {
        // Fallback to straight line if curve fails
        g.DrawLine(pen, start, end);
      }
    }

    private Point3D[] GetComponentCorners(Component3D component)
    {
      var pos = component.Position;
      var dim = component.Dimensions;

      // 8 corners of a box
      return new[]
      {
        new Point3D(pos.X, pos.Y, pos.Z),                           // 0: front-bottom-left
        new Point3D(pos.X + dim.Width, pos.Y, pos.Z),               // 1: front-bottom-right
        new Point3D(pos.X + dim.Width, pos.Y + dim.Height, pos.Z),  // 2: front-top-right
        new Point3D(pos.X, pos.Y + dim.Height, pos.Z),              // 3: front-top-left
        new Point3D(pos.X, pos.Y, pos.Z + dim.Depth),               // 4: back-bottom-left
        new Point3D(pos.X + dim.Width, pos.Y, pos.Z + dim.Depth),   // 5: back-bottom-right
        new Point3D(pos.X + dim.Width, pos.Y + dim.Height, pos.Z + dim.Depth), // 6: back-top-right
        new Point3D(pos.X, pos.Y + dim.Height, pos.Z + dim.Depth)   // 7: back-top-left
      };
    }

    private PointF Project3DTo2D(Point3D point3D)
    {
      // Simple isometric projection with rotation
      var x = point3D.X;
      var y = point3D.Y;
      var z = point3D.Z;

      // Apply rotations (simple rotation matrices)
      var cosX = Math.Cos(rotationX * Math.PI / 180);
      var sinX = Math.Sin(rotationX * Math.PI / 180);
      var cosY = Math.Cos(rotationY * Math.PI / 180);
      var sinY = Math.Sin(rotationY * Math.PI / 180);
      var cosZ = Math.Cos(rotationZ * Math.PI / 180);
      var sinZ = Math.Sin(rotationZ * Math.PI / 180);

      // Rotate around X-axis
      var y1 = y * cosX - z * sinX;
      var z1 = y * sinX + z * cosX;

      // Rotate around Y-axis
      var x2 = x * cosY + z1 * sinY;
      var z2 = -x * sinY + z1 * cosY;

      // Rotate around Z-axis
      var x3 = x2 * cosZ - y1 * sinZ;
      var y3 = x2 * sinZ + y1 * cosZ;

      // Isometric projection
      var scale = 0.8f;
      var projectedX = (float)(x3 * scale);
      var projectedY = (float)((y3 - z2) * scale);

      return new PointF(projectedX, projectedY);
    }

    private void DrawWireframeCube(Graphics g, PointF[] corners, Pen pen)
    {
      // Front face
      g.DrawLine(pen, corners[0], corners[1]);
      g.DrawLine(pen, corners[1], corners[2]);
      g.DrawLine(pen, corners[2], corners[3]);
      g.DrawLine(pen, corners[3], corners[0]);

      // Back face
      g.DrawLine(pen, corners[4], corners[5]);
      g.DrawLine(pen, corners[5], corners[6]);
      g.DrawLine(pen, corners[6], corners[7]);
      g.DrawLine(pen, corners[7], corners[4]);

      // Connecting lines
      g.DrawLine(pen, corners[0], corners[4]);
      g.DrawLine(pen, corners[1], corners[5]);
      g.DrawLine(pen, corners[2], corners[6]);
      g.DrawLine(pen, corners[3], corners[7]);
    }

    private void DrawFilledFaces(Graphics g, PointF[] corners, Brush brush, Pen pen)
    {
      // Draw visible faces (simple approach - just front and top)

      // Front face
      var frontFace = new[] { corners[0], corners[1], corners[2], corners[3] };
      g.FillPolygon(brush, frontFace);
      g.DrawPolygon(pen, frontFace);

      // Top face
      var topFace = new[] { corners[3], corners[2], corners[6], corners[7] };
      g.FillPolygon(brush, topFace);
      g.DrawPolygon(pen, topFace);
    }

    private void DrawComponentLabel(Graphics g, Component3D component, PointF[] corners)
    {
      // Draw label at the center-top of the component
      var centerTop = new PointF(
        (corners[2].X + corners[3].X) / 2,
        (corners[2].Y + corners[3].Y) / 2 - 15
      );

      using (var font = new Font("Segoe UI", 6F))
      using (var brush = new SolidBrush(Color.White))
      using (var backgroundBrush = new SolidBrush(Color.FromArgb(128, Color.Black)))
      {
        var text = component.Name;
        var textSize = g.MeasureString(text, font);
        var textRect = new RectangleF(
          centerTop.X - textSize.Width / 2,
          centerTop.Y - textSize.Height / 2,
          textSize.Width,
          textSize.Height
        );

        g.FillRectangle(backgroundBrush, textRect);
        g.DrawString(text, font, brush, textRect.Location);
      }
    }

    // Mouse Event Handlers
    private void Viewport3D_MouseDown(object sender, MouseEventArgs e)
    {
      Program.Log($"[3DViewer] Viewport3D_MouseDown - Button: {e.Button}, Location: ({e.X}, {e.Y})");
      isDragging = true;
      lastMousePos = e.Location;
    }

    private void Viewport3D_MouseMove(object sender, MouseEventArgs e)
    {
      if (!isDragging) return;

      var deltaX = e.X - lastMousePos.X;
      var deltaY = e.Y - lastMousePos.Y;

      if (e.Button == MouseButtons.Left)
      {
        // Rotation
        var oldRotationX = rotationX;
        var oldRotationY = rotationY;
        rotationY += deltaX * 0.5f;
        rotationX += deltaY * 0.5f;
        Program.Log($"[3DViewer] Viewport3D_MouseMove - Rotation: ({oldRotationX:F1}, {oldRotationY:F1}) -> ({rotationX:F1}, {rotationY:F1})");
      }
      else if (e.Button == MouseButtons.Right)
      {
        // Panning
        var oldPanX = panOffset.X;
        var oldPanY = panOffset.Y;
        panOffset.X += deltaX;
        panOffset.Y += deltaY;
        Program.Log($"[3DViewer] Viewport3D_MouseMove - Pan: ({oldPanX:F1}, {oldPanY:F1}) -> ({panOffset.X:F1}, {panOffset.Y:F1})");
      }

      lastMousePos = e.Location;
      viewport3D.Invalidate();
    }

    private void Viewport3D_MouseUp(object sender, MouseEventArgs e)
    {
      Program.Log($"[3DViewer] Viewport3D_MouseUp - Button: {e.Button}, Dragging stopped");
      isDragging = false;
    }

    private void Viewport3D_MouseWheel(object sender, MouseEventArgs e)
    {
      var oldZoom = zoomFactor;
      var zoomDelta = e.Delta > 0 ? 1.1f : 0.9f;
      zoomFactor *= zoomDelta;
      zoomFactor = Math.Max(0.1f, Math.Min(5f, zoomFactor));
      Program.Log($"[3DViewer] Viewport3D_MouseWheel - Zoom: {oldZoom:F2} -> {zoomFactor:F2} (Delta: {e.Delta})");
      viewport3D.Invalidate();
    }

    // Toolbar Event Handlers
    private void ResetView(object sender, EventArgs e)
    {
      Program.Log("[3DViewer] ResetView - Resetting view to default values");
      rotationX = -20f;
      rotationY = 30f;
      rotationZ = 0f;
      zoomFactor = 1f;
      panOffset = new PointF(0, 0);
      Program.Log($"[3DViewer] ResetView - Reset to: Rotation({rotationX}, {rotationY}, {rotationZ}), Zoom: {zoomFactor}, Pan: (0, 0)");
      viewport3D.Invalidate();
    }

    private void FitToScreen(object sender, EventArgs e)
    {
      Program.Log("[3DViewer] FitToScreen - Starting fit to screen calculation");
      if (allComponents.Count == 0)
      {
        Program.Log("[3DViewer] FitToScreen - No components available");
        return;
      }

      // Calculate bounding box of all components
      var minX = allComponents.Min(c => c.Position.X);
      var maxX = allComponents.Max(c => c.Position.X + c.Dimensions.Width);
      var minY = allComponents.Min(c => c.Position.Y);
      var maxY = allComponents.Max(c => c.Position.Y + c.Dimensions.Height);
      var minZ = allComponents.Min(c => c.Position.Z);
      var maxZ = allComponents.Max(c => c.Position.Z + c.Dimensions.Depth);

      var width = maxX - minX;
      var height = maxY - minY;
      var depth = maxZ - minZ;
      var maxDimension = Math.Max(width, Math.Max(height, depth));

      Program.Log($"[3DViewer] FitToScreen - Bounding box: X({minX:F1} to {maxX:F1}), Y({minY:F1} to {maxY:F1}), Z({minZ:F1} to {maxZ:F1})");
      Program.Log($"[3DViewer] FitToScreen - Dimensions: W:{width:F1}, H:{height:F1}, D:{depth:F1}, Max: {maxDimension:F1}");

      var oldZoom = zoomFactor;
      if (maxDimension > 0)
      {
        zoomFactor = Math.Min(viewport3D.Width, viewport3D.Height) / (maxDimension * 2f);
        zoomFactor = Math.Max(0.1f, Math.Min(5f, zoomFactor));
      }

      panOffset = new PointF(0, 0);
      Program.Log($"[3DViewer] FitToScreen - Adjusted zoom from {oldZoom:F2} to {zoomFactor:F2}, reset pan to (0, 0)");
      viewport3D.Invalidate();
    }

    private void ToggleWireframe(object sender, EventArgs e)
    {
      // TODO: Implement wireframe mode toggle
      viewport3D.Invalidate();
    }

    private void ToggleSolid(object sender, EventArgs e)
    {
      // TODO: Implement solid mode toggle
      viewport3D.Invalidate();
    }

    private void ExportImage(object sender, EventArgs e)
    {
      try
      {
        using (var saveDialog = new SaveFileDialog())
        {
          saveDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
          saveDialog.Title = "Export 3D View";
          saveDialog.FileName = $"Product_{productId}_3DView.png";

          if (saveDialog.ShowDialog() == DialogResult.OK)
          {
            using (var bitmap = new Bitmap(viewport3D.Width, viewport3D.Height))
            {
              viewport3D.DrawToBitmap(bitmap, new Rectangle(0, 0, viewport3D.Width, viewport3D.Height));
              bitmap.Save(saveDialog.FileName);
              MessageBox.Show($"3D view exported to {saveDialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error exporting image: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void Product3DViewer_KeyDown(object sender, KeyEventArgs e)
    {
      switch (e.KeyCode)
      {
        case Keys.R:
          ResetView(sender, e);
          break;
        case Keys.F:
          FitToScreen(sender, e);
          break;
        case Keys.Escape:
          Close();
          break;
      }
    }

    private string GetWindowTitle(string id, string sourceTable)
    {
      switch (sourceTable?.ToLowerInvariant())
      {
        case "products":
          return $"3D Viewer - Product: {id}";
        case "parts":
          return $"3D Viewer - Individual Part: {id}";
        case "subassemblies":
          return $"3D Viewer - Subassembly: {id}";
        case "hardware":
          return $"3D Viewer - Hardware: {id}";
        default:
          return $"3D Viewer - {id}";
      }
    }

    private void UpdateWindowTitleWithData()
    {
      if (productData == null) return;

      switch (sourceTableName?.ToLowerInvariant())
      {
        case "products":
          Text = $"3D Viewer - Product: {productData.Name}";
          break;
        case "parts":
          if (productData.Parts.Count > 0)
          {
            var part = productData.Parts.First();
            Text = $"3D Viewer - Part: {part.Name} (from {productData.Name})";
          }
          break;
        case "subassemblies":
          if (productData.Subassemblies.Count > 0)
          {
            var sub = productData.Subassemblies.First();
            Text = $"3D Viewer - Subassembly: {sub.Name} (from {productData.Name})";
          }
          break;
        default:
          Text = $"3D Viewer - {productData.Name}";
          break;
      }
    }

    protected override void Dispose(bool disposing)
    {
      Program.Log("[3DViewer] Dispose - Starting cleanup process");
      if (disposing)
      {
        try
        {
          if (connection != null)
          {
            Program.Log("[3DViewer] Dispose - Closing database connection");
            connection?.Close();
            connection?.Dispose();
            connection = null;
            Program.Log("[3DViewer] Dispose - Database connection disposed successfully");
          }
        }
        catch (Exception ex)
        {
          Program.Log("[3DViewer] Dispose - ERROR disposing 3D Viewer connection", ex);
        }
      }
      Program.Log("[3DViewer] Dispose - Cleanup completed");
      base.Dispose(disposing);
    }
  }

  // 3D Data Classes
  public class Product3DData
  {
    public string ProductID { get; set; }
    public string Name { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Depth { get; set; }
    public float Angle { get; set; }
    public List<Subassembly3D> Subassemblies { get; set; } = new List<Subassembly3D>();
    public List<Part3D> Parts { get; set; } = new List<Part3D>();
    public List<Hardware3D> Hardware { get; set; } = new List<Hardware3D>();
    public List<DrillHole3D> DrillHoles { get; set; } = new List<DrillHole3D>();
    public List<Route3D> Routes { get; set; } = new List<Route3D>();
  }

  public class Subassembly3D
  {
    public string ID { get; set; }
    public string Name { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Depth { get; set; }
    public float Angle { get; set; }
    public string ParentProductID { get; set; }
    public string ParentSubassemblyID { get; set; } // For nested subassemblies
  }

  public class Part3D
  {
    public string ID { get; set; }
    public string Name { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Length { get; set; }
    public float Width { get; set; }
    public float Thickness { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public string ParentSubassemblyID { get; set; }
    public string MaterialName { get; set; }
    public bool IsDirectToProduct { get; set; } // True if linked directly to product (not via subassembly)
  }

  public class Hardware3D
  {
    public string ID { get; set; }
    public string Name { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Depth { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public string ParentSubassemblyID { get; set; }
  }

  public class Component3D
  {
    public string ID { get; set; }
    public string Name { get; set; }
    public ComponentType Type { get; set; }
    public Point3D Position { get; set; }
    public Size3D Dimensions { get; set; }
    public Rotation3D Rotation { get; set; }
    public Color Color { get; set; }
    public string ParentID { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsSelected { get; set; }
    public string MaterialName { get; set; }
    public bool IsDirectToProduct { get; set; } // For parts: true if linked directly to product (not via subassembly)
  }

  public struct Point3D
  {
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Point3D(float x, float y, float z)
    {
      X = x;
      Y = y;
      Z = z;
    }
  }

  public struct Size3D
  {
    public float Width { get; set; }
    public float Height { get; set; }
    public float Depth { get; set; }

    public Size3D(float width, float height, float depth)
    {
      Width = width;
      Height = height;
      Depth = depth;
    }
  }

  public struct Rotation3D
  {
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Rotation3D(float x, float y, float z)
    {
      X = x;
      Y = y;
      Z = z;
    }
  }

  public class DrillHole3D
  {
    public string ID { get; set; }
    public string Name { get; set; }
    public string PartID { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Diameter { get; set; }
    public float Depth { get; set; }
    public string DrillType { get; set; }
    public string ToolName { get; set; }
    public int Face { get; set; } // Which face of the part (1-6)
  }

  public class Route3D
  {
    public string ID { get; set; }
    public string Name { get; set; }
    public string PartID { get; set; }
    public float Diameter { get; set; }
    public float Depth { get; set; }
    public string ToolName { get; set; }
    public int Face { get; set; }
    public List<Vector3D> Vectors { get; set; } = new List<Vector3D>();
  }

  public class Vector3D
  {
    public string ID { get; set; }
    public string RouteID { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Radius { get; set; } // For arcs
    public float Bulge { get; set; } // For arc direction
    public int Sequence { get; set; }
  }

  public enum ComponentType
  {
    Product,
    Subassembly,
    Part,
    Hardware,
    DrillHole,
    Route
  }
}
