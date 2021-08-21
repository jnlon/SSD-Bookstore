using System.Collections.Generic;
using System.Linq;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Controllers.Helpers
{
    internal class TagHelper
    {
        private long _userId;
        private HashSet<Tag> _userTags;
        private BookmarksContext _context;
        
        public TagHelper(BookstoreService bookstore, BookmarksContext context)
        {
            _userId = bookstore.User.Id;
            _userTags = bookstore.QueryAllUserTags().ToHashSet();
            _context = context;
        }
        
        private Tag? GetExistingTag(string tagName)
        {
            return _userTags.FirstOrDefault(t => t.Name == tagName);
        }

        private Tag CreateNewTag(string tagName)
        {
            var tag = new Tag {Name = tagName, UserId = _userId};
            _userTags.Add(tag);
            _context.Tags.Add(tag);
            return tag;
        }

        public IEnumerable<string> ParseTagList(string tagList)
        {
            return tagList.Split(",").Select(t => t.Trim().ToLower());   
        }

        public Tag GetOrCreateNewTag(string tagName)
        {
            return GetExistingTag(tagName) ?? CreateNewTag(tagName);   
        } 
    }
}