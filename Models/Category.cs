using System.ComponentModel.DataAnnotations;

namespace OnlineQuizApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // The section this category belongs to. Null = global/shared category (legacy or super-admin created).
        public int? SectionId { get; set; }
        public Section? Section { get; set; }

        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
