using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ALGASystem.Models;

namespace ALGASystem.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<IEnumerable<Company>> GetAllCompaniesAsync(bool includeInactive = false);
        Task<Company> GetCompanyByIdAsync(Guid id);
        Task<Company> GetCompanyByTaxNumberAsync(string taxNumber);
        Task<bool> CreateCompanyAsync(Company company);
        Task<bool> UpdateCompanyAsync(Company company);
        Task<bool> ToggleCompanyStatusAsync(Guid id, bool isActive);
        Task<bool> IsTaxNumberUniqueAsync(string taxNumber, Guid? excludeCompanyId = null);
    }
}
