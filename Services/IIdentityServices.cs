using ValternativeServer.Models.Auth;

namespace ValternativeServer.Services
{
    public interface IIdentityServices
    {
        ValternativeUser CurrentUser { get; }
        Task<string> GetCurrentUserRoleAsync();
        Guid? GetUserId();
    }
}