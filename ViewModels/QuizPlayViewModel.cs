namespace OnlineQuizApp.ViewModels
{
    public class QuizPlayViewModel
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public List<QuestionPlayViewModel> Questions { get; set; } = new();
    }

    public class QuestionPlayViewModel
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<OptionPlayViewModel> Options { get; set; } = new();
        public int? SelectedOptionId { get; set; }
    }

    public class OptionPlayViewModel
    {
        public int OptionId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
