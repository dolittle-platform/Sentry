﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Dolittle.DependencyInversion.Autofac;
using Dolittle.Runtime.Events.Coordination;
using IdentityServer4;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace Web
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Startup
    {
        BootResult _bootResult;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
            });

            services.AddMvc();

            services.AddIdentityServer(options =>
                {
                    options.UserInteraction.LoginUrl = "/accounts/login";
                    options.UserInteraction.LogoutUrl = "/accounts/logout";
                    options.UserInteraction.ConsentUrl = "/accounts/consent";
                })

                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryApiResources(Config.GetApiResources())
                .AddInMemoryClients(Config.GetClients())
                .AddInMemoryPersistedGrants()
                .AddProfileService<MyProfileService>();;


            services.AddAuthentication()
                .AddOpenIdConnect("oidc", "Azure Active Directory", options =>
                {
                    options.CallbackPath = "/signin-oidc";
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;

                    // be4c4da6-5ede-405f-a947-8aedad564b7f

                    // https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-v2-protocols-oidc

                    // https://login.microsoftonline.com/{tenant}/v2.0/.well-known/openid-configuration
                    //options.Authority = "https://login.microsoftonline.com/381088c1-de08-4d18-9e60-bbe2c94eccb5/v2.0";
                    options.Authority = "https://login.microsoftonline.com/common";

                    // organizations
                    options.ClientId = "Blah";
                    //options.ClientId = "2e2cad73-c11a-4d9f-8af9-beeebcdc5a27";
                    //options.ClientSecret = "jW6L65FIXZmsY6kIM+TKYws3zFJ03MyCAF9sFpIbFMs=";

                    //options.Events.RedirectToIdentityProvider

                    options.Events.OnRedirectToIdentityProvider = async(context)=>
                    {
                        /*
                        options.TokenValidationParameters.AudienceValidator = (IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters) =>
                        {
                            
                            return true; 
                        };*/

                        //context.Options.TokenValidationParameters.
                        //context.Options.ClientId = "2e2cad73-c11a-4d9f-8af9-beeebcdc5a27";
                        context.ProtocolMessage.ClientId = "2e2cad73-c11a-4d9f-8af9-beeebcdc5a27";

                        await Task.CompletedTask;
                    };

                    options.Events.OnTokenResponseReceived = async(context)=>
                    {
                        await Task.CompletedTask;
                    };

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });

            _bootResult = services.AddDolittle();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerBuilder"></param>
        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.AddDolittle(_bootResult.Assemblies, _bootResult.Bindings);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var committedEventStreamCoordinator = app.ApplicationServices.GetService(typeof(ICommittedEventStreamCoordinator))as ICommittedEventStreamCoordinator;
            committedEventStreamCoordinator.Initialize();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseMiddleware<TenantMiddleware>();
            app.UseIdentityServer();

            app.UseMvc();

            // Keep this last as this is the fallback when nothing else works - spit out the index file           
            app.Run(async context =>
            {
                if( Path.HasExtension(context.Request.Path)) await Task.CompletedTask;
                context.Request.Path = new PathString("/");
                var path = $"{env.ContentRootPath}/wwwroot/index.html";
                await context.Response.SendFileAsync(new PhysicalFileInfo(new FileInfo(path)));
            });            
        }
    }
}