namespace ExpenseManagementApp.Services;

public interface IChatService
{
    Task<(string Message, bool UsedDummy)> AskAsync(string prompt, CancellationToken ct);
}
