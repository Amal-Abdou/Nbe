using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Nbe.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Nbe.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class PaymentNbeController : BasePaymentController
    {
        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Ctor

        public PaymentNbeController(IGenericAttributeService genericAttributeService,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IPaymentPluginManager paymentPluginManager,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            ILogger logger,
            INotificationService notificationService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            IWorkContext workContext,
            ShoppingCartSettings shoppingCartSettings)
        {
            _genericAttributeService = genericAttributeService;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _paymentPluginManager = paymentPluginManager;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _logger = logger;
            _notificationService = notificationService;
            _settingService = settingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var nbePaymentSettings = await _settingService.LoadSettingAsync<NbePaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = nbePaymentSettings.UseSandbox,
                Merchant = nbePaymentSettings.Merchant,
                AccessCode = nbePaymentSettings.AccessCode,
                SecureSecret = nbePaymentSettings.SecureSecret,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope <= 0)
                return View("~/Plugins/Payments.Nbe/Views/Configure.cshtml", model);

            model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(nbePaymentSettings, x => x.UseSandbox, storeScope);
            model.Merchant_OverrideForStore = await _settingService.SettingExistsAsync(nbePaymentSettings, x => x.Merchant, storeScope);
            model.AccessCode_OverrideForStore = await _settingService.SettingExistsAsync(nbePaymentSettings, x => x.AccessCode, storeScope);
            model.SecureSecret_OverrideForStore = await _settingService.SettingExistsAsync(nbePaymentSettings, x => x.SecureSecret, storeScope);
            
            return View("~/Plugins/Payments.Nbe/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]        
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var nbePaymentSettings = await _settingService.LoadSettingAsync<NbePaymentSettings>(storeScope);

            nbePaymentSettings.UseSandbox = model.UseSandbox;
            nbePaymentSettings.Merchant = model.Merchant;
            nbePaymentSettings.AccessCode = model.AccessCode;
            nbePaymentSettings.SecureSecret = model.SecureSecret;

            await _settingService.SaveSettingOverridablePerStoreAsync(nbePaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(nbePaymentSettings, x => x.Merchant, model.Merchant_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(nbePaymentSettings, x => x.AccessCode, model.AccessCode_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(nbePaymentSettings, x => x.SecureSecret, model.SecureSecret_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        //action displaying notification (warning) to a store owner about inaccurate PayPal rounding
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> RoundingWarning(bool passProductNamesAndTotals)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //prices and total aren't rounded, so display warning
            if (passProductNamesAndTotals && !_shoppingCartSettings.RoundPricesDuringCalculation)
                return Json(new { Result = await _localizationService.GetResourceAsync("Plugins.Payments.Nbe.RoundingWarning") });

            return Json(new { Result = string.Empty });
        }

        public async Task<IActionResult> PDTHandler(
            string vpc_AVSRequestCode,
            string vpc_AVSResultCode,
            string vpc_AcqAVSRespCode,
            string vpc_AcqCSCRespCode,
            string vpc_AcqResponseCode,
            string vpc_Amount,
            string vpc_AuthorizeId,
            string vpc_BatchNo,
            string vpc_CSCResultCode,
            string vpc_Card,
            string vpc_CardNum,
            string vpc_Command,
            string vpc_Currency,
            string vpc_Locale,
            string vpc_MerchTxnRef,
            string vpc_Merchant,
            string vpc_Message,
            string vpc_OrderInfo,
            string vpc_ReceiptNo,
            string vpc_RiskOverallResult,
            string vpc_SecureHash,
            string vpc_SecureHashType,
            string vpc_TransactionNo,
            string vpc_TxnResponseCode,
            string vpc_Version
            )
        {
            var order =await _orderService.GetOrderByIdAsync(int.Parse(vpc_MerchTxnRef));
            if (order != null && !string.IsNullOrEmpty(vpc_Message) && vpc_Message == "Approved")
            {
                await _orderProcessingService.MarkOrderAsPaidAsync(order);
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
            else if (order != null && !string.IsNullOrEmpty(vpc_Message))
            {
               await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "The order cancelled because payment failed, " + vpc_Message,
                    DisplayToCustomer = true,
                    CreatedOnUtc = DateTime.UtcNow
                });
                order.OrderStatusId = (int)OrderStatus.Cancelled;
               await _orderService.UpdateOrderAsync(order);
            }

            return RedirectToAction("Index", "Home", new { area = string.Empty });
        }

        public async Task<IActionResult> CancelOrder(string PaymentID, string Result, string PostDate, string TranID, string Ref, string TrackID, string Auth, string OrderID, string cust_ref, string trnUdf)
        {
            var order = await _orderService.GetOrderByIdAsync(int.Parse(OrderID));

            if (order != null)
            {
               await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "The order cancelled because payment failed.",
                    DisplayToCustomer = true,
                    CreatedOnUtc = DateTime.UtcNow
                });
                order.OrderStatusId = (int)OrderStatus.Cancelled;
               await _orderService.UpdateOrderAsync(order);
            }

            return RedirectToRoute("Homepage");
        }

        #endregion
    }
}