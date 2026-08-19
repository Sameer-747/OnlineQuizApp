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
        public DbSet<Section> Sections { get; set; } = default!;
        public DbSet<StudentBadge> StudentBadges { get; set; } = default!;
        public DbSet<TestEvent> TestEvents { get; set; } = default!;
        public DbSet<TestEventAssignment> TestEventAssignments { get; set; } = default!;

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
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QuizAttempt>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserAnswer>()
                .HasOne(ua => ua.QuizAttempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(ua => ua.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAnswer>()
                .HasOne(ua => ua.Question)
                .WithMany()
                .HasForeignKey(ua => ua.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserAnswer>()
                .HasOne(ua => ua.SelectedOption)
                .WithMany()
                .HasForeignKey(ua => ua.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Section relationships - explicit to avoid EF ambiguity
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Section)
                .WithMany()
                .HasForeignKey(u => u.SectionId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Section>()
                .HasOne(s => s.AdminUser)
                .WithMany()
                .HasForeignKey(s => s.AdminUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Quiz Section and CreatedBy
            builder.Entity<Quiz>()
                .HasOne(q => q.Section)
                .WithMany()
                .HasForeignKey(q => q.SectionId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Quiz>()
                .HasOne(q => q.CreatedByUser)
                .WithMany()
                .HasForeignKey(q => q.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Category Section
            builder.Entity<Category>()
                .HasOne(c => c.Section)
                .WithMany()
                .HasForeignKey(c => c.SectionId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // RollNumber unique index
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.RollNumber)
                .IsUnique()
                .HasFilter("\"RollNumber\" IS NOT NULL");

            builder.Entity<StudentBadge>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentBadge>()
                .HasOne(b => b.QuizAttempt)
                .WithMany()
                .HasForeignKey(b => b.QuizAttemptId)
                .OnDelete(DeleteBehavior.SetNull);

            // TestEvent relationships
            builder.Entity<TestEvent>()
                .HasOne(te => te.Section)
                .WithMany()
                .HasForeignKey(te => te.SectionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.Entity<TestEvent>()
                .HasOne(te => te.CreatedByUser)
                .WithMany()
                .HasForeignKey(te => te.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Deleting a TestEvent removes the per-language quizzes generated for it.
            builder.Entity<Quiz>()
                .HasOne(q => q.TestEvent)
                .WithMany(te => te.Quizzes)
                .HasForeignKey(q => q.TestEventId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // TestEventAssignment relationships - all cascade from TestEvent so there's a single,
            // unambiguous delete path (avoids Restrict-vs-Cascade ordering conflicts on delete).
            builder.Entity<TestEventAssignment>()
                .HasOne(a => a.TestEvent)
                .WithMany(te => te.Assignments)
                .HasForeignKey(a => a.TestEventId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.Entity<TestEventAssignment>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.Entity<TestEventAssignment>()
                .HasOne(a => a.Quiz)
                .WithMany()
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.Entity<TestEventAssignment>()
                .HasIndex(a => new { a.TestEventId, a.UserId })
                .IsUnique();
        }
    }
}
