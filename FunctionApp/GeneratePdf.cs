using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PuppeteerSharp;
using System.Linq;
using PuppeteerSharp.Input;

namespace Company.Function
{
    public class GeneratePdf
    {
        private readonly AppInfo appInfo;

        public GeneratePdf(AppInfo browserInfo)
        {
            this.appInfo = browserInfo;
        }

        [FunctionName("GeneratePdf")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Browser path: {appInfo.BrowserExecutablePath}");

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = appInfo.BrowserExecutablePath
            });
            var page = await browser.NewPageAsync();
            await page.GoToAsync($"http://localhost:{appInfo.RazorPagesServerPort}/");

            string data = GetData();
            await page.TypeAsync("#items-box", data);
            await Task.WhenAll(
                page.WaitForNavigationAsync(), 
                page.ClickAsync("#submit-button"));
            var stream = await page.PdfStreamAsync();
            await browser.CloseAsync();

            return new FileStreamResult(stream, "application/pdf");
        }

        private string GetData()
        {
            var fakeProducts = new Bogus.DataSets.Commerce();
            var rand = new Random();
            var data = Enumerable.Range(0, 10)
                .Select(_ => new {
                    productId = fakeProducts.Ean13(),
                    name = fakeProducts.ProductName(),
                    quantity = rand.Next(1, 10),
                    unitPrice = Convert.ToDecimal(fakeProducts.Price())
                });
            return JsonConvert.SerializeObject(data);
        }
    }
}
