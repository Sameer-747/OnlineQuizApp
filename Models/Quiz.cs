using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    public class Quiz
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Range(1, 240)]
        public int DurationMinutes { get; set; } = 10;

        // The section this quiz belongs to. Null = visible to everyone (legacy/global quizzes).
        public int? SectionId { get; set; }
        public Section? Section { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
