using ALGASystem.Data;
using ALGASystem.Data.Interfaces;
using ALGASystem.Models;
using ALGASystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ALGASystem.Services
{
    public class CompanyUserService : ICompanyUserService
    {
        private readonly IGenericRepository<CompanyUser> _repository;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyUserService(
            IGenericRepository<CompanyUser> repository,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _repository = repository;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<List<CompanyUser>> GetCompanyUsersAsync(Guid companyId)
        {
            return await _dbContext.CompanyUsers
                .AsNoTracking()
                .Where(cu => cu.CompanyId == companyId)
                .Include(cu => cu.User)
                .ToListAsync();
        }

        public async Task<CompanyUser> GetCompanyUserByIdAsync(Guid id)
        {
            return await _dbContext.CompanyUsers
                .AsNoTracking()
                .Include(cu => cu.User)
                .Include(cu => cu.Company)
                .FirstOrDefaultAsync(cu => cu.Id == id);
        }

        public async Task<CompanyUser> GetCompanyUserByUserAndCompanyAsync(string userId, Guid companyId)
        {
            return await _dbContext.CompanyUsers
                .AsNoTracking()
                .Include(cu => cu.User)
                .Include(cu => cu.Company)
                .FirstOrDefaultAsync(cu => cu.UserId == userId && cu.CompanyId == companyId);
        }

        public async Task<bool> UserExistsInCompanyAsync(string userId, Guid companyId)
        {
            return await _dbContext.CompanyUsers
                .AnyAsync(cu => cu.UserId == userId && cu.CompanyId == companyId);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<CompanyUser> AddCompanyUserAsync(CompanyUser companyUser)
        {
            // Verificamos que el usuario exista
            var user = await _userManager.FindByIdAsync(companyUser.UserId);
            if (user == null)
            {
                throw new InvalidOperationException("El usuario no existe");
            }

            // Verificamos que no esté ya asignado a la empresa
            var exists = await UserExistsInCompanyAsync(companyUser.UserId, companyUser.CompanyId);
            if (exists)
            {
                throw new InvalidOperationException("El usuario ya está asignado a esta empresa");
            }

            // Asignar un ID si no tiene uno
            if (companyUser.Id == Guid.Empty)
            {
                companyUser.Id = Guid.NewGuid();
            }

            // Configuramos la fecha de creación
            companyUser.CreatedAt = DateTime.Now;

            // Guardar el nuevo registro
            await _repository.AddAsync(companyUser);
            await _repository.SaveChangesAsync();

            return companyUser;
        }

        public async Task<bool> UpdateCompanyUserAsync(CompanyUser companyUser)
        {
            var existingCompanyUser = await _dbContext.CompanyUsers.FindAsync(companyUser.Id);
            if (existingCompanyUser == null)
            {
                return false;
            }

            // Solo actualizamos el rol, no los IDs
            existingCompanyUser.Role = companyUser.Role;

            _dbContext.CompanyUsers.Update(existingCompanyUser);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> RemoveCompanyUserAsync(Guid id)
        {
            var companyUser = await _dbContext.CompanyUsers.FindAsync(id);
            if (companyUser == null)
            {
                return false;
            }

            _dbContext.CompanyUsers.Remove(companyUser);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> RemoveCompanyUserAsync(string userId, Guid companyId)
        {
            var companyUser = await _dbContext.CompanyUsers
                .FirstOrDefaultAsync(cu => cu.UserId == userId && cu.CompanyId == companyId);
                
            if (companyUser == null)
            {
                return false;
            }

            _dbContext.CompanyUsers.Remove(companyUser);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }

        public Task<List<string>> GetAvailableCompanyRolesAsync()
        {
            // Retorna los roles disponibles para las empresas (no son los mismos que los roles de Identity)
            var roles = new List<string> { "admin", "user", "viewer", "manager", "accountant" };
            return Task.FromResult(roles);
        }

        public IEnumerable<string> GetAvailableRoles()
        {
            // Retorna los roles disponibles para las empresas (no son los mismos que los roles de Identity)
            return new List<string> { "admin", "user", "viewer", "manager", "accountant" };
        }

        public async Task<bool> IsUserAssignedToCompanyAsync(Guid companyId, string userId)
        {
            return await _dbContext.CompanyUsers
                .AnyAsync(cu => cu.UserId == userId && cu.CompanyId == companyId);
        }

        public async Task<List<CompanyUser>> GetCompaniesByUserIdAsync(string userId)
        {
            return await _dbContext.CompanyUsers
                .AsNoTracking()
                .Where(cu => cu.UserId == userId)
                .Include(cu => cu.Company)
                .ToListAsync();
        }
    }
}
