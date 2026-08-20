namespace OnlineQuizApp.ViewModels
{
    // One row in the TestEvent results dashboard: one student's outcome for their assigned language.
    public class TestEventResultRow
    {
        public string Language { get; set; } = string.Empty;
        public int QuizId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public bool Attempted { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public double Percentage { get; set; }
        public int? Rank { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool AutoSubmitted { get; set; }
        public int TabSwitchCount { get; set; }
        public string? ViolationReason { get; set; }
        public int? LatestSnapshotId { get; set; }
    }
}
