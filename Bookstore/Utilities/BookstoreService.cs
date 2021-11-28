using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Bookstore.Common.Authorization;
using Bookstore.Controllers.Dto;
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
            var maybeValue = user.FindFirst(BookstoreClaims.UserId)?.Value;
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
            // Load user folders into context memory, so folders whose parents have no bookmarks are still included
            _context.Folders.Where(f => f.UserId == _user.Id).Load();

            return _context.Bookmarks
                .AsSplitQuery()
                .Include(bm => bm.Folder).ThenInclude(f => f.Parent)
                .Include(bm => bm.Tags)
                .Include(bm => bm.User)
                .Where(bm => bm.UserId == _user.Id);
        }

        public bool ValidateUserPassword(string? password)
        {
            return password != null &&
                   CryptoUtility.PasswordHashMatches(password, _user.PasswordHash, _user.PasswordSalt);
        }
        
        public bool ValidateNewPassword(string? password, string? confirmPassword, out string? error)
        {
            error = null;

            if (password == null || confirmPassword == null)
            {
                error = "Password and confirm password cannot be missing";
            }
            else if (password != confirmPassword)
            {
                error = "Password confirmation does not match";
            }
            else if (password.Length < 12)
            {
                error = @"Password must be at least 12 characters in length";
            }

            // Return true if password is valid
            return error == null;
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
                .Include(bm => bm.Favicon)
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


        public List<UserStatisticsDto> GetUserStatistics()
        {
            if (!IsAdmin)
                throw new UnauthorizedAccessException("User must be an administrator to access this resource");

            UserStatisticsDto StatsFromUser(User u)
            {
                //var bookmarks = _context.Bookmarks.Include(bm => bm.Archive).Where();

                IQueryable<Bookmark> UserBookmarks() => _context.Bookmarks
                    .Where(bm => bm.UserId == u.Id);

                IQueryable<Archive> UserArchives() => _context.Archives
                    .Where(ar => ar.UserId == u.Id);

                var nob = UserBookmarks().Count();
                var abdu = UserArchives().Sum(ByteCountOfArchive);
                var noab = UserArchives().Count();

                return new UserStatisticsDto
                {
                    User = u,
                    NumberOfBookmarks = nob,
                    ArchivedBookmarkDiskUsage = abdu,
                    NumberOfArchivedBookmarks = noab
                };
            }
            
            return _context.Users.ToList().Select(StatsFromUser).ToList();
        }

        public AdminStatisticsDto GetAdminStatistics()
        {
            if (!IsAdmin)
                throw new UnauthorizedAccessException("User must be an administrator to access this resource");

            return new AdminStatisticsDto
            {
                NumberOfUsers = _context.Users.Count(),
                ArchivedDiskUsageBytes = _context.Archives.Sum(ByteCountOfArchive),
                TotalNumberOfBookmarks = _context.Bookmarks.Count(),
                TotalNumberOfArchivedBookmarks = _context.Archives.Count(),
                TotalSpaceOnDisk = new DriveInfo(Directory.GetDirectoryRoot(Environment.CurrentDirectory)).TotalSize
            };
        }

        public User? GetUserById(long id)
        {
            if (!IsAdmin)
                throw new UnauthorizedAccessException("User must be an administrator to access this resource");

            return _context.Users.FirstOrDefault(u => u.Id == id);
        }
        
        public User? GetUserByUserName(string username)
        {
            if (!IsAdmin)
                throw new UnauthorizedAccessException("User must be an administrator to access this resource");
            
            return _context.Users.FirstOrDefault(n => n.Username == username);
        }
        
        // Note: Updates settings for the *current user*
        public void UpdateUserSettings(string defaultQuery, bool archiveByDefault, int defaultPaginationLimit)
        {
            User user = _context.Users
                .Include(u => u.Settings)
                .FirstOrDefault(u => u.Id == User.Id)!;
                
            user.Settings = new Settings
            {
                DefaultQuery = defaultQuery,
                UserId = User.Id,
                ArchiveByDefault = archiveByDefault,
                DefaultPaginationLimit = defaultPaginationLimit
            };
        }
        
        public void UpdateSelfCredentials(string username, string password)
        {
            UpdateUserCredentials(User, username, password);
        }

        public void UpdateUserCredentials(long id, string username, string password)
        {
            User? user = GetUserById(id);
            
            if (user == null)
                throw new ArgumentException($"User with ID {id} does not exist");
            
            UpdateUserCredentials(user, username, password);
        }
        
        private void UpdateUserCredentials(User user, string username, string password)
        {
            var (passwordHash, passwordSalt) = CryptoUtility.GeneratePasswordHash(password);
            user.Username = username;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
        }
        
        public Tag CreateNewTag(string tagName)
        {
            var tag = new Tag { Name = tagName, UserId = _user.Id };
            _context.Tags.Add(tag);
            return tag;
        }
        
        public Folder CreateFolder(string folderName, Folder? parent)
        {
            var folder = new Folder { Bookmarks = new(), Name = folderName, Parent = parent, UserId = _user.Id};
            _context.Folders.Add(folder);
            return folder;
        }

        public Bookmark CreateBookmark(Archive? archive, DateTime created, DateTime modified, byte[]? faviconData, string? faviconMime, Uri? faviconUrl, Folder? folder, HashSet<Tag> tags, string title, Uri url)
        {
            Favicon favicon = null;
            if (faviconData != null && faviconMime != null)
            {
                favicon = new Favicon
                {
                    Data = faviconData,
                    Mime = faviconMime,
                    Url = faviconUrl,
                };
            }
            
            var bookmark = new Bookmark
            {
               Archive = archive, 
               Created = created,
               Favicon = favicon,
               Folder = folder,
               Modified = modified,
               Tags = tags,
               Title = title.Trim(),
               User = _user,
               Url = url
            };
            _context.Bookmarks.Add(bookmark);
            return bookmark;
        }
        
        public void CreateNewUser(string username, string password, bool admin)
        {
            var (passwordHash, passwordSalt) = CryptoUtility.GeneratePasswordHash(password);
            _context.Users.Add(new User
            {
                Admin = admin,
                Bookmarks = new List<Bookmark>(),
                Settings = Settings.CreateDefault(),
                Username = username,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            });
        }

        // Remove 
        public void CleanupBookmarkOrphans()
        {
            var bookmarksToDelete = _context.ChangeTracker
                .Entries<Bookmark>()
                .Where(e => e.State == EntityState.Deleted)
                .Select(ent => ent.Entity)
                .ToList();
            
            var foldersToAdd = _context.ChangeTracker
                .Entries<Folder>()
                .Where(e => e.State == EntityState.Added)
                .Select(ent => ent.Entity)
                .ToList();

            // var tagsToAdd = _context.ChangeTracker
            //     .Entries<Tag>()
            //     .Where(e => e.State == EntityState.Added)
            //     .Select(ent => ent.Entity)
            //     .ToList();

            var allFolders = QueryAllUserFolders()
                .Include(f => f.Bookmarks)
                .ToList()
                .Concat(foldersToAdd)
                .ToList();
            
            // 2 conditions must be met before we attempt deleting the folder:
            // - If any bookmarks exist, they are are going to be deleted
            // - If any child folders exist, it contains no bookmarks or only bookmarks to be deleted
            
            bool CanDeleteFolder(Folder folder)
            {
                // All the bookmarks in this folder are going to be deleted
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
                .Where(f => !foldersToAdd.Any(fa => fa.ToArray().SequenceEqual(f.ToArray())))
                .ToList();
            
            var tagsToDelete = QueryAllUserTags()
                .Include(tag => tag.Bookmarks)
                .ToList()
                .Where(tag =>
                    tag.Bookmarks.All(bm => bookmarksToDelete.Any(e => e.Id == bm.Id))
                ).ToList();
            
            _context.Folders.RemoveRange(foldersToDelete);
            _context.Tags.RemoveRange(tagsToDelete);
            _context.Archives.RemoveRange(archivesToDelete);
        }
    }
}