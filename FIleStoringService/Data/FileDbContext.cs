using System;
using Microsoft.EntityFrameworkCore;
using FIleStoringService.DTOs;

namespace FIleStoringService.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options)
    {
    }

    public DbSet<FileDto> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {

        builder.Entity<FileDto>()
            .HasKey(f => f.Id);

        builder.Entity<FileDto>()
            .HasIndex(f => f.Hash)
            .IsUnique();
    }
}
