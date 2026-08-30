using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services
{
    /// <summary>
    /// Provides role lookup operations backed by ASP.NET Core Identity data stored in the HRFlowDbContext.
    /// </summary>
    public class EmployeeRoleLookupService : IEmployeeRoleLookupService
    {
        private readonly HRFlowDbContext _identityContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmployeeRoleLookupService"/> class.
        /// </summary>
        /// <param name="identityContext">The database context containing ASP.NET Core Identity tables.</param>
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
                                  orderby r.Name
                                  select r.Name).FirstOrDefaultAsync(cancellationToken);

            return roleName ?? string.Empty;
        }
    }
}