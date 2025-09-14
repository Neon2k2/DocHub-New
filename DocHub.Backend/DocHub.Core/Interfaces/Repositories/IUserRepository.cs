using DocHub.Core.Entities;

namespace DocHub.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> IsUsernameAvailableAsync(string username);
    Task<bool> IsEmailAvailableAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<IEnumerable<User>> GetUsersByRoleAsync(Guid roleId);
}