using Microsoft.AspNetCore.Mvc;
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
            _logger.LogInformation("Loading chat page");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                _logger.LogWarning("Received empty message request");
                return Json(new { success = false, message = "Message cannot be empty" });
            }

            _logger.LogInformation("Processing chat message: {Message}", request.Message);

            var response = await _chatService.SendMessageAsync(request.Message);

            return Json(new { success = true, message = response });
        }
    }

    public class ChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
