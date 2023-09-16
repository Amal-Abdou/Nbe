using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Nbe.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //PDT
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Nbe.PDTHandler", "Plugins/PaymentNbe/PDTHandler",
                 new { controller = "PaymentNbe", action = "PDTHandler" });


            //Cancel
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Nbe.CancelOrder", "Plugins/PaymentNbe/CancelOrder",
                 new { controller = "PaymentNbe", action = "CancelOrder" });
        }

        public int Priority => -1;
    }
}