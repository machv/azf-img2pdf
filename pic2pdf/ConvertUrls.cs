using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using Newtonsoft.Json;

namespace img2pdf
{
    public static class ConvertUrls
    {
        [FunctionName("convertUrls")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("[convertUrls] trigger function");

            StreamReader reader = new StreamReader(req.Body);
            string json = reader.ReadToEnd();
            string[] urls = JsonConvert.DeserializeObject<string[]>(json);

            ///

            using (MemoryStream ms = new MemoryStream())
            using (PdfWriter writer = new PdfWriter(ms))
            {
                PdfDocument pdfDocument = new PdfDocument(writer);
                Document document = null;

                foreach (var url in urls)
                {
                    log.LogInformation($" - processing {url}");

                    // Add image
                    Image image = new Image(ImageDataFactory
                       .Create(url))
                       .SetBorder(Border.NO_BORDER)
                       .SetAutoScale(true);
                    image.SetFixedPosition(0, 0);

                    PageSize pageSize = new PageSize(image.GetImageScaledWidth(), image.GetImageScaledHeight());
                    if (document == null)
                    {
                        // first page
                        document = new Document(pdfDocument, pageSize);
                        document.SetMargins(0, 0, 0, 0);
                    }
                    else
                    {
                        // additional pages are handled differently
                        document.Add(new AreaBreak(new PageSize(pageSize)));
                    }

                    document.Add(image);
                }

                document.Close();

                byte[] payload = ms.ToArray();
                return new FileContentResult(payload, "application/pdf")
                {
                    FileDownloadName = "converted.pdf"
                };
            }
        }
    }
}
