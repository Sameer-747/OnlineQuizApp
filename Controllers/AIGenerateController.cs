using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OnlineQuizApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/AIGenerate")]
    public class AIGenerateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AIGenerateController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // GET: /Admin/AIGenerate?quizId=1
        [HttpGet("")]
        public async Task<IActionResult> Index(int quizId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return NotFound();

            ViewBag.QuizId = quizId;
            ViewBag.QuizTitle = quiz.Title;
            return View();
        }

        // POST: /Admin/AIGenerate/Generate
        [HttpPost("Generate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Topic))
                return BadRequest(new { error = "Topic is required." });

            var apiKey = _configuration["Groq:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, new { error = "API key not configured. Add Groq:ApiKey to appsettings.json." });

            var prompt = $@"Generate exactly {request.Count} multiple choice questions on the topic: ""{request.Topic}"".
Difficulty: {request.Difficulty}.
Rules:
- Each question must have exactly 4 options (A, B, C, D).
- Only one option is correct.
- Questions should be clear and educational.
- Return ONLY a JSON array, no extra text, no markdown, no code blocks.
- Format: [{{""text"":""Question text?"",""options"":[""Option A"",""Option B"",""Option C"",""Option D""],""correctIndex"":0}}]
- correctIndex is 0-based (0=A, 1=B, 2=C, 3=D).";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var body = JsonSerializer.Serialize(new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 4000,
                temperature = 0.7
            });

            var response = await client.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                new StringContent(body, Encoding.UTF8, "application/json")
            );

            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode(500, new { error = $"AI API error: {response.StatusCode}" });

            using var doc = JsonDocument.Parse(responseText);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var cleaned = text.Replace("```json", "").Replace("```", "").Trim();

            return Content(cleaned, "application/json");
        }

        // POST: /Admin/AIGenerate/SaveQuestions
        [HttpPost("SaveQuestions")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveQuestions([FromBody] SaveQuestionsRequest request)
        {
            if (request?.Questions == null || request.Questions.Count == 0)
                return BadRequest(new { error = "No questions provided." });

            var quiz = await _context.Quizzes.FindAsync(request.QuizId);
            if (quiz == null) return NotFound(new { error = "Quiz not found." });

            int saved = 0;
            foreach (var q in request.Questions)
            {
                if (string.IsNullOrWhiteSpace(q.Text)) continue;

                var question = new Question
                {
                    QuizId = request.QuizId,
                    Text = q.Text.Trim(),
                    Options = q.Options.Select((o, i) => new Option
                    {
                        Text = o.Text.Trim(),
                        IsCorrect = i == q.CorrectIndex
                    }).ToList()
                };

                _context.Questions.Add(question);
                saved++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { saved });
        }
    }

    public class GenerateRequest
    {
        public string Topic { get; set; } = string.Empty;
        public int Count { get; set; } = 5;
        public string Difficulty { get; set; } = "medium";
    }

    public class SaveQuestionsRequest
    {
        public int QuizId { get; set; }
        public List<AIQuestion> Questions { get; set; } = new();
    }

    public class AIQuestion
    {
        public string Text { get; set; } = string.Empty;
        public List<AIOption> Options { get; set; } = new();
        public int CorrectIndex { get; set; }
    }

    public class AIOption
    {
        public string Text { get; set; } = string.Empty;
    }
}
