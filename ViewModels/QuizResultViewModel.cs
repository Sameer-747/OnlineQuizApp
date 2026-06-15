namespace OnlineQuizApp.ViewModels
{
    public class QuizResultViewModel
    {
        public int AttemptId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<QuestionResultViewModel> QuestionResults { get; set; } = new();
    }

    public class QuestionResultViewModel
    {
        public string QuestionText { get; set; } = string.Empty;
        public string? SelectedOptionText { get; set; }
        public string CorrectOptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    // Used for posting answers back from the quiz form
    public class QuizSubmissionViewModel
    {
        public int QuizId { get; set; }
        public List<QuestionAnswerSubmission> Answers { get; set; } = new();
    }

    public class QuestionAnswerSubmission
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
    }
}
