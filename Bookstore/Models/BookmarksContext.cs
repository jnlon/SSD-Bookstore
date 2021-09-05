using Bookstore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Bookstore.Models
{
    public class BookmarksContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<Archive> Archives { get; set; }

        public BookmarksContext(DbContextOptions<BookmarksContext> options) : base(options)
        {
        }


        private void InsertProductionData(ModelBuilder builder, IConfiguration config)
        {
            var (passwordHash, passwordSalt) = Crypto.GeneratePasswordHash(config["Bookstore:DefaultPassword"]);
            var adminUser = new User() {Id = 1, Admin = true, Username = "admin", PasswordHash = passwordHash, PasswordSalt = passwordSalt};
            builder.Entity<User>().HasData(adminUser);
        }

        private void InsertDevelopmentData(ModelBuilder builder, IConfiguration config)
        {
            var (passwordHash, passwordSalt) = Crypto.GeneratePasswordHash(config["Bookstore:DefaultPassword"]);
            var adminUser = new User {Id = 1, Admin = true, Username = "admin", PasswordHash = passwordHash, PasswordSalt = passwordSalt};
            builder.Entity<User>().HasData(adminUser);
            
            (passwordHash, passwordSalt) = Crypto.GeneratePasswordHash(config["Bookstore:DefaultPassword"]);
            var standardUser = new User {Id = 2, Admin = false, Username = "toast", PasswordHash = passwordHash, PasswordSalt = passwordSalt};
            builder.Entity<User>().HasData(standardUser);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            builder.Entity<Bookmark>().Property(t => t.FolderString).UsePropertyAccessMode(PropertyAccessMode.Property);
            builder.Entity<Bookmark>().Property(t => t.TagString).UsePropertyAccessMode(PropertyAccessMode.Property);
            builder.Entity<Bookmark>().Property(t => t.UrlString).UsePropertyAccessMode(PropertyAccessMode.Property);

            IWebHostEnvironment env = this.GetService<IWebHostEnvironment>();
            IConfiguration config = this.GetService<IConfiguration>();
            if (env.IsDevelopment())
            {
                InsertDevelopmentData(builder, config);
            }

            if (env.IsProduction())
            {
                InsertProductionData(builder, config);
            }
        }
    }
}