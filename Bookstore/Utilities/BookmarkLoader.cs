using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Bookstore.Models;
using Bookstore.Views.Admin;
using HtmlAgilityPack;

namespace Bookstore.Utilities
{
    // Class to "preload" a bookmark before the user creates it. This fetches the bookmark title and favicon values
    public class BookmarkLoader
    {
        private class Download
        {
            public readonly HttpResponseMessage? Response;

            private Download(HttpResponseMessage response)
            {
                Response = response;
            }

            public static async Task<Download> Create(HttpClient client, Uri uri)
            {
                try
                {
                    var response = await client.GetAsync(HttpUtility.UrlDecode(uri.ToString()));
                    return new Download(response);
                }
                catch (Exception)
                {
                    return new Download(null);
                }
            }
            
            public bool Success => Response?.IsSuccessStatusCode ?? false;
            public string? ContentType => Response?.Content.Headers.ContentType?.ToString();
            public bool IsImageMime => ContentType?.ToLower().Contains("image") ?? false;
            public bool IsHtmlMime => ContentType?.ToLower().Contains("html") ?? false;
            public Uri? Location => Response?.Headers.Location;
            public Uri? RequestUrl => Response?.RequestMessage?.RequestUri;

            public byte[]? ReadAsBytes(uint limitBytes)
            {
                return Response?.Content.ReadAsByteArrayLimited(limitBytes);
            }
        }

        private class Html
        {
            public string? Title { get; }
            public Uri? IconUri { get; }
            private readonly Uri _source;
            private readonly HtmlDocument _doc;
            public Html(byte[] content, Uri source)
            {
                _source = source;
                _doc = new HtmlDocument();
                var encoding = DetectPageEncoding(content);
                _doc.Load(new MemoryStream(content), encoding);
                IconUri = FindIconUri(source);
                Title = _doc.DocumentNode.SelectSingleNode("//head/title")?.InnerText;
            }

            private static Encoding DetectPageEncoding(byte[] content)
            {
                var doc = new HtmlDocument();
                doc.Load(new MemoryStream(content));
                
                // Look for <meta charset=""> tag
                string? metaCharset = doc.DocumentNode
                    ?.SelectNodes("//meta[@charset]")
                    ?.FirstOrDefault()
                    ?.Attributes["charset"]
                    .Value;

                try
                {
                    if (metaCharset != null)
                    {
                        return CodePagesEncodingProvider.Instance.GetEncoding(metaCharset) ?? Encoding.UTF8;
                    }
                }
                catch (ArgumentException) { }
                
                return Encoding.UTF8;
            }

            private Uri? FindIconUri(Uri sourceUri)
            {
                string? href = _doc.DocumentNode.SelectSingleNode(@"//head/link[contains(@rel, 'icon')]")?.Attributes["href"]?.Value;
                href ??= WebUtility.UrlDecode(href);
                
                if (href is null)
                    return null;
                if (href.StartsWith("http://") || href.StartsWith("https://"))
                    return new UriBuilder(href).Uri;
                if (href.StartsWith("//"))
                    return new UriBuilder(sourceUri.Scheme + ":" + href).Uri;
                if (href.StartsWith("/"))
                    return new UriBuilder(_source) { Path = href }.Uri;
                
                return new UriBuilder(_source) { Path = _source.AbsolutePath + "/" + href }.Uri;
            }
        }

        private static readonly uint MaxDownloadSize = 3 * 1000 * 1000;

        public Favicon? Favicon { get; set; }

        // public string? FaviconMimeType { get; private set; }
       // public byte[]? Favicon { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public Uri FinalUrl { get; private set; } // After redirects
        public Uri OriginalUrl { get; private set; } // Before Redirects

        public byte[]? Raw { get; private set; }
        public HttpResponseMessage? Response { get; private set; }

        public static async Task<BookmarkLoader> Create(Uri bookmarkUrl, HttpClient client)
        {
            Uri originalUri = bookmarkUrl;
            byte[]? rawHtml = null;
            Html? html = null;
            Favicon? favicon = null;
            
            // Download the bookmark. If it was successful, and was an HTML file, load HTMLHelper
            var htmlDownload = await Download.Create(client, bookmarkUrl);

            bookmarkUrl = htmlDownload.RequestUrl ?? bookmarkUrl;

            for (int redirect=0; htmlDownload.Location != null && redirect < 10; redirect++)
            {
                bookmarkUrl = htmlDownload.Location;
                htmlDownload = await Download.Create(client, bookmarkUrl);
            }
            
            if (htmlDownload.Success && htmlDownload.IsHtmlMime)
            {
                rawHtml = htmlDownload.ReadAsBytes(MaxDownloadSize);
                html = new Html(rawHtml ?? new byte[]{}, bookmarkUrl);
            }
            
            // Retrieve favicon from the HTMl, or fall back to standard location
            var faviconUrl = html?.IconUri ?? new UriBuilder(bookmarkUrl) { Path = "/favicon.ico" }.Uri;
            // Attempt to Download the favicon
            var faviconDownload = await Download.Create(client, faviconUrl);
            
            // If the favicon download was successful, and it is an image
            if (faviconDownload.Success && faviconDownload.IsImageMime)
            {
                favicon = new Favicon
                {
                    Data = faviconDownload.ReadAsBytes(MaxDownloadSize),
                    Mime = faviconDownload.ContentType!,
                    Url = faviconDownload.RequestUrl!
                };
            }

            string title = html?.Title == null ? bookmarkUrl.ToString() : WebUtility.HtmlDecode(html.Title);
            
            return new BookmarkLoader
            {
                Title = title,
                Raw = rawHtml,
                Favicon = favicon,
                Response = htmlDownload.Response,
                FinalUrl = bookmarkUrl,
                OriginalUrl = originalUri
            };
        }
    }
}