using Microsoft.AspNetCore.Identity;

namespace OnlineQuizApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}
