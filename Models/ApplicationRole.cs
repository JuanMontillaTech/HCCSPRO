using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ALGASystem.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; }
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
    }
}
