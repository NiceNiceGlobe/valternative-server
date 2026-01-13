using Microsoft.AspNetCore.Identity;
using ValternativeServer.Models.Auth;
using ValternativeServer.ValternativeDb;

namespace ValternativeServer.Services
{
    public class IdentityServices : IIdentityServices
    {
        private readonly IHttpContextAccessor _httpContext;
        private readonly ValternativeDbContext _db;
        private readonly UserManager<ValternativeUser> _userManager;

        public IdentityServices(
            IHttpContextAccessor context,
            ValternativeDbContext db,
            UserManager<ValternativeUser> userManager)
        {
            _httpContext = context;
            _db = db;
            _userManager = userManager;
        }

        public ValternativeUser CurrentUser =>
            _db.Set<ValternativeUser>()
               .FirstOrDefault(x => x.Email == _httpContext.HttpContext.User.Identity.Name);

        public async Task<string> GetCurrentUserRoleAsync()
        {
            var user = CurrentUser;
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        public Guid? GetUserId()
        {
            var userIdStr = _httpContext.HttpContext?
                .User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
                .Value;

            return Guid.TryParse(userIdStr, out var guid) ? guid : (Guid?)null;
        }
    }
}