using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    public class Option
    {
        public int Id { get; set; }

        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public Question? Question { get; set; }

        [Required, StringLength(300)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
