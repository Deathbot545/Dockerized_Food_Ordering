using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;   // For HttpContext
using Microsoft.AspNetCore.Routing; // For IRouteConstraint, IRouter, RouteValueDictionary, RouteDirection


namespace Infrastructure.Constraints
{
    public class SubdomainRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var host = httpContext.Request.Host.Host;
            var index = host.IndexOf(".");
            if (index < 0)
                return false;

            var sub = host.Substring(0, index);
            if (sub == "www")
                return false;

            values["subdomain"] = sub;
            return true;
        }
    }

}
