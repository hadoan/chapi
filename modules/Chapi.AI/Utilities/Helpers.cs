using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Chapi.AI.Services;
using Chapi.EndpointCatalog.Domain;

namespace Chapi.AI.Utilities
{
    public static class Helpers
    {

        public static string BuildAllowedOps(IEnumerable<ApiEndpoint> eps) =>
            string.Join("\n", eps.OrderBy(e => e.Path).ThenBy(e => e.Method)
                                 .Select(e => $"{e.Method} {e.Path}"));

        public static string Hint(ApiEndpoint e)
        {
            string auth = EndpointIntrospection.ExtractAuth(e);            // "bearer"|"none"
            string req = EndpointIntrospection.ExtractReq(e);             // "application/json"|"-"
            string succ = EndpointIntrospection.ExtractSuccess(e);         // "200:application/json User"|"-"
            return $"{e.Method} {e.Path} | auth:{auth} | req:{req} | {succ}";
        }
        public static string BuildHints(IEnumerable<ApiEndpoint> eps) =>
            string.Join("\n", eps.Select(Hint));

        public static string BuildEndpointsContextFromPicks(IEnumerable<EndpointSelectorService.Pick>? picks) =>
            picks == null ? string.Empty : string.Join("\n", picks.Select(p =>
              $"- {p.Method,-4} {p.Path,-28} | auth:{p.Auth,-6} | req:{p.Req,-16} | {p.Success}"
            ));

    }
}