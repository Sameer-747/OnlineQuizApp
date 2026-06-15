using System.ComponentModel.DataAnnotations;

namespace OnlineQuizApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
