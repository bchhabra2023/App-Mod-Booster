using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ExpenseManagementApp.Models;
using Azure.Core;
using Azure.Identity;

namespace ExpenseManagementApp.Services;

public sealed class ChatService(
    IConfiguration configuration,
    IExpenseService expenseService,
    IHttpClientFactory httpClientFactory,
    ILogger<ChatService> logger) : IChatService
{
    private readonly string _endpoint = configuration["OpenAI:Endpoint"] ?? string.Empty;
    private readonly string _deployment = configuration["OpenAI:DeploymentName"] ?? string.Empty;
    private readonly string _apiVersion = configuration["OpenAI:ApiVersion"] ?? "2024-10-21";
    private readonly string? _managedIdentityClientId = configuration["ManagedIdentityClientId"];

    public async Task<(string Message, bool UsedDummy)> AskAsync(string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_deployment))
        {
            return ($"GenAI resources are not deployed. Use deploy-with-chat.sh for full experience. You asked: {prompt}", true);
        }

        try
        {
            var tools = BuildTools();
            var messages = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = "You have real backend functions for expense operations. Use tools when listing/creating/updating expenses." },
                new JsonObject { ["role"] = "user", ["content"] = prompt }
            };

            for (var iteration = 0; iteration < 3; iteration++)
            {
                var payload = new JsonObject
                {
                    ["messages"] = messages,
                    ["tools"] = tools,
                    ["tool_choice"] = "auto",
                    ["temperature"] = 0.2
                };

                var response = await SendChatRequestAsync(payload, ct);
                var message = response["choices"]?[0]?["message"]?.AsObject();
                if (message is null)
                {
                    return ("I couldn't process that request right now.", true);
                }

                var toolCalls = message["tool_calls"] as JsonArray;
                if (toolCalls is null || toolCalls.Count == 0)
                {
                    var content = message["content"]?.ToString() ?? "No response content returned.";
                    return (content, false);
                }

                messages.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["tool_calls"] = toolCalls.DeepClone()
                });

                foreach (var callNode in toolCalls)
                {
                    var call = callNode!.AsObject();
                    var id = call["id"]?.ToString() ?? Guid.NewGuid().ToString("N");
                    var function = call["function"]?.AsObject();
                    var name = function?["name"]?.ToString() ?? string.Empty;
                    var arguments = function?["arguments"]?.ToString() ?? "{}";

                    var result = await ExecuteToolAsync(name, arguments, ct);
                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = id,
                        ["content"] = result
                    });
                }
            }

            return ("I ran out of tool-calling iterations for this request.", false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat service failed");
            return ($"Chat fallback mode: {ErrorDetailsFormatter.BuildHeader(ex)}", true);
        }
    }

    private async Task<string> ExecuteToolAsync(string name, string argsJson, CancellationToken ct)
    {
        var args = JsonNode.Parse(argsJson)?.AsObject() ?? new JsonObject();

        return name switch
        {
            "get_expenses" => await HandleGetExpensesAsync(args, ct),
            "create_expense" => await HandleCreateExpenseAsync(args, ct),
            "update_expense_status" => await HandleUpdateExpenseStatusAsync(args, ct),
            _ => JsonSerializer.Serialize(new { error = $"Unknown function: {name}" })
        };
    }

    private async Task<string> HandleGetExpensesAsync(JsonObject args, CancellationToken ct)
    {
        var status = args["statusName"]?.ToString();
        var email = args["userEmail"]?.ToString();
        var (rows, error) = await expenseService.GetExpensesAsync(status, email, ct);
        return JsonSerializer.Serialize(new { error, expenses = rows });
    }

    private async Task<string> HandleCreateExpenseAsync(JsonObject args, CancellationToken ct)
    {
        var request = new CreateExpenseRequest
        {
            UserEmail = args["userEmail"]?.ToString() ?? string.Empty,
            CategoryName = args["categoryName"]?.ToString() ?? string.Empty,
            Amount = decimal.TryParse(args["amount"]?.ToString(), out var amount) ? amount : 0,
            ExpenseDate = DateOnly.TryParse(args["expenseDate"]?.ToString(), out var date) ? date : DateOnly.FromDateTime(DateTime.UtcNow),
            Description = args["description"]?.ToString() ?? string.Empty
        };

        var (ok, error) = await expenseService.CreateExpenseAsync(request, ct);
        return JsonSerializer.Serialize(new { success = ok, error });
    }

    private async Task<string> HandleUpdateExpenseStatusAsync(JsonObject args, CancellationToken ct)
    {
        var id = int.TryParse(args["expenseId"]?.ToString(), out var parsed) ? parsed : 0;
        var request = new UpdateExpenseStatusRequest
        {
            ManagerEmail = args["managerEmail"]?.ToString() ?? string.Empty,
            StatusName = args["statusName"]?.ToString() ?? "Submitted"
        };

        var (ok, error) = await expenseService.UpdateExpenseStatusAsync(id, request, ct);
        return JsonSerializer.Serialize(new { success = ok, error });
    }

    private async Task<JsonObject> SendChatRequestAsync(JsonObject payload, CancellationToken ct)
    {
        TokenCredential credential = string.IsNullOrWhiteSpace(_managedIdentityClientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = _managedIdentityClientId
            });

        var token = await credential.GetTokenAsync(new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]), ct);
        var client = httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        req.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var res = await client.SendAsync(req, ct);
        var content = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();
        return JsonNode.Parse(content)?.AsObject() ?? new JsonObject();
    }

    private static JsonArray BuildTools() =>
    [
        new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = "get_expenses",
                ["description"] = "Retrieve expense records, optionally filtered by status or user email",
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["statusName"] = new JsonObject { ["type"] = "string", ["description"] = "Draft/Submitted/Approved/Rejected" },
                        ["userEmail"] = new JsonObject { ["type"] = "string", ["description"] = "User email" }
                    }
                }
            }
        },
        new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = "create_expense",
                ["description"] = "Create a new expense record",
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["required"] = new JsonArray("userEmail", "categoryName", "amount", "expenseDate"),
                    ["properties"] = new JsonObject
                    {
                        ["userEmail"] = new JsonObject { ["type"] = "string" },
                        ["categoryName"] = new JsonObject { ["type"] = "string" },
                        ["amount"] = new JsonObject { ["type"] = "number" },
                        ["expenseDate"] = new JsonObject { ["type"] = "string", ["description"] = "YYYY-MM-DD" },
                        ["description"] = new JsonObject { ["type"] = "string" }
                    }
                }
            }
        },
        new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = "update_expense_status",
                ["description"] = "Approve, reject or submit an expense",
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["required"] = new JsonArray("expenseId", "managerEmail", "statusName"),
                    ["properties"] = new JsonObject
                    {
                        ["expenseId"] = new JsonObject { ["type"] = "integer" },
                        ["managerEmail"] = new JsonObject { ["type"] = "string" },
                        ["statusName"] = new JsonObject { ["type"] = "string" }
                    }
                }
            }
        }
    ];
}
