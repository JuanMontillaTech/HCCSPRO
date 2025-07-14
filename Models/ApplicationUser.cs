using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ALGASystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual ICollection<UserPermission> UserPermissions { get; set; }
    }
}
