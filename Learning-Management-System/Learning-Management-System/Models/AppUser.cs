using Microsoft.AspNetCore.Identity;

namespace Learning_Management_System.Models
{
    public class AppUser : IdentityUser
    {
        // Additional profile fields
        public string? FullName { get; set; }
    }
}