using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bookstore.Utilities
{
    // return 404 on resource when the attribute argument does not match ASPNETCORE_ENVIRONMENT env variable value
    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class EnvironmentFilterAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _environment;
        public EnvironmentFilterAttribute(string environment)
        {
            _environment = environment;
        }
        
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_environment != Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                context.Result = new NotFoundResult();
        }
    }
}