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

namespace img2pdf
{
    public static class ConvertUrls
    {
        [FunctionName("convertUrls")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("[convertUrls] Starting...");

            StreamReader reader = new StreamReader(req.Body);
            string json = reader.ReadToEnd();
            string[] urls = JsonConvert.DeserializeObject<string[]>(json);

            using (MemoryStream ms = new MemoryStream())
            using (PdfWriter writer = new PdfWriter(ms))
            {
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

                byte[] payload = ms.ToArray();
                return new FileContentResult(payload, "application/pdf")
                {
                    FileDownloadName = "converted.pdf"
                };
            }
        }
    }
}
