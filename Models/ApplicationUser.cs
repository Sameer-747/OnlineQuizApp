using Microsoft.AspNetCore.Identity;

namespace OnlineQuizApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        // Only set for students. Unique across the whole app.
        public string? RollNumber { get; set; }

        // For students: the section they belong to (their classroom).
        // For section-admins: also set, to indicate which section they manage.
        public int? SectionId { get; set; }
        public Section? Section { get; set; }
    }
}
