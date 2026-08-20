using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    // A single piece of camera-based evidence captured during a proctored Test Event exam
    // (e.g. no face visible, multiple faces, a phone in frame). Stored independently of
    // QuizAttempt since a violation can happen before the attempt row exists (mid-exam,
    // before Submit is ever posted).
    public class ExamSnapshot
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz? Quiz { get; set; }

        [Required, StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        // Base64-encoded JPEG frame from the student's webcam at the moment of violation.
        // Stored in Postgres (not on disk) since Render's free web service filesystem is
        // ephemeral and would lose anything written there on every redeploy/restart.
        public string ImageData { get; set; } = string.Empty;

        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    }
}
