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

namespace img2pdf
{
    public static class ConvertBody
    {
        public static byte[] ReadStreamToEnd(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        [FunctionName("convertBody")]
        public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            byte[] imageBody = req.Body.ReadToEnd();

            using (MemoryStream ms = new MemoryStream())
            using (PdfWriter writer = new PdfWriter(ms))
            {
                PdfDocument pdfDocument = new PdfDocument(writer);

                // Add image
                Image image = new Image(ImageDataFactory
                   .Create(imageBody))
                   .SetBorder(Border.NO_BORDER)
                   .SetAutoScale(true);
                image.SetFixedPosition(0, 0);

                PageSize pageSize = new PageSize(image.GetImageScaledWidth(), image.GetImageScaledHeight());
                Document document = new Document(pdfDocument, pageSize);
                document.SetMargins(0, 0, 0, 0);
                document.Add(image);
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
