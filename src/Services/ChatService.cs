using System.Text;
using System.Text.Json;

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

        public async Task<string> SendMessageAsync(string userMessage)
        {
            try
            {
                var endpointUrl = _configuration["FoundryAI:EndpointUrl"];
                var apiKey = _configuration["FoundryAI:ApiKey"];
                var modelName = _configuration["FoundryAI:ModelName"] ?? "phi4";

                if (string.IsNullOrEmpty(endpointUrl))
                {
                    _logger.LogError("FoundryAI:EndpointUrl is not configured");
                    return "Error: Chat service is not properly configured. Please contact the administrator.";
                }

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("FoundryAI:ApiKey is not configured");
                    return "Error: Chat service is not properly configured. Please contact the administrator.";
                }

                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "user", content = userMessage }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                _logger.LogInformation("Sending message to Foundry AI endpoint: {EndpointUrl}", endpointUrl);

                var response = await _httpClient.PostAsync(endpointUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Foundry AI API request failed with status {StatusCode}: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    return $"Error: Failed to get response from AI service (Status: {response.StatusCode})";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Received response from Foundry AI: {ResponseContent}", responseContent);

                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Try to extract the message from common response formats
                if (responseJson.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) && 
                        message.TryGetProperty("content", out var messageContent))
                    {
                        return messageContent.GetString() ?? "No response from AI";
                    }
                }

                // If the above format doesn't work, try alternative format
                if (responseJson.TryGetProperty("message", out var directMessage))
                {
                    return directMessage.GetString() ?? "No response from AI";
                }

                _logger.LogWarning("Unexpected response format from Foundry AI");
                return "Error: Received unexpected response format from AI service";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while calling Foundry AI");
                return "Error: Unable to connect to AI service. Please check your connection and try again.";
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while processing Foundry AI response");
                return "Error: Unable to parse AI service response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ChatService");
                return "Error: An unexpected error occurred. Please try again later.";
            }
        }
    }
}
