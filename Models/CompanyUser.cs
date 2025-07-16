using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ALGASystem.Models
{
    public class CompanyUser
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid CompanyId { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Role { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [ForeignKey("CompanyId")]
        public Company Company { get; set; }
        
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
