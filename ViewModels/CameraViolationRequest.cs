namespace OnlineQuizApp.ViewModels
{
    // Posted from the exam page's camera-monitoring JS when a sustained violation
    // (no face / multiple faces / phone in frame) is detected.
    public class CameraViolationRequest
    {
        public int QuizId { get; set; }
        public string Reason { get; set; } = string.Empty;
        // Base64-encoded JPEG data URL (or raw base64) of the offending frame.
        public string ImageData { get; set; } = string.Empty;
    }
}
