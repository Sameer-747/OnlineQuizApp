using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    public class QuizAttempt
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz? Quiz { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        // Exam-integrity signals for proctored Test Event attempts (always 0/false for regular quizzes).
        public int TabSwitchCount { get; set; }
        public bool AutoSubmitted { get; set; }
        // Human-readable reason when AutoSubmitted is true, e.g. "3 tab/app switches" or
        // "Camera: multiple faces detected". Null for manual submissions.
        public string? ViolationReason { get; set; }

        public ICollection<UserAnswer> Answers { get; set; } = new List<UserAnswer>();
    }
}
