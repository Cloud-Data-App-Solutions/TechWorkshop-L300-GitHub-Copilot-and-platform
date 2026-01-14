using System.Text;
using System.Text.Json;
using ZavaStorefront.Models;

namespace ZavaStorefront.Services
{
    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;

        public ChatService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> SendMessageAsync(string userMessage, List<ChatMessage> conversationHistory)
        {
            try
            {
                var endpoint = _configuration["Foundry:Endpoint"];
                var apiKey = _configuration["Foundry:ApiKey"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Foundry endpoint or API key is not configured");
                    return "Error: Chat service is not configured. Please check appsettings.json";
                }

                // Add the user message to conversation history
                conversationHistory.Add(new ChatMessage { Role = "user", Content = userMessage });

                // Prepare the request payload
                var requestPayload = new
                {
                    messages = conversationHistory.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                    max_tokens = 800,
                    temperature = 0.7,
                    top_p = 0.95
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Add API key to request headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

                _logger.LogInformation("Sending message to Foundry Phi4 endpoint");

                var response = await _httpClient.PostAsync(endpoint, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Foundry API request failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return $"Error: Failed to get response from AI service (Status: {response.StatusCode})";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);

                // Extract the assistant's message from the response
                var assistantMessage = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrEmpty(assistantMessage))
                {
                    _logger.LogWarning("Received empty response from Foundry API");
                    return "Error: Received empty response from AI service";
                }

                // Add assistant response to conversation history
                conversationHistory.Add(new ChatMessage { Role = "assistant", Content = assistantMessage });

                _logger.LogInformation("Successfully received response from Foundry Phi4");
                return assistantMessage;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while communicating with Foundry API");
                return "Error: Network error while communicating with AI service";
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse response from Foundry API");
                return "Error: Failed to parse response from AI service";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ChatService");
                return "Error: An unexpected error occurred";
            }
        }
    }
}
