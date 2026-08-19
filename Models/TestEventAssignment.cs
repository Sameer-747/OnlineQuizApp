using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    // Records which language-quiz a given student was randomly assigned within a TestEvent.
    // One row per (TestEventId, UserId).
    public class TestEventAssignment
    {
        public int Id { get; set; }

        public int TestEventId { get; set; }

        [ForeignKey(nameof(TestEventId))]
        public TestEvent? TestEvent { get; set; }

        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz? Quiz { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
