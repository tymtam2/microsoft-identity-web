﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of web APIs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the token acquisition service.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configureTokenAcquisitionOptions">Options for token acquisition service.</param>
        /// <returns>The service collection.</returns>
        /// <example>
        /// This method is typically called from the <c>ConfigureServices(IServiceCollection services)</c> in Startup.cs.
        /// Note that the implementation of the token cache can be chosen separately.
        ///
        /// <code>
        /// // Token acquisition service and its cache implementation as a session cache
        /// services.AddTokenAcquisition()
        /// .AddDistributedMemoryCache()
        /// .AddSession()
        /// .AddSessionBasedTokenCache();
        /// </code>
        /// </example>
        public static IServiceCollection AddTokenAcquisition(
            this IServiceCollection services,
            Action<TokenAcquisitionOptions>? configureTokenAcquisitionOptions = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Configure(configureTokenAcquisitionOptions);

            services.AddOptions<TokenAcquisitionOptions>()
                .Configure<IServiceProvider, IOptions<TokenAcquisitionOptions>>(
                    (options, serviceProvider, tokenAcquisitionOptions) =>
                {

                });

            ServiceDescriptor? tokenAcquisitionService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition));
            ServiceDescriptor? tokenAcquisitionInternalService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionInternal));
            if (tokenAcquisitionService != null && tokenAcquisitionInternalService != null)
            {
                if (isTokenAcquisitionSingleton ^ (tokenAcquisitionService.Lifetime == ServiceLifetime.Singleton))
                {
                    // The service was already added, but not with the right lifetime
                    services.Remove(tokenAcquisitionService);
                    services.Remove(tokenAcquisitionInternalService);
                }
                else
                {
                    // The service is already added with the right lifetime
                    return services;
                }
            }

            // Token acquisition service
            services.AddHttpContextAccessor();
            if (isTokenAcquisitionSingleton)
            {
                services.AddSingleton<ITokenAcquisition, TokenAcquisition>();
                services.AddSingleton<ITokenAcquisitionInternal>(s => (ITokenAcquisitionInternal)s.GetRequiredService<ITokenAcquisition>());
            }
            else
            {
                services.AddScoped<ITokenAcquisition, TokenAcquisition>();
                services.AddScoped<ITokenAcquisitionInternal>(s => (ITokenAcquisitionInternal)s.GetRequiredService<ITokenAcquisition>());
            }

            return services;
        }
    }
}
