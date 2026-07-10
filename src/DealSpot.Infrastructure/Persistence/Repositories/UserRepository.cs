using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers.FindAsync([id], cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public void Add(User user) => dbContext.AppUsers.Add(user);

    public void Update(User user) => dbContext.AppUsers.Update(user);
}
