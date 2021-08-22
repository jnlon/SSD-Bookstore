using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Schema;
using Bookstore.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Utilities
{
    public class BookmarkArchiver
    {
        private class DownloadException : Exception
        {
            public readonly Uri Url;
            public readonly HttpResponseMessage? Response;

            public DownloadException(string message, Exception? inner) : base(message, inner) { }
            public DownloadException(string message, Uri url, HttpResponseMessage? response, Exception? inner = null) : this(message, inner) 
            {
                Url = url;
                Response = response;
            }
        }
        
        private HttpClient _httpClient;

        public BookmarkArchiver()
        {
            var handler = new HttpClientHandler();
            handler.MaxConnectionsPerServer = 1;
            _httpClient = new HttpClient(handler);
        }

        public async Task<HttpResponseMessage> Download(Uri url)
        {
            if (url.Scheme != "http" && url.Scheme != "https")
                throw new DownloadException("URL scheme must be either HTTP or HTTPS", url, null); // TODO

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url);
            }
            catch (HttpRequestException e)
            {
                throw new DownloadException(e.Message, url, null, e);
            }
            
            if (response.Headers.Location != null)
                throw new DownloadException("HTTP response returned a redirect location", url, response); // TODO
            
            if (!response.IsSuccessStatusCode)
                throw new DownloadException("HTTP response did not return 200 OK", url, response);
            
            // TODO FIXME: Byte limit should be configurable in startup env
            if (response.Content.Headers.ContentLength > 10_000_000)
                throw new DownloadException($"Response content length is too large", url, response);

            return response;
        }

        public static string PlainTextFromHtml(HtmlDocument doc)
        {
            string rawText = doc.DocumentNode?.SelectNodes("//body")?.FirstOrDefault()?.InnerText ?? "";
            string normalizedText = Regex.Replace(rawText, @"\W+", " ");
            return normalizedText;
        }

        private string CleanupDocument(HtmlDocument document)
        {
            void RemoveNode(HtmlNode n)
            {
                n.Remove();
            }
            
            void ForAllNodes(string selector, Action<HtmlNode> action)
            {
                var collection = document.DocumentNode?.SelectNodes(selector);
                if (collection != null)
                {
                    foreach (var n in collection)
                    {
                        action(n);
                    }
                }
            }

            string[] removeQueries = 
            {
                "//link", "//script", "//img",
                "//style", "//svg", /*"//header",*/
                "//footer", "//nav", "//input",
                "//button", "//textarea", "//form" 
            };

            foreach (var query in removeQueries)
            {
                ForAllNodes(query, RemoveNode);
            }
            
            ForAllNodes("//*[@style]", n => n.Attributes["style"].Remove());

            return document.DocumentNode.OuterHtml;
        }

        public async Task Archive(Bookmark bookmark)
        {
            HttpResponseMessage response;
            try
            {
                response = await Download(bookmark.Url);
            }
            catch (DownloadException dle)
            {
                // TODO: We should log something here, or add the error message to the archive object?
                return;
            }
            
            string mime = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            byte[] raw = await response.Content.ReadAsByteArrayAsync();

            string? plainText = null;
            byte[]? cleanHtml = null;
            
            if (mime == "application/xhtml+xml" || mime == "text/html")
            {
                HtmlDocument document = new();
                document.Load(new MemoryStream(raw));
                plainText = PlainTextFromHtml(document);
                cleanHtml = Encoding.ASCII.GetBytes(CleanupDocument(document));
            }
            
            bookmark.Archive = new Archive
            {
                UserId = bookmark.UserId,
                Created = DateTime.Now,
                Bytes = raw,
                PlainText = plainText,
                Formatted = cleanHtml,
                Mime = mime,
            };
        }
        
        public void ArchiveAll(IEnumerable<Bookmark> bookmarks)
        {
            Task.WaitAll(bookmarks.Select(Archive).ToArray());
            // Parallel.ForEach(bookmarks, bookmark => Archive(bookmark).Wait());
        }
    }
}