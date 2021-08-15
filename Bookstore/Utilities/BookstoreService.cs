using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Bookstore.Constants.Authorization;
using Bookstore.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Utilities
{
    public class BookstoreService
    {
        private BookmarksContext _context;
        private User? _user;
        public User User => _user!;
        
        private User GetUserFromClaims(ClaimsPrincipal user)
        {
            var maybeValue = user.FindFirst(Claims.UserId)?.Value;
            if (maybeValue is null)
                return null;
            ulong value = ulong.Parse(maybeValue);
            return _context.Users.First(u => u.Id == value);
        }
        
        public BookstoreService(ClaimsPrincipal? userPrincipal, BookmarksContext context)
        {
            _context = context;
            _user = userPrincipal != null ? GetUserFromClaims(userPrincipal) : null;
        }

        public IQueryable<Bookmark> QueryAllUserBookmarks()
        {
            return _context.Bookmarks
                .Include(bm => bm.Folder)
                .Include(bm => bm.Tags)
                .Include(bm => bm.Archive)
                .Include(bm => bm.User)
                .Where(bm => bm.User.Id == _user.Id);
        }

        public Bookmark? QuerySingleBookmarkById(ulong id)
        {
            var bookmark = _context.Bookmarks.Find(id);
            return bookmark.UserId == _user.Id ? bookmark : null; 
        }

        public IQueryable<Tag> QueryAllUserTags()
        {
            return _context.Tags
                .Where(tag => tag.UserId == _user.Id);
        }

        public IQueryable<Folder> QueryAllUserFolders()
        {
            return _context.Folders
                .Include(f => f.Parent)
                .Where(f => f.UserId == _user.Id);
        }
    }
}