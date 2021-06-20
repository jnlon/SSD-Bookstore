using System.Collections.Generic;
using System.Linq;
using Bookstore.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Controllers.Helpers
{
    internal class TagHelper
    {
        private DbSet<Tag> _tagsDbSet;
        private ulong _userId;
        private HashSet<Tag> _userTags;
        
        public TagHelper(DbSet<Tag> tagsDbSet, ulong userId)
        {
            _tagsDbSet = tagsDbSet;
            _userId = userId;
            _userTags = AllUserTags.ToHashSet();
        }
        private IEnumerable<Tag> AllUserTags => _tagsDbSet.Where(t => t.UserId == _userId);
        private Tag? GetExistingTag(string tagName) => _userTags.FirstOrDefault(t => t.Name == tagName);
        private Tag CreateNewTag(string tagName) => new Tag {Name = tagName, UserId = _userId};
        public IEnumerable<string> ParseTagList(string tagList) => tagList.Split(",").Select(t => t.Trim().ToLower());
        public Tag GetOrCreateNewTag(string tagName) => GetExistingTag(tagName) ?? CreateNewTag(tagName);
    }
}