using System;
using FileAnalysisService.DTOs;
using Microsoft.EntityFrameworkCore;
using FileAnalysisService.Data;
using System.Collections.Generic;

namespace FileAnalysisService.Services;

public class ResultStoringService
{
    private readonly AnalysisDbContext AnalysisDbContext_;


    public ResultStoringService(AnalysisDbContext dbContext)
    {
        AnalysisDbContext_ = dbContext;
    }


    public async Task<AnalysisDto> GetResultAsync(Guid fileId)
    {
        var results = await AnalysisDbContext_.Results
            .FirstOrDefaultAsync(a => a.FileId == fileId);

        if (results == null)
        {
            throw new KeyNotFoundException($"File analysis result with FileId '{fileId}' was not found.");
        }

        return results;
    }

    public async Task UploadResult(AnalysisDto result)
    {
        AnalysisDbContext_.Add(result);
        await AnalysisDbContext_.SaveChangesAsync();
    }
}
