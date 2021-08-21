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
            long value = long.Parse(maybeValue);
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
                .Where(bm => bm.UserId == _user.Id);
        }
        
        public IQueryable<Bookmark> QueryUserBookmarksByIds(long[] ids)
        {
            return QueryAllUserBookmarks().Where(bm => ids.Contains(bm.Id));
        }

        public Bookmark? QuerySingleBookmarkById(long id)
        {
            return _context.Bookmarks
                .Include(bm => bm.Tags)
                .Include(bm => bm.Folder)
                .FirstOrDefault(bm => bm.Id == id && bm.UserId == _user.Id);
        }

        public IQueryable<Tag> QueryAllUserTags()
        {
            return _context.Tags.Where(tag => tag.UserId == _user.Id);
        }

        public IQueryable<Folder> QueryAllUserFolders()
        {
            return _context.Folders
                .Include(f => f.Parent)
                .Where(f => f.UserId == _user.Id);
        }

        public Settings GetUserSettings()
        {
            Settings? settings = _context.Users
                .Include(u => u.Settings)
                .FirstOrDefault(u => u.Id == _user.Id)
                ?.Settings;

            return settings ?? Settings.CreateDefault();
        }

        // Remove 
        public void RefreshTagsAndFolders()
        {
            var bookmarksToDelete = _context.ChangeTracker
                .Entries<Bookmark>()
                .Where(e => e.State == EntityState.Deleted)
                .Select(ent => ent.Entity)
                .ToList();
            
            var allFolders = QueryAllUserFolders()
                .Include(f => f.Bookmarks)
                .ToList();
            
            // 2 conditions must be met before we attempt deleting the folder:
            // - If any bookmarks exist, they are are going to be deleted
            // - If any child folders exist, it contains no bookmarks or only bookmarks to be deleted
            
            bool CanDeleteFolder(Folder folder)
            {
                bool isEmptyFolder = folder
                    .Bookmarks
                    .All(bm => bookmarksToDelete.Any(tdl => tdl.Id == bm.Id));

                return isEmptyFolder &&
                       allFolders.Where(f => f.ParentId == folder.Id).All(CanDeleteFolder);
            }
            
            var foldersToDelete = allFolders.Where(CanDeleteFolder).ToList();
            var tagsToDelete = QueryAllUserTags()
                .Include(tag => tag.Bookmarks)
                .ToList()
                .Where(tag => tag.Bookmarks.All(bm => bookmarksToDelete.Any(e => e.Id == bm.Id)))
                .ToList();
            
            _context.Folders.RemoveRange(foldersToDelete);
            _context.Tags.RemoveRange(tagsToDelete);
        }
    }
}