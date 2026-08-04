using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Services
{
    public class BadgeService
    {
        private readonly ApplicationDbContext _context;

        public BadgeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AwardBadgesAsync(QuizAttempt attempt)
        {
            var userId = attempt.UserId;
            var newBadges = new List<StudentBadge>();

            double percentage = attempt.TotalQuestions > 0
                ? (double)attempt.Score / attempt.TotalQuestions * 100
                : 0;

            var allAttempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            var existingBadgeTypes = await _context.StudentBadges
                .Where(b => b.UserId == userId)
                .Select(b => b.BadgeType)
                .ToListAsync();

            // 1. First Quiz
            if (allAttempts.Count == 1 && !existingBadgeTypes.Contains(BadgeType.FirstQuiz))
            {
                newBadges.Add(new StudentBadge { UserId = userId, BadgeType = BadgeType.FirstQuiz, QuizAttemptId = attempt.Id });
            }

            // 2. Perfect Score (100%)
            if (percentage == 100 && !existingBadgeTypes.Contains(BadgeType.PerfectScore))
            {
                newBadges.Add(new StudentBadge { UserId = userId, BadgeType = BadgeType.PerfectScore, QuizAttemptId = attempt.Id });
            }

            // 3. Sharp Shooter (80%+)
            if (percentage >= 80 && !existingBadgeTypes.Contains(BadgeType.SharpShooter))
            {
                newBadges.Add(new StudentBadge { UserId = userId, BadgeType = BadgeType.SharpShooter, QuizAttemptId = attempt.Id });
            }

            // 4. Quiz Master (5+ quizzes completed)
            if (allAttempts.Count >= 5 && !existingBadgeTypes.Contains(BadgeType.QuizMaster))
            {
                newBadges.Add(new StudentBadge { UserId = userId, BadgeType = BadgeType.QuizMaster, QuizAttemptId = attempt.Id });
            }

            // 5. On Fire (3 consecutive quizzes with 80%+)
            if (!existingBadgeTypes.Contains(BadgeType.OnFire) && allAttempts.Count >= 3)
            {
                var lastThree = allAttempts.Take(3).ToList();
                bool allHighScores = lastThree.All(a =>
                    a.TotalQuestions > 0 && (double)a.Score / a.TotalQuestions * 100 >= 80);

                if (allHighScores)
                {
                    newBadges.Add(new StudentBadge { UserId = userId, BadgeType = BadgeType.OnFire, QuizAttemptId = attempt.Id });
                }
            }

            // 6. Fast Finisher (completed in under half the time limit)
            if (!existingBadgeTypes.Contains(BadgeType.FastFinisher) && attempt.Quiz != null)
            {
                var timeTaken = (attempt.CompletedAt ?? DateTime.UtcNow) - attempt.StartedAt;
                var halfTime = TimeSpan.FromMinutes(attempt.Quiz.DurationMinutes / 2.0);
                if (timeTaken < halfTime && percentage >= 50)
                {
                    newBadges.Add(new StudentBadge { UserId = userId, BadgeType = BadgeType.FastFinisher, QuizAttemptId = attempt.Id });
                }
            }

            if (newBadges.Any())
            {
                _context.StudentBadges.AddRange(newBadges);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetCurrentStreakAsync(string userId)
        {
            var attempts = await _context.QuizAttempts
                .Where(a => a.UserId == userId && a.CompletedAt != null)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            if (!attempts.Any()) return 0;

            int streak = 1;
            var prevDate = attempts[0].CompletedAt!.Value.Date;

            for (int i = 1; i < attempts.Count; i++)
            {
                var currentDate = attempts[i].CompletedAt!.Value.Date;
                var diff = (prevDate - currentDate).Days;

                if (diff == 1)
                {
                    streak++;
                    prevDate = currentDate;
                }
                else if (diff == 0)
                {
                    continue; // Same day, don't break streak
                }
                else
                {
                    break; // Gap in days, streak ends
                }
            }

            // Check if streak is still active (last attempt was today or yesterday)
            var daysSinceLast = (DateTime.UtcNow.Date - attempts[0].CompletedAt!.Value.Date).Days;
            if (daysSinceLast > 1) return 0; // Streak broken

            return streak;
        }

        public async Task<int> GetLongestStreakAsync(string userId)
        {
            var attempts = await _context.QuizAttempts
                .Where(a => a.UserId == userId && a.CompletedAt != null)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            if (!attempts.Any()) return 0;

            int longest = 1;
            int current = 1;
            var prevDate = attempts[0].CompletedAt!.Value.Date;

            for (int i = 1; i < attempts.Count; i++)
            {
                var currentDate = attempts[i].CompletedAt!.Value.Date;
                var diff = (prevDate - currentDate).Days;

                if (diff == 1)
                {
                    current++;
                    if (current > longest) longest = current;
                    prevDate = currentDate;
                }
                else if (diff == 0)
                {
                    continue;
                }
                else
                {
                    current = 1;
                    prevDate = currentDate;
                }
            }

            return longest;
        }
    }
}
