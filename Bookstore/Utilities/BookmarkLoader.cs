using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using HtmlAgilityPack;

namespace Bookstore.Utilities
{
    // Class to "preload" a bookmark before the user creates it. This fetches the bookmark title and favicon values
    public class BookmarkLoader
    {
        private class Download
        {
            private readonly HttpResponseMessage? _response;
            public Download(HttpClient client, Uri uri)
            {
                try
                {
                    _response = client.GetAsync(uri).Result;
                }
                catch (Exception)
                {
                    _response = null;
                }
            }
            
            public bool Success => _response?.IsSuccessStatusCode ?? false;
            public string? ContentType => _response?.Content.Headers.ContentType?.ToString();
            public bool IsImageMime => ContentType?.ToLower().Contains("image") ?? false;
            public bool IsHtmlMime => ContentType?.ToLower().Contains("html") ?? false;

            public Stream? ReadAsStream(uint limitBytes)
            {
                if (_response.Content.Headers.ContentLength < limitBytes)
                    return _response.Content.ReadAsStream();
                
                return null;
            }

            public byte[]? ReadAsBytes(uint limitBytes)
            {
                return _response.Content.ReadAsByteArrayLimited(limitBytes);
            }
        }

        private class Html
        {
            public string? Title { get; }
            public Uri? IconUri { get; }
            private readonly Uri _source;
            private readonly HtmlDocument _doc;
            public Html(Stream? content, Uri source)
            {
                _source = source;
                _doc = new HtmlDocument();
                _doc.Load(content ?? new MemoryStream());
                IconUri = FindIconUri();
                Title = _doc.DocumentNode.SelectSingleNode("//head/title")?.InnerText;
            }

            private Uri? FindIconUri()
            {
                string? href = _doc.DocumentNode.SelectSingleNode(@"//head/link[@rel=""icon""]")?.Attributes["href"]?.Value;
                
                if (href is null)
                    return null;
                if (href.StartsWith("http://") || href.StartsWith("https://"))
                    return new UriBuilder(href).Uri;
                if (href.StartsWith("/"))
                    return new UriBuilder(_source) { Path = href }.Uri;
                
                return new UriBuilder(_source) { Path = _source.AbsolutePath + "/" + href }.Uri;
            }
        }

        private static readonly uint MaxDownloadSize = 3 * 1000 * 1000;

        public string? FaviconMimeType { get; private set; }
        public byte[]? Favicon { get; private set; }
        public string Title { get; private set; } = string.Empty;

        public static BookmarkLoader Create(Uri bookmarkUrl, HttpClient client)
        {
            Html? html = null;
            byte[]? favicon = null;
            string? faviconMimeType = null;
            
            // Download the bookmark. If it was successful, and was an HTML file, load HTMLHelper
            var htmlDownload = new Download(client, bookmarkUrl);
            if (htmlDownload.Success && htmlDownload.IsHtmlMime)
                html = new Html(htmlDownload.ReadAsStream(MaxDownloadSize), bookmarkUrl);
            
            // Retrieve favicon from the HTMl, or fall back to standard location
            var faviconUrl = html?.IconUri ?? new UriBuilder(bookmarkUrl) { Path = "/favicon.ico" }.Uri;
            // Attempt to Download the favicon
            var faviconDownload = new Download(client, faviconUrl);
            
            // If the favicon download was successful, and it is an image
            if (faviconDownload.Success && faviconDownload.IsImageMime)
            {
                favicon = faviconDownload.ReadAsBytes(MaxDownloadSize);
                faviconMimeType = faviconDownload.ContentType;
            }

            string title = html?.Title == null ? bookmarkUrl.ToString() : WebUtility.HtmlDecode(html.Title);
            
            return new BookmarkLoader()
            {
                FaviconMimeType = faviconMimeType,
                Favicon = favicon,
                Title = title
            };
        }
    }
}