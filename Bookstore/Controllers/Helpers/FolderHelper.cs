using System.Collections.Generic;
using System.Linq;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Controllers.Helpers
{
    internal class FolderHelper
    {
        private List<Folder> _folders;
        private BookmarksContext _context;
        private BookstoreService _bookstore;
        
        public FolderHelper(BookstoreService bookstore, BookmarksContext context)
        {
            _bookstore = bookstore;
            _folders = bookstore.QueryAllUserFolders().ToList();
            _context = context;
        }

        public Folder? GetFolder(ulong folderId)
        {
            return _folders.FirstOrDefault(f => f.Id == folderId);   
        } 
        
        // A valid folder is null or exists for this user
        public bool ValidFolder(ulong? folderId)
        {
            return folderId == null || GetFolder((ulong) folderId) != null;   
        } 
        
        public Folder CreateFolder(string name, Folder? parent)
        {
            var folder = new Folder()
            {
                Name = name,
                UserId = _bookstore.User.Id,
                Parent = parent
            };
            _folders.Add(folder);
            _context.Folders.Add(folder);
            return folder;
        }
    }
}