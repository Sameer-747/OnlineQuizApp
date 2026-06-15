using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuizApp.Models
{
    public class UserAnswer
    {
        public int Id { get; set; }

        public int QuizAttemptId { get; set; }

        [ForeignKey(nameof(QuizAttemptId))]
        public QuizAttempt? QuizAttempt { get; set; }

        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public Question? Question { get; set; }

        public int? SelectedOptionId { get; set; }

        [ForeignKey(nameof(SelectedOptionId))]
        public Option? SelectedOption { get; set; }
    }
}
