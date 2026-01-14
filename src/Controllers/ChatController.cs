using Microsoft.AspNetCore.Mvc;
using ZavaStorefront.Models;
using ZavaStorefront.Services;

namespace ZavaStorefront.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;
        private readonly ChatService _chatService;

        public ChatController(ILogger<ChatController> logger, ChatService chatService)
        {
            _logger = logger;
            _chatService = chatService;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Loading Chat page");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            _logger.LogInformation("Sending message to chat service");

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            // Get conversation history from session or create new
            var conversationHistory = HttpContext.Session.GetObjectFromJson<List<ChatMessage>>("ConversationHistory") 
                ?? new List<ChatMessage>();

            var response = await _chatService.SendMessageAsync(request.Message, conversationHistory);

            // Save updated conversation history to session
            HttpContext.Session.SetObjectAsJson("ConversationHistory", conversationHistory);

            return Ok(new { response });
        }

        [HttpPost]
        public IActionResult ClearHistory()
        {
            _logger.LogInformation("Clearing chat history");
            HttpContext.Session.Remove("ConversationHistory");
            return Ok(new { success = true });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}

// Extension methods for session management
public static class SessionExtensions
{
    public static void SetObjectAsJson(this ISession session, string key, object value)
    {
        session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));
    }

    public static T? GetObjectFromJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(value);
    }
}
