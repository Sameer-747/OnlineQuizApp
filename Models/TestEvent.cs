using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    // A scheduled, multi-language test. The admin generates one quiz per language via AI,
    // then each student in the section is randomly assigned exactly one language quiz to take.
    public class TestEvent
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        // Null = a super-admin-created "global" event spanning multiple sections (see
        // SectionLanguages below for the per-section language mapping). Set = a regular,
        // single-section event created by that section's admin (or by the super admin for
        // just that one section).
        public int? SectionId { get; set; }

        [ForeignKey(nameof(SectionId))]
        public Section? Section { get; set; }

        [NotMapped]
        public bool IsGlobal => SectionId == null;

        // Only populated for global events: which section must attempt which language (and
        // therefore which generated quiz). Empty for regular single-section events.
        public ICollection<TestEventSectionLanguage> SectionLanguages { get; set; } = new List<TestEventSectionLanguage>();

        // Window during which assigned students may start their quiz. Stored in UTC.
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // The per-language quizzes generated for this event.
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

        // Which student got assigned which language quiz.
        public ICollection<TestEventAssignment> Assignments { get; set; } = new List<TestEventAssignment>();

        [NotMapped]
        public bool HasStarted => DateTime.UtcNow >= StartTime;

        [NotMapped]
        public bool HasEnded => DateTime.UtcNow > EndTime;

        [NotMapped]
        public string Status => !HasStarted ? "Upcoming" : (HasEnded ? "Expired" : "Active");
    }
}
