using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Nbe.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Nbe.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Nbe.Fields.Merchant")]
        public string Merchant { get; set; }
        public bool Merchant_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Nbe.Fields.AccessCode")]
        public string AccessCode { get; set; }
        public bool AccessCode_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Nbe.Fields.SecureSecret")]
        public string SecureSecret { get; set; }
        public bool SecureSecret_OverrideForStore { get; set; }

    }
}