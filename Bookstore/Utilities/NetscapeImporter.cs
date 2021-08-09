using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using BookmarksManager;
using Bookstore.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Utilities
{
    public class NetscapeImporter
    {
        private BookmarksContext _context;
        private List<Folder> _folders;
        private User _user;

        public NetscapeImporter(BookmarksContext context, List<Folder> userFolders, ClaimsPrincipal userClaim)
        {
            _context = context;
            _user = _context.GetUser(userClaim)
               .Include(u => u.Bookmarks)
               .First();
            _folders = _context.Folders
                .Include(f => f.Parent)
                .Where(f => f.UserId == _user.Id)
                .ToList();
        }

        private Tag ResolveTag(string tagName)
        {
           // Create new tag if it does not exist
           Tag? tag = _context.Tags.FirstOrDefault(t => t.Name == tagName && t.UserId == _user.Id);
           
           if (tag is null)
           {
               tag = new Tag 
               {
                   Name = tagName,
                   UserId = _user.Id
               };
               _context.Tags.Add(tag);
           }
           
           return tag;
        }

        private List<Tag> ResolveTags(BookmarkLink link)
        {
            if (link.Attributes.ContainsKey("tags"))
            {
               return link.Attributes["tags"]
                   .Split(',')
                   .Select(ResolveTag)
                   .ToList();
            }

            return new List<Tag>();
        }

        private Folder? ResolveFolder(string[] folderPath)
        {
            foreach (Folder folder in _folders)
            {
                if (folder.ToStringArray().SequenceEqual(folderPath))
                {
                    return folder;
                }
            }
            
            return null;
        }

        private Folder CreateFolderPath(string[] folderPath)
        {
            int resolvedDepth = 0;
            Folder? resolved = null;
            for (int i=0; i <= folderPath.Length; i++)
            {
                resolved = ResolveFolder(folderPath.SkipLast(i).ToArray());
                if (resolved != null)
                {
                    resolvedDepth = folderPath.Length - i;
                    break;
                }
            }

            foreach (var unresolvedFolderSegment in folderPath.Skip(resolvedDepth).ToArray())
            {
                Folder newFolder = new Folder()
                {
                    Name = unresolvedFolderSegment,
                    Parent = resolved,
                    UserId = _user.Id
                };
                _folders.Add(newFolder);
                _context.Add(newFolder);
                resolved = newFolder;
            }

            return resolved!;
        }

        private string ExtractIconMime(BookmarkLink link)
        {
            if (link.IconUrl?.StartsWith("data:") ?? false)
            {
                var match = Regex.Match(link.IconUrl, @"^data:(?<mime>[\w/\-\.]+)(;\w+)?,");
                var mime = match.Groups["mime"];
                if (mime.Success)
                {
                    return mime.Value;
                }
            }
            
            return link.IconContentType;
        }

        public string DetectFaviconMimeType(BookmarkLink link)
        {
            // NOTE HACK: Firefox netscape export will incorrectly classify SVG favicons as 'image/png' in data URL of netscape export
            // Correct this with a heuristic; look at the canonical URL for hints
            if ((link.IconUrl?.EndsWith(".svg") ?? false) ||
                (link.IconUrl?.StartsWith("data:image/svg+xml") ?? false))
            {
                return "image/svg+xml";
            }

            return link.IconContentType;
        }

        private void ImportBookmark(string[] folderStackArray, BookmarkLink link)
        {
           _context.Bookmarks.Add(new Bookmark()
           {
               Archive = null,
               Created = link.Added ?? new DateTime(),
               Favicon = (link.IconData != null && link.IconData.Length > 0) ? link.IconData : null,
               FaviconMime = (link.IconData != null && link.IconData.Length > 0) ? DetectFaviconMimeType(link) : null,
               Folder = ResolveFolder(folderStackArray) ?? CreateFolderPath(folderStackArray),
               Modified = link.Added ?? new DateTime(),
               Tags = ResolveTags(link).ToHashSet(),
               Title = link.Title,
               User = _user,
               Url = new Uri(link.Url)
           });
        }

        private void ImportFolder(Stack<string> folderStack, BookmarkFolder currentFolder)
        {
            string[] folderStackArray = folderStack.Reverse().ToArray(); // Note: Avoid allocating new array each loop iteration
            foreach (var link in currentFolder.Where(entry => entry is BookmarkLink))
                ImportBookmark(folderStackArray, (BookmarkLink)link);

            foreach (var nextFolder in currentFolder.Where(entry => entry is BookmarkFolder))
            {
                folderStack.Push(nextFolder.Title);
                ImportFolder(folderStack, (BookmarkFolder)nextFolder);
                folderStack.Pop();
            }
        }

        public void Import(Stream stream)
        {
           var parsed = new NetscapeBookmarksReader().Read(stream);
           ImportFolder(new Stack<string>(), parsed);
           _context.SaveChanges(); 
        }
    }
}