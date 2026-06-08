using System.Diagnostics;

namespace ExpenseManagementApp.Services;

public static class ErrorDetailsFormatter
{
    public static string BuildHeader(Exception ex)
    {
        var frame = new StackTrace(ex, true).GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0);
        var file = frame?.GetFileName() ?? "unknown-file";
        var line = frame?.GetFileLineNumber() ?? 0;

        return $"Database fallback mode: {ex.Message}. File: {file}. Line: {line}. " +
               "Managed identity fix: verify App Service has the user-assigned managed identity, set AZURE_CLIENT_ID to that identity client id, " +
               "grant database access (CREATE USER FROM EXTERNAL PROVIDER + db_datareader/db_datawriter/EXECUTE), and ensure connection string uses Authentication=Active Directory Managed Identity with User Id=<client-id>.";
    }
}
