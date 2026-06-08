using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseApp.Models;
using OpenAI.Chat;

namespace ExpenseApp.Services;

public interface IChatService
{
    Task<ChatResponse> ChatAsync(string userMessage);
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ChatService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ChatService(
        IConfiguration configuration,
        IExpenseService expenseService,
        ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task<ChatResponse> ChatAsync(string userMessage)
    {
        var endpoint = _configuration["OpenAI:Endpoint"];
        var deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new ChatResponse
            {
                Reply = "**Azure OpenAI is not configured.**\n\n" +
                        "To enable the AI assistant, deploy the GenAI resources by running:\n\n" +
                        "```bash\nbash deploy-with-chat.sh\n```\n\n" +
                        "This will provision Azure OpenAI (GPT-4o) and Azure AI Search, " +
                        "and configure the app service settings automatically.\n\n" +
                        "In the meantime, here is some dummy data to show the interface is working:\n\n" +
                        "- **Total Expenses:** 4\n" +
                        "- **Submitted (Pending Approval):** 1 — £25.40\n" +
                        "- **Approved:** 2 — £137.25\n" +
                        "- **Draft:** 1 — £7.99"
            };
        }

        try
        {
            // Use ManagedIdentityCredential with explicit client ID on App Service;
            // fall back to DefaultAzureCredential for local development.
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
            Azure.Core.TokenCredential credential;

            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            var openAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
            var chatClient = openAIClient.GetChatClient(deploymentName);

            // ── Define tools ───────────────────────────────────────────────────
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_all_expenses",
                    "Retrieves all expenses from the database with user, category and status details"),

                ChatTool.CreateFunctionTool(
                    "get_pending_expenses",
                    "Retrieves all expenses with 'Submitted' status that are awaiting manager approval"),

                ChatTool.CreateFunctionTool(
                    "get_expense_summary",
                    "Returns a summary count and total GBP amount grouped by expense status"),

                ChatTool.CreateFunctionTool(
                    "create_expense",
                    "Creates a new Draft expense in the database",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "userId":      { "type": "integer", "description": "ID of the user creating the expense" },
                            "categoryId":  { "type": "integer", "description": "ID of the expense category" },
                            "amountMinor": { "type": "integer", "description": "Amount in minor units/pence (e.g. 1250 for £12.50)" },
                            "expenseDate": { "type": "string",  "description": "Date of the expense in YYYY-MM-DD format" },
                            "description": { "type": "string",  "description": "Optional description of the expense" }
                        },
                        "required": ["userId", "categoryId", "amountMinor", "expenseDate"]
                    }
                    """)),

                ChatTool.CreateFunctionTool(
                    "approve_expense",
                    "Approves a submitted expense",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "expenseId":  { "type": "integer", "description": "ID of the expense to approve" },
                            "reviewedBy": { "type": "integer", "description": "User ID of the manager approving" }
                        },
                        "required": ["expenseId", "reviewedBy"]
                    }
                    """)),

                ChatTool.CreateFunctionTool(
                    "reject_expense",
                    "Rejects a submitted expense",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "expenseId":  { "type": "integer", "description": "ID of the expense to reject" },
                            "reviewedBy": { "type": "integer", "description": "User ID of the manager rejecting" }
                        },
                        "required": ["expenseId", "reviewedBy"]
                    }
                    """)),
            };

            var systemPrompt = """
                You are an intelligent assistant for an Expense Management System.
                You have access to real functions that interact with the database:
                - get_all_expenses: List all expenses
                - get_pending_expenses: List expenses waiting for approval
                - get_expense_summary: Get counts and totals by status
                - create_expense: Create a new draft expense
                - approve_expense: Approve a submitted expense (manager action)
                - reject_expense: Reject a submitted expense (manager action)

                Always use these functions when users ask about expense data.
                Format monetary amounts in GBP (£) by dividing pence values by 100.
                Present lists in a clear, formatted way using markdown.
                Be helpful, professional, and concise.
                """;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
                options.Tools.Add(tool);

            // ── Orchestration loop ─────────────────────────────────────────────
            const int maxIterations = 5;
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                var response = await chatClient.CompleteChatAsync(messages, options);
                var completion = response.Value;

                if (completion.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Add assistant message with tool calls
                    messages.Add(new AssistantChatMessage(completion));

                    // Execute each tool call
                    foreach (var toolCall in completion.ToolCalls)
                    {
                        var result = await ExecuteToolAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                        messages.Add(new ToolChatMessage(toolCall.Id, result));
                    }
                    // Continue loop to get final response
                    continue;
                }

                // Final text response
                var replyText = completion.Content.FirstOrDefault()?.Text ?? "I'm sorry, I could not generate a response.";
                return new ChatResponse { Reply = replyText };
            }

            return new ChatResponse { Reply = "The assistant reached the maximum number of tool call iterations." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChatService.ChatAsync");
            return new ChatResponse
            {
                Reply = $"**Error connecting to Azure OpenAI:** {ex.Message}\n\n" +
                        "Please check that:\n" +
                        "- The OpenAI endpoint is configured correctly\n" +
                        "- The managed identity has 'Cognitive Services OpenAI User' role\n" +
                        "- The AZURE_CLIENT_ID app setting is set to the managed identity client ID"
            };
        }
    }

    private async Task<string> ExecuteToolAsync(string functionName, string argumentsJson)
    {
        try
        {
            _logger.LogInformation("Executing tool: {FunctionName} with args: {Args}", functionName, argumentsJson);

            return functionName switch
            {
                "get_all_expenses" => await GetAllExpensesToolAsync(),
                "get_pending_expenses" => await GetPendingExpensesToolAsync(),
                "get_expense_summary" => await GetExpenseSummaryToolAsync(),
                "create_expense" => await CreateExpenseToolAsync(argumentsJson),
                "approve_expense" => await ApproveExpenseToolAsync(argumentsJson),
                "reject_expense" => await RejectExpenseToolAsync(argumentsJson),
                _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> GetAllExpensesToolAsync()
    {
        var (data, _) = await _expenseService.GetAllExpensesAsync();
        var simplified = data.Select(e => new
        {
            e.ExpenseId, e.UserName, e.CategoryName, e.StatusName,
            AmountGbp = e.AmountGbp, e.ExpenseDate, e.Description
        });
        return JsonSerializer.Serialize(simplified, JsonOpts);
    }

    private async Task<string> GetPendingExpensesToolAsync()
    {
        var (data, _) = await _expenseService.GetPendingExpensesAsync();
        var simplified = data.Select(e => new
        {
            e.ExpenseId, e.UserName, e.CategoryName, e.StatusName,
            AmountGbp = e.AmountGbp, e.ExpenseDate, e.Description, e.SubmittedAt
        });
        return JsonSerializer.Serialize(simplified, JsonOpts);
    }

    private async Task<string> GetExpenseSummaryToolAsync()
    {
        var (data, _) = await _expenseService.GetExpenseSummaryAsync();
        return JsonSerializer.Serialize(data, JsonOpts);
    }

    private async Task<string> CreateExpenseToolAsync(string argsJson)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var root = doc.RootElement;

        var request = new CreateExpenseRequest
        {
            UserId      = root.GetProperty("userId").GetInt32(),
            CategoryId  = root.GetProperty("categoryId").GetInt32(),
            AmountMinor = root.GetProperty("amountMinor").GetInt32(),
            ExpenseDate = DateTime.Parse(root.GetProperty("expenseDate").GetString()!),
            Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
        };

        var (newId, error) = await _expenseService.CreateExpenseAsync(request);
        if (error?.HasError == true)
            return JsonSerializer.Serialize(new { error = error.Message });
        return JsonSerializer.Serialize(new { success = true, newExpenseId = newId });
    }

    private async Task<string> ApproveExpenseToolAsync(string argsJson)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var root = doc.RootElement;
        var expenseId = root.GetProperty("expenseId").GetInt32();
        var reviewedBy = root.GetProperty("reviewedBy").GetInt32();
        var (success, error) = await _expenseService.ApproveExpenseAsync(expenseId, reviewedBy);
        if (error?.HasError == true)
            return JsonSerializer.Serialize(new { error = error.Message });
        return JsonSerializer.Serialize(new { success, expenseId });
    }

    private async Task<string> RejectExpenseToolAsync(string argsJson)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var root = doc.RootElement;
        var expenseId = root.GetProperty("expenseId").GetInt32();
        var reviewedBy = root.GetProperty("reviewedBy").GetInt32();
        var (success, error) = await _expenseService.RejectExpenseAsync(expenseId, reviewedBy);
        if (error?.HasError == true)
            return JsonSerializer.Serialize(new { error = error.Message });
        return JsonSerializer.Serialize(new { success, expenseId });
    }
}
