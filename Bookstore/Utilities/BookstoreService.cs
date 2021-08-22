using System;
using System.Collections.Generic;
using System.IO;
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
        
        private bool IsAdmin => _user?.Admin ?? false;
        
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
                .Include(bm => bm.User)
                .Where(bm => bm.UserId == _user.Id);
        }
        
        public IQueryable<Bookmark> QueryUserBookmarksByIds(long[] ids)
        {
            return QueryAllUserBookmarks().Where(bm => ids.Contains(bm.Id));
        }

        public Bookmark? GetSingleBookmarkById(long id)
        {
            return _context.Bookmarks
                .Include(bm => bm.Tags)
                .Include(bm => bm.Folder)
                .FirstOrDefault(bm => bm.Id == id && bm.UserId == _user.Id);
        }

        public Archive? GetArchiveByBookmarkId(long id)
        {
            var bookmark = _context.Bookmarks
                .Include(bm => bm.Archive)
                .FirstOrDefault(bm => bm.Id == id && bm.UserId == _user.Id);

            return bookmark?.Archive;
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
        
        private long ByteCountOfArchive(Archive? archive)
        {
            if (archive == null)
                return 0;
            
            long sum = archive.Bytes.Length;
            
            if (archive.Formatted != null)
                sum += archive.Formatted.Length;
            
            if (archive.PlainText != null)
                sum += archive.PlainText.Length;

            return sum;
        }
        
        public void DeleteUserById(long id)
        {
            if (!IsAdmin)
                throw new UnauthorizedAccessException("User must be an administrator to access this resource");

            var user = _context.Users
                .Include(u => u.Settings)
                .Include(u => u.Bookmarks).ThenInclude(bm => bm.Archive)
                .Include(u => u.Bookmarks).ThenInclude(bm => bm.Folder).ThenInclude(f => f!.Parent)
                .Include(u => u.Bookmarks).ThenInclude(bm => bm.Tags)
                .First(u => u.Id == id);

            _context.Bookmarks.RemoveRange(user.Bookmarks);
            _context.Users.Remove(user);
        }


        public List<UserStatistics> GetUserStatistics()
        {
            if (!IsAdmin)
                throw new UnauthorizedAccessException("User must be an administrator to access this resource");

            UserStatistics StatsFromUser(User u)
            {
                //var bookmarks = _context.Bookmarks.Include(bm => bm.Archive).Where();

                IQueryable<Bookmark> UserBookmarks() => _context.Bookmarks
                    .Where(bm => bm.UserId == u.Id);

                IQueryable<Archive> UserArchives() => _context.Archives
                    .Where(ar => ar.UserId == u.Id);

                var nob = UserBookmarks().Count();
                var abdu = UserArchives().Sum(ByteCountOfArchive);
                var noab = UserArchives().Count();

                return new UserStatistics
                {
                    User = u,
                    NumberOfBookmarks = nob,
                    ArchivedBookmarkDiskUsage = abdu,
                    NumberOfArchivedBookmarks = noab
                };
            }
            
            return _context.Users.ToList().Select(StatsFromUser).ToList();
        }

        public AdminStatistics GetAdminStatistics()
        {
            if (!IsAdmin)
                throw new UnauthorizedAccessException("User must be an administrator to access this resource");

            return new AdminStatistics
            {
                NumberOfUsers = _context.Users.Count(),
                ArchivedDiskUsageBytes = _context.Archives.Sum(ByteCountOfArchive),
                TotalNumberOfBookmarks = _context.Bookmarks.Count(),
                TotalNumberOfArchivedBookmarks = _context.Archives.Count(),
                TotalSpaceOnDisk = new DriveInfo(Directory.GetDirectoryRoot(Environment.CurrentDirectory)).TotalSize
            };
        }

        // Remove 
        public void CleanupBookmarkOrphans()
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
            
            var archivesIdsToDelete = bookmarksToDelete
                .Where(bm => bm.ArchiveId != null)
                .Select(bm => bm.ArchiveId)
                .ToList();

            var archivesToDelete = _context.Archives
                .Where(ar => archivesIdsToDelete.Contains(ar.Id));

            var foldersToDelete = allFolders
                .Where(CanDeleteFolder)
                .ToList();
            
            var tagsToDelete = QueryAllUserTags()
                .Include(tag => tag.Bookmarks)
                .ToList()
                .Where(tag => tag.Bookmarks.All(bm => bookmarksToDelete.Any(e => e.Id == bm.Id)))
                .ToList();
            
            _context.Folders.RemoveRange(foldersToDelete);
            _context.Tags.RemoveRange(tagsToDelete);
            _context.Archives.RemoveRange(archivesToDelete);
        }
    }
}