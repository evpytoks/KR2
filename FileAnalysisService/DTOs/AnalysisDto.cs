using System;
namespace FileAnalysisService.DTOs;

public class AnalysisDto
{
    public Guid FileId { get; set; }
    public int Paragraphs { get; set; }
    public int Words { get; set; }
    public int Symbols { get; set; }
}
