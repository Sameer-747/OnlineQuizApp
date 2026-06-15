using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    public class Question
    {
        public int Id { get; set; }

        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz? Quiz { get; set; }

        [Required, StringLength(500)]
        public string Text { get; set; } = string.Empty;

        public ICollection<Option> Options { get; set; } = new List<Option>();
    }
}
