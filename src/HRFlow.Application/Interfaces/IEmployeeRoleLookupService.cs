namespace HRFlow.Application.Interfaces
{
    public interface IEmployeeRoleLookupService
    {
        System.Threading.Tasks.Task<string> GetRoleNameByIdentityUserIdAsync(string identityUserId, System.Threading.CancellationToken cancellationToken);
    }
}