using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PuppeteerSharp;

[assembly: FunctionsStartup(typeof(Company.Function.Startup))]

namespace Company.Function
{
    public class Startup : FunctionsStartup
    {

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var bfOptions = new BrowserFetcherOptions();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                bfOptions.Path = Path.GetTempPath();
            }
            var bf = new BrowserFetcher(bfOptions);
            bf.DownloadAsync(BrowserFetcher.DefaultRevision).Wait();
            var info = new AppInfo
            {
                BrowserExecutablePath = bf.GetExecutablePath(BrowserFetcher.DefaultRevision)
            };

            var port = GetAvailablePort();
            info.RazorPagesServerPort = port;
            builder.Services.AddSingleton(info);

            var webHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var scriptRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
                    System.Console.WriteLine($"Starting web server on port {port}");
                    if (!string.IsNullOrEmpty(scriptRoot))
                    {
                        webBuilder.UseContentRoot(scriptRoot);
                    }

                    webBuilder.UseUrls($"http://0.0.0.0:{port}")
                        .UseStartup<RazorPagesApp.Startup>();
                })
                .Build();

            webHost.Start();
        }

        private int GetAvailablePort()
        {
            // https://stackoverflow.com/a/150974/9035640
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int availablePort = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return availablePort;
        }
    }

    public class AppInfo
    {
        public string BrowserExecutablePath { get; set; }
        public int RazorPagesServerPort { get; set; }
    }
}