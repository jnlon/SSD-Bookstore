using System;

namespace Bookstore.Models
{
    // Model storing bookmark 'favicon' data
    // Note: This could be stored in regular bookmark model but for performance reasons, we store it outside and only .include() it when necessary.
    // This dramatically speeds up queries involving many bookmarks
    public class Favicon
    {
        public long Id { get; set; }
        public byte[] Data { get; set; }
        public string Mime { get; set; }
        public Uri? Url { get; set; }
    }
}