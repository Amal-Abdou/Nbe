﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Nbe.Services;
using Nop.Web.Framework.Infrastructure.Extensions;

namespace Nop.Plugin.Payments.Nbe.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<NbeHttpClient>().WithProxy();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order => 101;
    }
}