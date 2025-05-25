using System;
using FileAnalysisService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FileAnalysisService.Data;

public class AnalysisDbContext : DbContext
{
	public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : base(options)
	{
	}

    public DbSet<AnalysisDto> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AnalysisDto>()
            .HasKey(r => r.FileId);
    }
}

