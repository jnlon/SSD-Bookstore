using System.Linq;
using System.Security.Claims;
using Bookstore.Constants.Authorization;
using Bookstore.Utilities;
using Microsoft.EntityFrameworkCore;

// Bookstore.Models.BookmarksContext

namespace Bookstore.Models
{
    public class BookmarksContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Folder> Folders { get; set; }

        public BookmarksContext(DbContextOptions<BookmarksContext> options) : base(options)
        {
        }

        public IQueryable<User> GetUser(ClaimsPrincipal user)
        {
            var maybeValue = user.FindFirst(Claims.UserId)?.Value;
            if (maybeValue is null)
                return null;
            ulong value = ulong.Parse(maybeValue);
            return Users.Where(u => u.Id == value);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            var (passwordHash, passwordSalt) = Crypto.GeneratePasswordHash("123");
            var user = new User()
                {Id = 1, Admin = false, Username = "toast", PasswordHash = passwordHash, PasswordSalt = passwordSalt};
            
            builder.Entity<User>().HasData(user);

            builder.Entity<Folder>().HasData(new Folder() { Id = 1, Name = "Bun", ParentId = null, UserId = user.Id });
            builder.Entity<Folder>().HasData(new Folder() { Id = 2, Name = "Cheese", ParentId = 1, UserId = user.Id });
            builder.Entity<Folder>().HasData(new Folder() { Id = 3, Name = "Meat", ParentId = 2, UserId = user.Id });
        }
    }
}