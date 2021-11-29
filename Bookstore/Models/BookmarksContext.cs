using System;
using System.Linq;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Bookstore.Models
{
    // Database context for managing bookmarks
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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            builder.Entity<Bookmark>().Property(t => t.FolderString).UsePropertyAccessMode(PropertyAccessMode.Property);
            builder.Entity<Bookmark>().Property(t => t.TagString).UsePropertyAccessMode(PropertyAccessMode.Property);
            builder.Entity<Bookmark>().Property(t => t.UrlString).UsePropertyAccessMode(PropertyAccessMode.Property);
            builder.Entity<Bookmark>().Property(t => t.DomainString).UsePropertyAccessMode(PropertyAccessMode.Property);
        }
    }
}