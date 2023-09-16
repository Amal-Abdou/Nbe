using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Payments.Nbe.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Nbe.Components
{
    [ViewComponent(Name = "PaymentNbe")]
    public class PaymentNbeViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel()
            {
                CreditCardTypes = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Master Card", Value = "mastercard" },
                    new SelectListItem { Text = "Credit Card", Value = "creditcard" },
                }
            };
            return View("~/Plugins/Payments.Nbe/Views/PaymentInfo.cshtml",model);
        }
    }
}
