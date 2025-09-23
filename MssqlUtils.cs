using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace WorkOrderBlender
{
  internal static class MssqlUtils
  {
    /// <summary>
    /// Creates a connection to the main Microvellum MSSQL database using UserConfig settings
    /// </summary>
    /// <returns>SqlConnection instance for the Microvellum database</returns>
    public static SqlConnection CreateMicrovellumConnection()
    {
      var config = UserConfig.LoadOrDefault();
      var connectionString = $"Server={config.MssqlServer};Database={config.MssqlDatabase};User Id={config.MssqlUsername};Password={config.MssqlPassword};TrustServerCertificate=true;Connection Timeout=10;";
      return new SqlConnection(connectionString);
    }

    /// <summary>
    /// Checks if a work order name already exists in the WorkOrders table
    /// </summary>
    /// <param name="workOrderName">The work order name to check</param>
    /// <returns>True if the work order name exists, false otherwise</returns>
    public static async Task<bool> WorkOrderNameExistsAsync(string workOrderName)
    {
      if (string.IsNullOrWhiteSpace(workOrderName))
        return false;

      var config = UserConfig.LoadOrDefault();
      if (!config.MssqlEnabled)
      {
        Program.Log($"MssqlUtils: MSSQL validation is disabled, skipping work order name check for '{workOrderName}'");
        return false;
      }

      try
      {
        // Add a timeout to prevent hanging
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        {
          using (var connection = CreateMicrovellumConnection())
          {
            await connection.OpenAsync(cts.Token);
            Program.Log($"MssqlUtils: Connected to Microvellum database ({config.MssqlServer}\\{config.MssqlDatabase}) to check work order name '{workOrderName}'");

            var query = "SELECT COUNT(1) FROM WorkOrders WHERE Name = @workOrderName";
            using (var command = new SqlCommand(query, connection))
            {
              command.CommandTimeout = 10; // 10 second command timeout
              command.Parameters.AddWithValue("@workOrderName", workOrderName.Trim());

              var count = (int)await command.ExecuteScalarAsync(cts.Token);
              var exists = count > 0;

              Program.Log($"MssqlUtils: Work order name '{workOrderName}' exists: {exists}");
              return exists;
            }
          }
        }
      }
      catch (OperationCanceledException)
      {
        Program.Log($"MssqlUtils: Work order name check timed out for '{workOrderName}'");
        return false; // Assume name doesn't exist on timeout
      }
      catch (Exception ex)
      {
        Program.Log($"MssqlUtils: Error checking work order name '{workOrderName}': {ex.Message}", ex);
        // If we can't connect to the database, assume the name doesn't exist to avoid blocking the user
        return false;
      }
    }

    /// <summary>
    /// Synchronous version of WorkOrderNameExistsAsync for compatibility
    /// </summary>
    /// <param name="workOrderName">The work order name to check</param>
    /// <returns>True if the work order name exists, false otherwise</returns>
    public static bool WorkOrderNameExists(string workOrderName)
    {
      try
      {
        return WorkOrderNameExistsAsync(workOrderName).GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        Program.Log($"MssqlUtils: Error in synchronous work order name check: {ex.Message}", ex);
        return false;
      }
    }

    /// <summary>
    /// Tests the connection to the Microvellum database
    /// </summary>
    /// <returns>True if connection is successful, false otherwise</returns>
    public static async Task<bool> TestConnectionAsync()
    {
      var config = UserConfig.LoadOrDefault();
      if (!config.MssqlEnabled)
      {
        Program.Log("MssqlUtils: MSSQL validation is disabled, cannot test connection");
        return false;
      }

      try
      {
        using (var connection = CreateMicrovellumConnection())
        {
          await connection.OpenAsync();
          Program.Log($"MssqlUtils: Successfully connected to Microvellum database ({config.MssqlServer}\\{config.MssqlDatabase})");
          return true;
        }
      }
      catch (Exception ex)
      {
        Program.Log($"MssqlUtils: Failed to connect to Microvellum database ({config.MssqlServer}\\{config.MssqlDatabase}): {ex.Message}", ex);
        return false;
      }
    }

    /// <summary>
    /// Synchronous version of TestConnectionAsync for compatibility
    /// </summary>
    /// <returns>True if connection is successful, false otherwise</returns>
    public static bool TestConnection()
    {
      try
      {
        return TestConnectionAsync().GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        Program.Log($"MssqlUtils: Error in synchronous connection test: {ex.Message}", ex);
        return false;
      }
    }
  }
}
