using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Nbe
{
    public class NbePaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }

        public string Merchant { get; set; }

        public string AccessCode { get; set; }

        public string SecureSecret { get; set; }

    }
}
