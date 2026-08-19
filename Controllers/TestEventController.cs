using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;
using OnlineQuizApp.ViewModels;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OnlineQuizApp.Controllers
{
    // Admin-only: create multi-language "test events" for a section. The admin picks a set of
    // languages, AI generates one quiz per language, and every student currently in the section
    // is randomly (but evenly) assigned exactly one language to attempt within a start/end window.
    [Authorize(Roles = "Admin")]
    [Route("Admin/TestEvents")]
    public class TestEventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private const string SuperAdminEmail = "admin@quizapp.com";
        private const string AiCategoryName = "AI Language Tests";

        public TestEventController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private bool IsSuperAdmin() => User.Identity?.Name?.ToLower() == SuperAdminEmail.ToLower();

        private async Task<(bool isSuper, int? sectionId)> GetScopeAsync()
        {
            if (IsSuperAdmin()) return (true, null);

            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            return (false, user?.SectionId);
        }

        // GET: /Admin/TestEvents
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var (isSuper, sectionId) = await GetScopeAsync();

            var query = _context.TestEvents
                .Include(te => te.Section)
                .Include(te => te.Quizzes)
                .Include(te => te.Assignments)
                .AsQueryable();

            if (!isSuper)
            {
                query = query.Where(te => te.SectionId == sectionId);
                if (sectionId == null)
                    TempData["Error"] = "You are not yet assigned to a section. Ask the super admin to assign you one before creating test events.";
            }

            var events = await query.OrderByDescending(te => te.CreatedAt).ToListAsync();
            return View(events);
        }

        // GET: /Admin/TestEvents/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            if (!isSuper && sectionId == null)
            {
                TempData["Error"] = "You are not yet assigned to a section. Ask the super admin to assign you one before creating test events.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.IsSuper = isSuper;
            if (isSuper)
            {
                ViewBag.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
            }
            else
            {
                var section = await _context.Sections.FindAsync(sectionId!.Value);
                ViewBag.OwnSectionName = section?.Name ?? "—";
            }

            return View();
        }

        // POST: /Admin/TestEvents/CreateAndPost  (AJAX - creates the event, generates every language
        // via AI, and randomly/evenly assigns the section's current students, all in one step)
        [HttpPost("CreateAndPost")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAndPost([FromBody] CreateTestEventRequest request)
        {
            var (isSuper, adminSectionId) = await GetScopeAsync();

            int sectionId;
            if (isSuper)
            {
                if (request.SectionId == null)
                    return BadRequest(new { error = "Please choose a section." });
                sectionId = request.SectionId.Value;
            }
            else
            {
                if (adminSectionId == null)
                    return BadRequest(new { error = "You are not yet assigned to a section." });
                sectionId = adminSectionId.Value;
            }

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { error = "Title is required." });

            var languages = (request.Languages ?? new List<string>())
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (languages.Count == 0)
                return BadRequest(new { error = "Select at least one language." });

            var apiKey = _configuration["Groq:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, new { error = "API key not configured. Add Groq:ApiKey to appsettings.json." });

            var ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

            if (!DateTime.TryParse(request.StartTime, out var startLocal) ||
                !DateTime.TryParse(request.EndTime, out var endLocal))
            {
                return BadRequest(new { error = "Invalid start/end time." });
            }

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), ist);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), ist);

            if (endUtc <= startUtc)
                return BadRequest(new { error = "End time must be after start time." });

            var durationMinutes = request.DurationMinutes is >= 1 and <= 240 ? request.DurationMinutes : 15;
            var questionCount = request.QuestionCount is >= 1 and <= 30 ? request.QuestionCount : 10;
            var difficulty = string.IsNullOrWhiteSpace(request.Difficulty) ? "medium" : request.Difficulty;

            // Find or create a shared category to hold these AI-generated language quizzes.
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.SectionId == sectionId && c.Name == AiCategoryName);
            if (category == null)
            {
                category = new Category { Name = AiCategoryName, SectionId = sectionId };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }

            var testEvent = new TestEvent
            {
                Title = request.Title.Trim(),
                SectionId = sectionId,
                StartTime = startUtc,
                EndTime = endUtc,
                CreatedByUserId = _userManager.GetUserId(User),
                CreatedAt = DateTime.UtcNow
            };
            _context.TestEvents.Add(testEvent);
            await _context.SaveChangesAsync();

            var createdQuizzes = new List<Quiz>();
            var failedLanguages = new List<string>();
            bool first = true;

            foreach (var language in languages)
            {
                // Groq's free-tier rate limit trips almost immediately if requests fire
                // back-to-back, so space them out a little.
                if (!first) await Task.Delay(2000);
                first = false;

                var generated = await GenerateQuestionsForLanguageAsync(language, questionCount, difficulty);
                if (generated == null || generated.Count == 0)
                {
                    failedLanguages.Add(language);
                    continue;
                }

                var quiz = new Quiz
                {
                    Title = language,
                    CategoryId = category.Id,
                    SectionId = sectionId,
                    DurationMinutes = durationMinutes,
                    TestEventId = testEvent.Id,
                    CreatedByUserId = _userManager.GetUserId(User)
                };

                foreach (var q in generated)
                {
                    if (string.IsNullOrWhiteSpace(q.Text) || q.Options == null || q.Options.Count < 2) continue;

                    var question = new Question { Text = q.Text.Trim() };
                    for (int i = 0; i < q.Options.Count; i++)
                    {
                        question.Options.Add(new Option
                        {
                            Text = q.Options[i]?.Trim() ?? string.Empty,
                            IsCorrect = i == q.CorrectIndex
                        });
                    }
                    quiz.Questions.Add(question);
                }

                if (quiz.Questions.Count == 0)
                {
                    failedLanguages.Add(language);
                    continue;
                }

                _context.Quizzes.Add(quiz);
                createdQuizzes.Add(quiz);
            }

            if (createdQuizzes.Count == 0)
            {
                _context.TestEvents.Remove(testEvent);
                await _context.SaveChangesAsync();
                return StatusCode(500, new { error = "AI generation failed for every language. Please try again." });
            }

            await _context.SaveChangesAsync();

            // Assign every current student in the section one language, as evenly as possible.
            var students = await GetStudentsInSectionAsync(sectionId);
            var quizIds = createdQuizzes.Select(q => q.Id).ToList();
            var pool = BuildBalancedShuffledQuizPool(quizIds, students.Count);

            for (int i = 0; i < students.Count; i++)
            {
                _context.TestEventAssignments.Add(new TestEventAssignment
                {
                    TestEventId = testEvent.Id,
                    UserId = students[i].Id,
                    QuizId = pool[i]
                });
            }
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                eventId = testEvent.Id,
                languagesGenerated = createdQuizzes.Select(q => q.Title).ToList(),
                failedLanguages,
                studentsAssigned = students.Count
            });
        }

        // GET: /Admin/TestEvents/Results/5
        [HttpGet("Results/{id:int}")]
        public async Task<IActionResult> Results(int id)
        {
            var (isSuper, sectionId) = await GetScopeAsync();

            var testEvent = await _context.TestEvents
                .Include(te => te.Section)
                .Include(te => te.Quizzes)
                .FirstOrDefaultAsync(te => te.Id == id);

            if (testEvent == null) return NotFound();
            if (!isSuper && testEvent.SectionId != sectionId) return Forbid();

            var assignments = await _context.TestEventAssignments
                .Include(a => a.User)
                .Include(a => a.Quiz)
                .Where(a => a.TestEventId == id)
                .ToListAsync();

            var quizIds = testEvent.Quizzes.Select(q => q.Id).ToList();
            var userIds = assignments.Select(a => a.UserId).ToList();

            var attempts = await _context.QuizAttempts
                .Where(a => quizIds.Contains(a.QuizId) && userIds.Contains(a.UserId))
                .ToListAsync();

            var rows = new List<TestEventResultRow>();
            foreach (var assignment in assignments)
            {
                var attempt = attempts
                    .Where(a => a.QuizId == assignment.QuizId && a.UserId == assignment.UserId)
                    .OrderByDescending(a => a.CompletedAt)
                    .FirstOrDefault();

                rows.Add(new TestEventResultRow
                {
                    Language = assignment.Quiz?.Title ?? "—",
                    QuizId = assignment.QuizId,
                    StudentName = assignment.User?.FullName ?? assignment.User?.Email ?? "—",
                    RollNumber = assignment.User?.RollNumber,
                    Attempted = attempt != null,
                    Score = attempt?.Score ?? 0,
                    TotalQuestions = attempt?.TotalQuestions ?? 0,
                    Percentage = attempt != null && attempt.TotalQuestions > 0
                        ? (attempt.Score * 100.0 / attempt.TotalQuestions) : 0,
                    CompletedAt = attempt?.CompletedAt
                });
            }

            // Rank within each language: completed attempts only, best percentage first,
            // ties broken by score then by who finished first.
            foreach (var group in rows.GroupBy(r => r.Language))
            {
                int rank = 1;
                foreach (var row in group.Where(r => r.Attempted)
                             .OrderByDescending(r => r.Percentage)
                             .ThenByDescending(r => r.Score)
                             .ThenBy(r => r.CompletedAt))
                {
                    row.Rank = rank++;
                }
            }

            ViewBag.TestEvent = testEvent;
            return View(rows.OrderBy(r => r.Language).ThenBy(r => r.Rank ?? int.MaxValue).ToList());
        }

        // POST: /Admin/TestEvents/Delete/5
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (isSuper, sectionId) = await GetScopeAsync();

            var testEvent = await _context.TestEvents
                .Include(te => te.Quizzes)
                .FirstOrDefaultAsync(te => te.Id == id);

            if (testEvent == null) return NotFound();
            if (!isSuper && testEvent.SectionId != sectionId) return Forbid();

            var quizIds = testEvent.Quizzes.Select(q => q.Id).ToList();
            var hasAttempts = quizIds.Count > 0 &&
                await _context.QuizAttempts.AnyAsync(a => quizIds.Contains(a.QuizId));

            // Attempts on a still-active/upcoming event are left alone for safety - only an
            // expired event's attempt history can be force-deleted along with it.
            if (hasAttempts && !testEvent.HasEnded)
            {
                TempData["Error"] = "Can't delete this test event: some students have already attempted it, and it hasn't expired yet.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (hasAttempts)
                {
                    var attempts = await _context.QuizAttempts
                        .Where(a => quizIds.Contains(a.QuizId))
                        .ToListAsync();
                    _context.QuizAttempts.RemoveRange(attempts);
                    await _context.SaveChangesAsync();
                }

                _context.TestEvents.Remove(testEvent);
                await _context.SaveChangesAsync();

                TempData["Success"] = hasAttempts
                    ? "Expired test event deleted, along with its recorded attempts."
                    : "Test event deleted.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Couldn't delete this test event. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<ApplicationUser>> GetStudentsInSectionAsync(int sectionId)
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUserIds = new HashSet<string>();
            if (adminRole != null)
            {
                adminUserIds = (await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync()).ToHashSet();
            }

            return await _context.Users
                .Where(u => u.SectionId == sectionId && !adminUserIds.Contains(u.Id))
                .OrderBy(u => u.RollNumber)
                .ToListAsync();
        }

        // Builds a list of quizIds of the given length where each quiz appears as evenly as
        // possible, then shuffles it so the pairing with students (in original order) is random.
        private static List<int> BuildBalancedShuffledQuizPool(List<int> quizIds, int count)
        {
            var pool = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                pool.Add(quizIds[i % quizIds.Count]);
            }

            var rng = Random.Shared;
            for (int j = pool.Count - 1; j > 0; j--)
            {
                int k = rng.Next(j + 1);
                (pool[j], pool[k]) = (pool[k], pool[j]);
            }

            return pool;
        }

        private async Task<List<GeneratedAiQuestion>?> GenerateQuestionsForLanguageAsync(string language, int count, string difficulty)
        {
            var apiKey = _configuration["Groq:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var prompt = $@"Generate exactly {count} multiple choice questions testing programming knowledge of: ""{language}"".
Difficulty: {difficulty}.
Rules:
- Each question must have exactly 4 options (A, B, C, D).
- Only one option is correct.
- Questions must be technically accurate and specific to {language}.
- Return ONLY a JSON array, no extra text, no markdown, no code blocks.
- Format: [{{""text"":""Question text?"",""options"":[""Option A"",""Option B"",""Option C"",""Option D""],""correctIndex"":0}}]
- correctIndex is 0-based (0=A, 1=B, 2=C, 3=D).";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var body = JsonSerializer.Serialize(new
            {
                model = "openai/gpt-oss-120b",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 4000,
                temperature = 0.7
            });

            const int maxAttempts = 4;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(
                        "https://api.groq.com/openai/v1/chat/completions",
                        new StringContent(body, Encoding.UTF8, "application/json"));
                }
                catch
                {
                    return null;
                }

                // Rate-limited or a transient server hiccup: back off and try again rather
                // than giving up on the whole language.
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                    (int)response.StatusCode >= 500)
                {
                    if (attempt == maxAttempts) return null;

                    TimeSpan wait = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 2); // 4s, 8s, 16s
                    if (response.Headers.RetryAfter?.Delta is TimeSpan retryAfter)
                        wait = retryAfter;

                    await Task.Delay(wait);
                    continue;
                }

                if (!response.IsSuccessStatusCode) return null;

                try
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseText);
                    var text = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "";

                    var cleaned = text.Replace("```json", "").Replace("```", "").Trim();

                    return JsonSerializer.Deserialize<List<GeneratedAiQuestion>>(
                        cleaned, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }

    public class CreateTestEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public int? SectionId { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int DurationMinutes { get; set; } = 15;
        public int QuestionCount { get; set; } = 10;
        public string Difficulty { get; set; } = "medium";
        public List<string> Languages { get; set; } = new();
    }

    public class GeneratedAiQuestion
    {
        public string Text { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectIndex { get; set; }
    }
}
