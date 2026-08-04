using System.ComponentModel.DataAnnotations;

namespace OnlineQuizApp.Models
{
    public enum BadgeType
    {
        PerfectScore,      // 100% on any quiz
        SharpShooter,      // 80%+ on first attempt
        QuizMaster,        // Completed 5+ quizzes
        OnFire,            // 3 quizzes in a row with 80%+
        FastFinisher,      // Completed quiz in under half the time limit
        FirstQuiz          // Completed first quiz ever
    }

    public class StudentBadge
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public BadgeType BadgeType { get; set; }

        public int? QuizAttemptId { get; set; }
        public QuizAttempt? QuizAttempt { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

        // Helper display properties
        public string Name => BadgeType switch
        {
            BadgeType.PerfectScore   => "Perfect Score",
            BadgeType.SharpShooter   => "Sharp Shooter",
            BadgeType.QuizMaster     => "Quiz Master",
            BadgeType.OnFire         => "On Fire",
            BadgeType.FastFinisher   => "Fast Finisher",
            BadgeType.FirstQuiz      => "First Quiz",
            _                        => "Badge"
        };

        public string Icon => BadgeType switch
        {
            BadgeType.PerfectScore   => "🥇",
            BadgeType.SharpShooter   => "🎯",
            BadgeType.QuizMaster     => "📚",
            BadgeType.OnFire         => "🔥",
            BadgeType.FastFinisher   => "⚡",
            BadgeType.FirstQuiz      => "🌟",
            _                        => "🏅"
        };

        public string Description => BadgeType switch
        {
            BadgeType.PerfectScore   => "Scored 100% on a quiz!",
            BadgeType.SharpShooter   => "Scored 80% or above on first attempt!",
            BadgeType.QuizMaster     => "Completed 5 or more quizzes!",
            BadgeType.OnFire         => "Scored 80%+ on 3 quizzes in a row!",
            BadgeType.FastFinisher   => "Finished the quiz in under half the time limit!",
            BadgeType.FirstQuiz      => "Completed your very first quiz!",
            _                        => "Achievement unlocked!"
        };

        public string Color => BadgeType switch
        {
            BadgeType.PerfectScore   => "#FFD700",
            BadgeType.SharpShooter   => "#FF6B6B",
            BadgeType.QuizMaster     => "#4ECDC4",
            BadgeType.OnFire         => "#FF8C00",
            BadgeType.FastFinisher   => "#A855F7",
            BadgeType.FirstQuiz      => "#22C55E",
            _                        => "#6B7280"
        };
    }
}
