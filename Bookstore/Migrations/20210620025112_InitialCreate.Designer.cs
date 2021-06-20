﻿// <auto-generated />
using System;
using Bookstore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bookstore.Migrations
{
    [DbContext(typeof(BookmarksContext))]
    [Migration("20210620025112_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.5");

            modelBuilder.Entity("BookmarkTag", b =>
                {
                    b.Property<ulong>("BookmarksId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("TagsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("BookmarksId", "TagsId");

                    b.HasIndex("TagsId");

                    b.ToTable("BookmarkTag");
                });

            modelBuilder.Entity("Bookstore.Models.Archive", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<string>("PlainText")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Archive");
                });

            modelBuilder.Entity("Bookstore.Models.Bookmark", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("ArchiveId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Favicon")
                        .HasColumnType("BLOB");

                    b.Property<ulong?>("FolderId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Modified")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ArchiveId");

                    b.HasIndex("FolderId");

                    b.HasIndex("UserId");

                    b.ToTable("Bookmarks");
                });

            modelBuilder.Entity("Bookstore.Models.Folder", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("Folders");

                    b.HasData(
                        new
                        {
                            Id = 1ul,
                            Name = "Bun",
                            UserId = 1ul
                        },
                        new
                        {
                            Id = 2ul,
                            Name = "Cheese",
                            ParentId = 1ul,
                            UserId = 1ul
                        },
                        new
                        {
                            Id = 3ul,
                            Name = "Meat",
                            ParentId = 2ul,
                            UserId = 1ul
                        });
                });

            modelBuilder.Entity("Bookstore.Models.Settings", b =>
                {
                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ArchiveByDefault")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("DefaultPaginationLimit")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DefaultQuery")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("Bookstore.Models.Tag", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("Bookstore.Models.User", b =>
                {
                    b.Property<ulong>("Id")
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
                            Id = 1ul,
                            Admin = false,
                            PasswordHash = "PkkqREI8rB4VNwDJ4AQBKx+JIHRu8lk2QX2QwAbbEcU=",
                            PasswordSalt = "jlh9Hxo4Ebr82D6pEX322g==",
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