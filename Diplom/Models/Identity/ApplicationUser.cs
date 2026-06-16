using Microsoft.AspNetCore.Identity;

namespace Diplom.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public string UserTag { get; set; }
    }
}
