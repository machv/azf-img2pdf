using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using iText.Kernel.Pdf;
using Newtonsoft.Json;
using System.Net;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using System.Net.Http;
using System.Text;

namespace img2pdf
{
    public static class ConvertUrlsAsBlob
    {
        private static readonly Lazy<TokenCredential> _msiCredential = new Lazy<TokenCredential>(() =>
        {
            // https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            // Using DefaultAzureCredential allows for local dev by setting environment variables for the current user, provided said user
            // has the necessary credentials to perform the operations the MSI of the Function app needs in order to do its work. Including
            // interactive credentials will allow browser-based login when developing locally.
            return new Azure.Identity.DefaultAzureCredential(includeInteractiveCredentials: true);
        });

        [FunctionName("convertUrlsAsBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("[convertUrlsAsBlob] Starting...");

            StreamReader reader = new StreamReader(req.Body);
            string json = reader.ReadToEnd();
            string[] urls = JsonConvert.DeserializeObject<string[]>(json);

            if(urls.Length == 0)
                new BadRequestObjectResult("Missing list of urls to convert to PDF.");

            string containerUrl = GetConfigurationValue("OutputBlobContainerPath");
            if (containerUrl == null)
                new BadRequestObjectResult("Output blob storage container is not configured.");

            using (MemoryStream ms = new MemoryStream())
            using (PdfWriter writer = new PdfWriter(ms))
            {
                writer.SetCloseStream(false);

                PdfDocument pdfDocument = new PdfDocument(writer);

                foreach (var url in urls)
                {
                    log.LogInformation($" - processing {url}");

                    Uri uri = new Uri(url);
                    string fileExtension = Path.GetExtension(uri.LocalPath).ToLower();

                    if (fileExtension == ".pdf")
                    {
                        log.LogInformation($" - appending existing PDF pages");

                        PdfUtilities.AppendPagesFromDocument(pdfDocument, url);
                    }
                    else
                    {
                        log.LogInformation($" - appending as an image page");

                        PdfUtilities.AppendImagePage(pdfDocument, url);
                    }
                }

                pdfDocument.Close();

                // Save to Blob
                var x = new BlobContainerClient(new Uri(containerUrl), _msiCredential.Value);
                string randomBlobName = $"{Guid.NewGuid():N}.pdf";
                ms.Seek(0, SeekOrigin.Begin);
                var info = x.UploadBlob(randomBlobName, ms);

                Uri containerUri = new Uri(containerUrl);
                Uri blobUri = new Uri(containerUri, randomBlobName);

                return new OkObjectResult(blobUri.ToString());
            }
        }

        private static string? GetConfigurationValue(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
