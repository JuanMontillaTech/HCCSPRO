namespace ALGASystem.Models
{
    public class RolePermission
    {
        public string RoleId { get; set; }
        public int PermissionId { get; set; }
        
        public virtual ApplicationRole Role { get; set; }
        public virtual Permission Permission { get; set; }
    }
}
