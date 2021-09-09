using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Controllers.Dto
{
    public class UpdateSettingsDto
    {
        [FromForm(Name = "default-query")]
        public string? DefaultQuery { get; set; }

        [FromForm(Name = "default-max-results")]
        public int? DefaultMaxResults { get; set; }

        [FromForm(Name = "archive-by-default")]
        public bool ArchiveByDefault { get; set; }
    }
}