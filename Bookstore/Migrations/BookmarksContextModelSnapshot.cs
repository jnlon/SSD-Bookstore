﻿// <auto-generated />
using System;
using Bookstore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bookstore.Migrations
{
    [DbContext(typeof(BookmarksContext))]
    partial class BookmarksContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.5");

            modelBuilder.Entity("BookmarkTag", b =>
                {
                    b.Property<long>("BookmarksId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("TagsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("BookmarksId", "TagsId");

                    b.HasIndex("TagsId");

                    b.ToTable("BookmarkTag");
                });

            modelBuilder.Entity("Bookstore.Models.Archive", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Bytes")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Formatted")
                        .HasColumnType("BLOB");

                    b.Property<string>("Mime")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PlainText")
                        .HasColumnType("TEXT");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Archives");
                });

            modelBuilder.Entity("Bookstore.Models.Bookmark", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long?>("ArchiveId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Favicon")
                        .HasColumnType("BLOB");

                    b.Property<string>("FaviconMime")
                        .HasColumnType("TEXT");

                    b.Property<long?>("FolderId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Modified")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ArchiveId");

                    b.HasIndex("FolderId");

                    b.HasIndex("UserId");

                    b.ToTable("Bookmarks");
                });

            modelBuilder.Entity("Bookstore.Models.Folder", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long?>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("Folders");
                });

            modelBuilder.Entity("Bookstore.Models.Settings", b =>
                {
                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ArchiveByDefault")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DefaultPaginationLimit")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DefaultQuery")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("Bookstore.Models.Tag", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("Bookstore.Models.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Admin")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            Admin = true,
                            PasswordHash = "3JVdPcaDGDmMkGgEs9sTxZeXz4bYBItuD5AaffFQWMM=",
                            PasswordSalt = "evNkbyJyGkzS8TbMtYtGaA==",
                            Username = "admin"
                        },
                        new
                        {
                            Id = 2L,
                            Admin = false,
                            PasswordHash = "FBvEEGGvbpBxSHLCtWdNn+UiZz6wIf4p+XeEUU9v1lI=",
                            PasswordSalt = "9vqnI/oT4vm5ZlER7Tn2dg==",
                            Username = "toast"
                        });
                });

            modelBuilder.Entity("BookmarkTag", b =>
                {
                    b.HasOne("Bookstore.Models.Bookmark", null)
                        .WithMany()
                        .HasForeignKey("BookmarksId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Bookstore.Models.Tag", null)
                        .WithMany()
                        .HasForeignKey("TagsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Bookstore.Models.Bookmark", b =>
                {
                    b.HasOne("Bookstore.Models.Archive", "Archive")
                        .WithMany()
                        .HasForeignKey("ArchiveId");

                    b.HasOne("Bookstore.Models.Folder", "Folder")
                        .WithMany("Bookmarks")
                        .HasForeignKey("FolderId");

                    b.HasOne("Bookstore.Models.User", "User")
                        .WithMany("Bookmarks")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Archive");

                    b.Navigation("Folder");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Bookstore.Models.Folder", b =>
                {
                    b.HasOne("Bookstore.Models.Folder", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentId");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("Bookstore.Models.Settings", b =>
                {
                    b.HasOne("Bookstore.Models.User", null)
                        .WithOne("Settings")
                        .HasForeignKey("Bookstore.Models.Settings", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Bookstore.Models.Folder", b =>
                {
                    b.Navigation("Bookmarks");
                });

            modelBuilder.Entity("Bookstore.Models.User", b =>
                {
                    b.Navigation("Bookmarks");

                    b.Navigation("Settings")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
