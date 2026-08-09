using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services
{
    public class EmployeeRoleLookupService : IEmployeeRoleLookupService
    {
        private readonly HRFlowDbContext _identityContext;

        public EmployeeRoleLookupService(HRFlowDbContext identityContext)
        {
            _identityContext = identityContext;
        }

        public async Task<string> GetRoleNameByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identityUserId))
            {
                return string.Empty;
            }

            var roleName = await (from ur in _identityContext.Set<IdentityUserRole<string>>()
                                  join r in _identityContext.Set<IdentityRole>() on ur.RoleId equals r.Id
                                  where ur.UserId == identityUserId
                                  select r.Name).FirstOrDefaultAsync(cancellationToken);

            return roleName ?? string.Empty;
        }
    }
}