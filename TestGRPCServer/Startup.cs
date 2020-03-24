using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TestGRPCServer.Services;

namespace TestGRPCServer
{
     public class Startup
    {
        private readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();
        private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());

        public Startup()
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {

            
            services.AddAuthorization(options =>
            {
                options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireClaim(ClaimTypes.Name);
                });
            });
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateAudience = false,
                            ValidateIssuer = false,
                            ValidateActor = false,
                            ValidateLifetime = true,
                            IssuerSigningKey = SecurityKey
                        };
                });

            services.AddGrpc();

            

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            try
            {


                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<InformerService>();
                    endpoints.MapGet("/generateJwtToken",
                        context =>
                        {
                            return context.Response.WriteAsync(GenerateJwtToken(context.Request.Query["name"],
                                context.Request.Query["password"]));
                        });
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }

        private string GenerateJwtToken(string name, string password)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Name or password is not specified.");
            }

            if (name.ToLower() == "systemuser" && password == "FSedfwie34rh9q2?n0-4rmyq2867rtfnw43mdp!")
            {
                var claims = new[] { new Claim(ClaimTypes.Name, name) };
                var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken("ExampleServer", "ExampleClients", claims, expires: DateTime.Now.AddSeconds(60), signingCredentials: credentials);
                return JwtTokenHandler.WriteToken(token);
            }
            else
            {
                throw new AuthenticationException("The username or password is incorrect!");
            }

            
        }
    }
}