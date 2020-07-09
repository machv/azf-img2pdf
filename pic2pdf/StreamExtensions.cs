using System.IO;

namespace img2pdf
{
    public static class StreamExtensions
    {
        public static byte[] ReadToEnd(this Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
