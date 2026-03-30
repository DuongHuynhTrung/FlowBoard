using System;
using System.Threading.Tasks;
using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role> GetByNameAsync(string name);
        Task AddAsync(Role role);
        Task<bool> ExistsByNameAsync(string name);
    }
}
