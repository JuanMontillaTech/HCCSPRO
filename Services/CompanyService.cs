using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ALGASystem.Data;
using ALGASystem.Models;
using ALGASystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ALGASystem.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(ApplicationDbContext context, ILogger<CompanyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Company>> GetAllCompaniesAsync(bool includeInactive = false)
        {
            try
            {
                var query = _context.Companies.AsQueryable();
                
                if (!includeInactive)
                {
                    query = query.Where(c => c.IsActive);
                }
                
                return await query.OrderBy(c => c.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de empresas");
                return new List<Company>();
            }
        }

        public async Task<Company> GetCompanyByIdAsync(Guid id)
        {
            try
            {
                return await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la empresa con ID {CompanyId}", id);
                return null;
            }
        }

        public async Task<Company> GetCompanyByTaxNumberAsync(string taxNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(taxNumber))
                {
                    return null;
                }
                
                return await _context.Companies.FirstOrDefaultAsync(c => c.TaxNumber == taxNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la empresa con RNC/NIT {TaxNumber}", taxNumber);
                return null;
            }
        }

        public async Task<bool> CreateCompanyAsync(Company company)
        {
            try
            {
                if (company == null)
                {
                    _logger.LogWarning("Intento de crear una empresa nula");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(company.Name))
                {
                    _logger.LogWarning("Intento de crear una empresa con nombre vacío");
                    return false;
                }
                
                if (!string.IsNullOrWhiteSpace(company.TaxNumber))
                {
                    var existingCompany = await GetCompanyByTaxNumberAsync(company.TaxNumber);
                    if (existingCompany != null)
                    {
                        _logger.LogWarning("Intento de crear una empresa con RNC/NIT duplicado: {TaxNumber}", company.TaxNumber);
                        return false;
                    }
                }

                company.Id = Guid.NewGuid();
                company.CreatedAt = DateTime.Now;
                company.IsActive = true;

                await _context.Companies.AddAsync(company);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Empresa creada correctamente: {CompanyName}, ID: {CompanyId}", company.Name, company.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la empresa {CompanyName}", company?.Name);
                return false;
            }
        }

        public async Task<bool> UpdateCompanyAsync(Company company)
        {
            try
            {
                if (company == null || company.Id == Guid.Empty)
                {
                    _logger.LogWarning("Intento de actualizar una empresa nula o sin ID válido");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(company.Name))
                {
                    _logger.LogWarning("Intento de actualizar una empresa con nombre vacío");
                    return false;
                }

                var existingCompany = await GetCompanyByIdAsync(company.Id);
                if (existingCompany == null)
                {
                    _logger.LogWarning("Intento de actualizar una empresa que no existe. ID: {CompanyId}", company.Id);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(company.TaxNumber) && 
                    !await IsTaxNumberUniqueAsync(company.TaxNumber, company.Id))
                {
                    _logger.LogWarning("Intento de actualizar una empresa con RNC/NIT duplicado: {TaxNumber}", company.TaxNumber);
                    return false;
                }

                // Mantener la fecha de creación original
                company.CreatedAt = existingCompany.CreatedAt;
                
                _context.Entry(existingCompany).State = EntityState.Detached;
                _context.Entry(company).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Empresa actualizada correctamente: {CompanyName}, ID: {CompanyId}", company.Name, company.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la empresa {CompanyName}, ID: {CompanyId}", company?.Name, company?.Id);
                return false;
            }
        }

        public async Task<bool> ToggleCompanyStatusAsync(Guid id, bool isActive)
        {
            try
            {
                var company = await GetCompanyByIdAsync(id);
                if (company == null)
                {
                    _logger.LogWarning("Intento de cambiar el estado de una empresa que no existe. ID: {CompanyId}", id);
                    return false;
                }

                company.IsActive = isActive;
                _context.Companies.Update(company);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Estado de empresa cambiado correctamente a {IsActive}. ID: {CompanyId}, Nombre: {CompanyName}", 
                    isActive, id, company.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar el estado de la empresa con ID: {CompanyId}", id);
                return false;
            }
        }

        public async Task<bool> IsTaxNumberUniqueAsync(string taxNumber, Guid? excludeCompanyId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(taxNumber))
                {
                    return true;
                }

                var query = _context.Companies.Where(c => c.TaxNumber == taxNumber);
                
                if (excludeCompanyId.HasValue)
                {
                    query = query.Where(c => c.Id != excludeCompanyId.Value);
                }

                return !(await query.AnyAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar si el RNC/NIT {TaxNumber} es único", taxNumber);
                return false;
            }
        }
    }
}
