﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core.Encryption;
using Swashbuckle.AspNetCore.Swagger;
using System.Reflection;
using System.Text;
using NosCore.WebApi.Controllers;

namespace NosCore.WorldServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Info { Title = "NosCore World API", Version = "v1" }));

            var keyByteArray = Encoding.ASCII.GetBytes(EncryptionHelper.Sha512("NosCorePassword"));//TODO replace by configured one
            var signinKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyByteArray);

            services.AddAuthentication(config => config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(cfg =>
            {
                cfg.RequireHttpsMetadata = false;
                cfg.SaveToken = true;
                cfg.TokenValidationParameters = new TokenValidationParameters()
                {
                    IssuerSigningKey = signinKey,
                    ValidAudience = "Audience",
                    ValidIssuer = "Issuer",
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                };
            });

            services.AddMvc(o =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                o.Filters.Add(new AuthorizeFilter(policy));
            }).AddApplicationPart(typeof(TokenController).GetTypeInfo().Assembly).AddControllersAsServices();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NosCore World API"));
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
