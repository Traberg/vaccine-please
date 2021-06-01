using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Serilog;

namespace vaccine_please
{
    class Program
    {
        private static string _fullName = "FILL";
        private static string _birthday = "FILL"; // dd/mm/yyyy
        private static string _emailAddress = "FILL";
        private static string _phoneNumber = "FILL";
        
        // Choose one of the following: "Vaccination Aarhus NORD", "Vaccination Aarhus SYD", "Vaccination Aarhus Ø", "Vaccination Skanderborg", "Vaccination Samsø"
        private static string _vaccineLocation = "FILL";
        
        static async Task Main()
        {
            var location = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var logger = new LoggerConfiguration()
                .WriteTo.File($"{location}/vaccine-please.log")
                .MinimumLevel.Information()
                .CreateLogger();
            
            try 
            {
                var getUrl = "https://www.auh.dk/om-auh/tilbud-om-covid-vaccination-ved-brug-af-restvacciner/";
                var postUri = "https://www.auh.dk/EPiServer.Forms/DataSubmit/Submit";

                CookieContainer cookieContainer = new CookieContainer();
                HttpClientHandler handler = new HttpClientHandler {CookieContainer = cookieContainer };

                var httpClient = new HttpClient(handler);

                var pageResponse = await httpClient.GetAsync(getUrl);

                var pageHtml = await pageResponse.Content.ReadAsStringAsync();
                
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageHtml);
                var allDescendants = htmlDoc.DocumentNode.Descendants().ToList();

                var requestVerificationToken = allDescendants.First(t => t.Attributes.Any(l => l.Value == "__RequestVerificationToken")).GetAttributes("value").Single().Value;

                var formParameters = GetFormData();

                using HttpContent formDataContent = new FormUrlEncodedContent(formParameters);

                formDataContent.Headers.Add("antiForgeryToken", requestVerificationToken);

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, postUri) {Content = formDataContent };

                using HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

                var responseData = response.Content.ReadAsStringAsync();

                logger.Information($"Finished run successfully - {responseData}");
            }
            catch(Exception e)
            {
                logger.Error($"Error during run", e);
            }
        }
        
        private static List<KeyValuePair<string, string>> GetFormData()
        {
            var formParameters = new List<KeyValuePair<string, string>>
            {
                new ("__FormGuid", "820fb682-58e5-45da-b863-f7fc076f4b48"),
                new ("__FormHostedPage", "913890"),
                new ("__FormLanguage", "da"),
                new ("__FormCurrentStepIndex", "0"),
                new ("__FormWithJavaScriptSupport", "true"),
                new ("__field_913918", _fullName),
                new ("__field_951871", _birthday),
                new ("__field_913920", _emailAddress),
                new ("__field_945261", _phoneNumber),
                new ("__field_913923", _vaccineLocation),
                new ("submit", "58efbdd7-1555-4202-aec4-30c5745c4797")
            };
            
            return formParameters;
        }
    }
}
