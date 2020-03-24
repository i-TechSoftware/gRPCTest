using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestGRPCServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test gRPC server!");
            Console.WriteLine($"Version {GetAssemblyVersion()}");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // Отключение логирования
                /*.ConfigureServices(services =>
                {
                    services.Configure<ConsoleLifetimeOptions>(options =>  // configure the options
                        options.SuppressStatusMessages = true); 
                })*/
                // Отключение логирования
                //.ConfigureLogging(logging => { logging.ClearProviders(); })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(5000, o =>
                        {
                            var cert = LoadCertificate();
                            o.Protocols = HttpProtocols.Http2;
                            o.UseHttps(cert);
                        }); 
                        options.ListenAnyIP(5001, o =>
                        {
                            var cert = LoadCertificate();
                            o.Protocols = HttpProtocols.Http1;
                            o.UseHttps(cert);
                        });
                    });
                    //webBuilder.UseUrls($"https://*:50051",$"http://*:50050");
                    webBuilder.UseStartup<Startup>();
                    
                });


        private static X509Certificate2 LoadCertificate()
        {
            var x509 = new X509Certificate2(File.ReadAllBytes("alexpc_loc_cert.pfx"),"Cegthctrhtn1");
            return x509;
            /*using (var store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certificate = store.Certificates.Find(X509FindType.FindBySerialNumber, "51B734123D768D8847DC261E08387683", true);

                if (certificate.Count == 0)
                {
                    throw new InvalidOperationException($"Certificate not found for localhost.");
                }

                return certificate[0];
            }
        throw new InvalidOperationException("No valid certificate configuration found for the current endpoint.");*/
        }

        private static string GetAssemblyVersion()
        {
            return (Assembly.GetEntryAssembly() ?? throw new InvalidOperationException())
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}
