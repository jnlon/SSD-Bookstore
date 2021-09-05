using System;

namespace Bookstore.Models
{
    public class Favicon
    {
        public long Id { get; set; }
        public byte[] Data { get; set; }
        public string Mime { get; set; }
        public Uri? Url { get; set; }
    }
}