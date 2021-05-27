using System;
using System.IO;
using System.Net.Http;

namespace Bookstore.Utilities
{
    public static class Extensions
    {
        public static byte[]? ReadAsByteArrayLimited(this HttpContent responseContent, uint downloadLimit)
        {
            using var memoryStream = new MemoryStream();
            using var download = new BufferedStream(responseContent.ReadAsStream());
            
            var buffer = new Span<Byte>(new byte[4096]);
            for (int read; (read = download.Read(buffer)) > 0;)
            {
                memoryStream.Write(buffer[..read]);
                if (memoryStream.Length > downloadLimit)
                    return null;
            }
            
            return memoryStream.ToArray();
        }
    }
}