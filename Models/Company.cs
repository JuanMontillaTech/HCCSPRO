using System;
using System.ComponentModel.DataAnnotations;

namespace ALGASystem.Models
{
    public class Company
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        [RegularExpression(@".*\S+.*", ErrorMessage = "El nombre no puede contener solo espacios")]
        public string Name { get; set; }
        
        [StringLength(20, ErrorMessage = "El RNC/NIT no puede exceder los 20 caracteres")]
        public string TaxNumber { get; set; }
        
        [StringLength(255, ErrorMessage = "La dirección no puede exceder los 255 caracteres")]
        public string Address { get; set; }
        
        [StringLength(30, ErrorMessage = "El teléfono no puede exceder los 30 caracteres")]
        public string Phone { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }
}
