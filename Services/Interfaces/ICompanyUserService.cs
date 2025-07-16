using ALGASystem.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ALGASystem.Services.Interfaces
{
    public interface ICompanyUserService
    {
        Task<List<CompanyUser>> GetCompanyUsersAsync(Guid companyId);
        Task<CompanyUser> GetCompanyUserByIdAsync(Guid id);
        Task<CompanyUser> GetCompanyUserByUserAndCompanyAsync(string userId, Guid companyId);
        Task<bool> UserExistsInCompanyAsync(string userId, Guid companyId);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<CompanyUser> AddCompanyUserAsync(CompanyUser companyUser);
        Task<bool> UpdateCompanyUserAsync(CompanyUser companyUser);
        Task<bool> RemoveCompanyUserAsync(Guid id);
        Task<bool> RemoveCompanyUserAsync(string userId, Guid companyId);
        Task<bool> IsUserAssignedToCompanyAsync(Guid companyId, string userId);
        IEnumerable<string> GetAvailableRoles();
        /// <summary>
        /// Obtiene las empresas asignadas a un usuario específico con sus roles
        /// </summary>
        Task<List<CompanyUser>> GetCompaniesByUserIdAsync(string userId);
        Task<List<string>> GetAvailableCompanyRolesAsync();
    }
}
