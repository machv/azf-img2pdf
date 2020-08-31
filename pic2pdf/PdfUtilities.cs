using iText.Forms;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace img2pdf
{
    public class PdfUtilities
    {
        public static void AppendPagesFromDocument(PdfDocument pdfDocument, string url)
        {
            using (WebClient wc = new WebClient())
            {
                using (var pdfReader = new PdfReader(wc.OpenRead(url)))
                {
                    PdfDocument src = new PdfDocument(pdfReader);
                    src.CopyPagesTo(1, src.GetNumberOfPages(), pdfDocument, new PdfPageFormCopier());
                    src.Close();
                }
            }
        }

        public static void AppendImagePage(PdfDocument pdfDocument, string imageUrl)
        {
            Image image = new Image(ImageDataFactory
               .Create(imageUrl))
               .SetBorder(Border.NO_BORDER)
               .SetAutoScale(true);
            image.SetFixedPosition(0, 0);

            PageSize pageSize = new PageSize(image.GetImageScaledWidth(), image.GetImageScaledHeight());
            Document document = new Document(pdfDocument, pageSize);
            document.SetMargins(0, 0, 0, 0);
            pdfDocument.AddNewPage();
            document.Add(new AreaBreak(AreaBreakType.LAST_PAGE));
            document.Add(image);
        }
    }
}
