using Microsoft.AspNetCore.Authorization;

namespace ProductionApi.Auth
{
    public class AdminWriteAttribute : AuthorizeAttribute
    {
        public AdminWriteAttribute()
        {
            Policy = AuthorizationPolicies.AdminOnly;
        }
    }
}
