using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Quiz> Quizzes { get; set; } = default!;
        public DbSet<Question> Questions { get; set; } = default!;
        public DbSet<Option> Options { get; set; } = default!;
        public DbSet<QuizAttempt> QuizAttempts { get; set; } = default!;
        public DbSet<UserAnswer> UserAnswers { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Quiz>()
                .HasOne(q => q.Category)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Question>()
                .HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Option>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuizAttempt>()
                .HasOne(a => a.Quiz)
                .WithMany()
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuizAttempt>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAnswer>()
                .HasOne(ua => ua.QuizAttempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(ua => ua.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAnswer>()
                .HasOne(ua => ua.Question)
                .WithMany()
                .HasForeignKey(ua => ua.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAnswer>()
                .HasOne(ua => ua.SelectedOption)
                .WithMany()
                .HasForeignKey(ua => ua.SelectedOptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
