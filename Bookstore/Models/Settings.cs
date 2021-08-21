using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookstore.Models
{
    public class Settings
    {
        [Key, ForeignKey("User")]
        public long UserId { get; set; }
        public bool ArchiveByDefault { get; set; }
        public string DefaultQuery { get; set; }
        public int DefaultPaginationLimit { get; set; }

        public static Settings CreateDefault()
        {
            return new Settings()
            {
                ArchiveByDefault = false,
                DefaultQuery = "",
                DefaultPaginationLimit = 100
            };
        }
    }
}