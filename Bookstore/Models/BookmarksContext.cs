using System.Linq;
using System.Security.Claims;
using Bookstore.Constants.Authorization;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;

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

        private void InsertDevelopmentData(ModelBuilder builder)
        {
            var (passwordHash, passwordSalt) = Crypto.GeneratePasswordHash("123");
            var user = new User() {Id = 1, Admin = false, Username = "toast", PasswordHash = passwordHash, PasswordSalt = passwordSalt};
            
            builder.Entity<User>().HasData(user);

            builder.Entity<Folder>().HasData(new Folder() { Id = 1, Name = "Bun", ParentId = null, UserId = user.Id });
            builder.Entity<Folder>().HasData(new Folder() { Id = 2, Name = "Cheese", ParentId = 1, UserId = user.Id });
            builder.Entity<Folder>().HasData(new Folder() { Id = 3, Name = "Meat", ParentId = 2, UserId = user.Id });
            builder.Entity<Folder>().HasData(new Folder() { Id = 4, Name = "Other Bookmarks", ParentId = null, UserId = user.Id });
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            
            var env = this.GetService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                InsertDevelopmentData(builder);
            }
        }
    }
}