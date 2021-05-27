using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookstore.Models
{
    public class Settings
    {
        [Key, ForeignKey("User")]
        public ulong UserId { get; set; }
        public bool ArchiveByDefault { get; set; }
        public string DefaultQuery { get; set; }
        public uint DefaultPaginationLimit { get; set; }
    }
}