namespace OnlineQuizApp.ViewModels
{
    // One row on the student-facing "My Tests" page: the language they were assigned
    // within a given TestEvent, and whether/when they can take it.
    public class MyTestEventRow
    {
        public int TestEventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int QuizId { get; set; }
        public string Language { get; set; } = string.Empty;
        public DateTime StartTimeIst { get; set; }
        public DateTime EndTimeIst { get; set; }
        public string Status { get; set; } = string.Empty; // Upcoming / Active / Expired
        public bool Attempted { get; set; }
        public int? AttemptId { get; set; }
    }
}
