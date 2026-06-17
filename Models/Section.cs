using System.ComponentModel.DataAnnotations;

namespace OnlineQuizApp.Models
{
    public class Section
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // The admin who manages this section. Null until super admin assigns one.
        public string? AdminUserId { get; set; }
        public ApplicationUser? AdminUser { get; set; }
    }
}
