/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Concepts;
using IdentityServer4.Extensions;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Web
{

    /// <summary>
    /// Represents a middleware that is aware of tenancy in the URLs coming in
    /// </summary>
    /// <remarks>
    /// IdentityServer is built to represent a single tenant - or rather, it seems to be very
    /// agnostic towards being tenant aware in the sense that we need it to be.
    /// 
    /// Luckily, under the hood in IdentityServer the PathBase of a request is used actively.
    /// For instance when generating the discovery document, it uses PathBase as prefix.
    /// So for the /.well-known/openid-configuration route it exposes, it uses PathBase to prefix.
    /// 
    /// This middleware supports paths that start with a Guid in the Path and then change the request
    /// path to not have the Guid but then moves the Guid into the PathBase. The Guid represents
    /// the unique identifier of the tenant.
    /// 
    /// In addition to the tenant information, we also want to get the application. This is the next
    /// segment in the path.
    /// 
    /// A valid path would therefor be something like: 
    /// https://dolittle.online/106e84fa-4eed-466b-bb83-70dff8607b4c/someapplication/.well-known/openid-configuration
    /// 
    /// Tenant in this context is the tenant owning the application / client - this is a globally unique identifier
    /// Application is the Dolittle application registration reference for the tenant - this is unique per tenant
    /// </remarks>
    public class AuthContextMiddleware
    {
        /// <summary>
        /// Gets the key used to get the <see cref="AuthContext"/> from the current <see cref="HttpContext"/>
        /// </summary>
        public const string AuthContextItemKey = "AuthContext";
        readonly RequestDelegate _next;
        readonly ITenantConfiguration _tenantConfiguration;
        readonly IHostingEnvironment _hostingEnvironment;
        readonly IAuthenticationHandlerProvider _handlerProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <param name="tenantConfiguration"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="handlerProvider"></param>
        public AuthContextMiddleware(
            RequestDelegate next,
            ITenantConfiguration tenantConfiguration,
            IHostingEnvironment hostingEnvironment,
            IAuthenticationHandlerProvider handlerProvider)
        {            
            _next = next;
            _tenantConfiguration = tenantConfiguration;
            _hostingEnvironment = hostingEnvironment;
            _handlerProvider = handlerProvider;
        }

        /// <summary>
        /// The method that gets invoked by the ASP.NET pipeline
        /// </summary>
        /// <param name="context"><see cref="HttpContext"/> for the request</param>
        public async Task Invoke(HttpContext context)
        {

            

            var segments = context.Request.Path.Value.Split('/');
            if (segments.Length > 2)
            {
                TenantId tenantId = null;
                Guid tenantGuid = Guid.Empty;
                var tenantSegment = segments[1];

                var isGuid = Guid.TryParse(tenantSegment, out tenantGuid);
                if (isGuid)
                {
                    var handler = await _handlerProvider.GetHandlerAsync(context, tenantSegment);
                    if( handler != null ) {
                        await _next(context);
                        return;
                    }

                    tenantId = tenantGuid;
                    if (!_tenantConfiguration.HasTenant(tenantId))
                    {
                        throw new ArgumentException("Tenant does not exist");
                        // Todo: redirect to error page with proper error 
                    }

                    var tenant = _tenantConfiguration.GetFor(tenantId);
                    var applicationName = segments[2];
                    if (!tenant.HasApplication(applicationName))
                    {
                        throw new ArgumentException($"Application '{applicationName}' does not exist in tenant '{tenantId.Value}'");
                        // Todo: redirect to error page with proper error 
                    }

                    context.Request.PathBase = new PathString($"/{tenantSegment}/{applicationName}");
                    if( !_hostingEnvironment.IsDevelopment() )
                    {
                        context.Request.Host = new HostString("dolittle.online");
                        context.Request.Scheme = "https";
                    }
                    
                    var remainingSegments = new List<string>(segments);
                    remainingSegments.RemoveRange(0, 3);
                    context.Request.Path = $"/{string.Join('/',remainingSegments)}";

                    var authContext = new AuthContext(tenant, tenant.Applications[applicationName]);
                    //context.Items[AuthContextItemKey] = authContext;
                    AuthContextBindings.AuthContext = authContext;
                }
            }
            else
            {

                // Todo: redirect to error page with proper error 

            }
            await _next(context);
        }
    }
}