using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    // For a super-admin-created global TestEvent: records that a given Section must attempt
    // a given Language, via a specific generated Quiz. One row per section in the event.
    public class TestEventSectionLanguage
    {
        public int Id { get; set; }

        public int TestEventId { get; set; }

        [ForeignKey(nameof(TestEventId))]
        public TestEvent? TestEvent { get; set; }

        public int SectionId { get; set; }

        [ForeignKey(nameof(SectionId))]
        public Section? Section { get; set; }

        [Required, StringLength(100)]
        public string Language { get; set; } = string.Empty;

        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz? Quiz { get; set; }
    }
}
