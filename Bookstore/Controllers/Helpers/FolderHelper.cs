using System.Linq;
using Bookstore.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Controllers.Helpers
{
    internal class FolderHelper
    {
        private DbSet<Folder> _folders;
        private ulong _userId;
        public FolderHelper(DbSet<Folder> folders, ulong userId)
        {
            _folders = folders;
            _userId = userId;
        }

        public Folder? GetFolder(ulong folderId) => _folders.FirstOrDefault(f => f.UserId == _userId && f.Id == folderId);
        // A valid folder is null or exists for this user
        public bool ValidFolder(ulong? folderId) => folderId == null || GetFolder((ulong) folderId) != null;
        public Folder CreateFolder(string name, Folder? parent)
        {
            var folder = new Folder()
            {
                Name = name,
                UserId = _userId,
                Parent = parent
            };
            _folders.Add(folder);
            return folder;
        }
    }
}