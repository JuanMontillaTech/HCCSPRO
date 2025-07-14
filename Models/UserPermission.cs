namespace ALGASystem.Models
{
    public class UserPermission
    {
        public string UserId { get; set; }
        public int PermissionId { get; set; }
        
        public virtual ApplicationUser User { get; set; }
        public virtual Permission Permission { get; set; }
    }
}
