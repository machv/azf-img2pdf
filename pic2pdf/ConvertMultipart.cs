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
    public static class ConvertMultipart
    {
        public static byte[] ReadStreamToEnd(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        [FunctionName("convertMultipart")]
        public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("[convertMultipart] HTTP trigger");

            using (MemoryStream ms = new MemoryStream())
            using (PdfWriter writer = new PdfWriter(ms))
            {
                PdfDocument pdfDocument = new PdfDocument(writer);
                Document document = null;

                foreach (var file in req.Form.Files)
                {
                    log.LogInformation($" - processing {file.FileName}");

                    byte[] imageBody;
                    using (MemoryStream fileMemoryStream = new MemoryStream())
                    {
                        file.CopyTo(fileMemoryStream);
                        imageBody = fileMemoryStream.ToArray();
                    }

                    // Add image
                    Image image = new Image(ImageDataFactory
                       .Create(imageBody))
                       .SetBorder(Border.NO_BORDER)
                       .SetAutoScale(true);
                    image.SetFixedPosition(0, 0);

                    PageSize pageSize = new PageSize(image.GetImageScaledWidth(), image.GetImageScaledHeight());
                    if (document == null)
                    { 
                        // first page
                        document = new Document(pdfDocument, pageSize);
                        document.SetMargins(0, 0, 0, 0);
                    } else
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
