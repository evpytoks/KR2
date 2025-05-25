using System;
namespace FIleStoringService.DTOs;

public class FileDto
{
    public Guid Id { get; set; }
    public string Hash { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public string Type { get; set; } = "text/plain";
    public long FileSize { get; set; }
}
