using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.PersonalInfoVault.Core.Data;
using TicketFlow.Services.PersonalInfoVault.Core.Data.Models;

namespace TicketFlow.Services.PersonalInfoVault.Core.Repositories;

public class PersonalInfoRepository(PersonalInfoVaultDbContext dbContext) : IPersonalInfoRepository
{
    public Task<PersonalInfo?> GetByTokenAsync(string personToken, CancellationToken cancellationToken = default) =>
        dbContext.PersonalInfos.FirstOrDefaultAsync(x => x.PersonToken == personToken, cancellationToken);

    public Task<PersonalInfo?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.PersonalInfos.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<List<PersonalInfo>> GetByTokensAsync(List<string> tokens, CancellationToken cancellationToken = default) =>
        dbContext.PersonalInfos.Where(x => tokens.Contains(x.PersonToken)).ToListAsync(cancellationToken);

    public Task<List<PersonalInfo>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        var pattern = $"%{query}%";
        return dbContext.PersonalInfos
            .Where(x => !x.IsAnonymized &&
                       (EF.Functions.ILike(x.Name, pattern) ||
                        EF.Functions.ILike(x.Email, pattern)))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PersonalInfo personalInfo, CancellationToken cancellationToken = default)
    {
        await dbContext.PersonalInfos.AddAsync(personalInfo, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PersonalInfo personalInfo, CancellationToken cancellationToken = default)
    {
        dbContext.PersonalInfos.Update(personalInfo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
