using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using OpenAI.Chat;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly ExpenseService _expenseService;
    private readonly UserService _userService;
    private readonly CategoryService _categoryService;
    private AzureOpenAIClient? _openAIClient;
    private readonly bool _isConfigured;
    private readonly string? _deploymentName;

    public ChatService(
        IConfiguration configuration,
        ILogger<ChatService> logger,
        ExpenseService expenseService,
        UserService userService,
        CategoryService categoryService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;
        _userService = userService;
        _categoryService = categoryService;

        var endpoint = configuration["OpenAI:Endpoint"];
        _deploymentName = configuration["OpenAI:DeploymentName"];
        var managedIdentityClientId = configuration["ManagedIdentityClientId"];

        _isConfigured = !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(_deploymentName);

        if (_isConfigured)
        {
            try
            {
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

                _openAIClient = new AzureOpenAIClient(new Uri(endpoint!), credential);
                _logger.LogInformation("Chat service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
                _isConfigured = false;
            }
        }
        else
        {
            _logger.LogWarning("Chat service not configured.");
        }
    }

    public async Task<ChatResponse> SendMessageAsync(List<Models.ChatMessage> messages)
    {
        if (!_isConfigured || _openAIClient == null || string.IsNullOrEmpty(_deploymentName))
        {
            return new ChatResponse
            {
                Success = false,
                Message = "Chat functionality is not available. Please deploy with GenAI resources using deploy-with-chat.sh to enable AI-powered chat features.",
                Error = "GenAI resources not deployed"
            };
        }

        try
        {
            var chatClient = _openAIClient.GetChatClient(_deploymentName);
            
            var chatMessages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(@"You are a helpful assistant for an Expense Management System.")
            };

            foreach (var msg in messages)
            {
                if (msg.Role == "user")
                    chatMessages.Add(new UserChatMessage(msg.Content));
                else if (msg.Role == "assistant")
                    chatMessages.Add(new AssistantChatMessage(msg.Content));
            }

            var completionOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 800,
                Temperature = 0.7f
            };

            var response = await chatClient.CompleteChatAsync(chatMessages, completionOptions);

            return new ChatResponse
            {
                Success = true,
                Message = response.Value.Content[0].Text
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat service");
            return new ChatResponse
            {
                Success = false,
                Message = "An error occurred. Please try again.",
                Error = ex.Message
            };
        }
    }
}
