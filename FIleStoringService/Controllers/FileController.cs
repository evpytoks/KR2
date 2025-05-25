using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FIleStoringService.Data;
using FIleStoringService.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.IO.Pipes;
using FIleStoringService.DTOs;
using System.Security.Cryptography.X509Certificates;

namespace FIleStoringService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    FileDbContext FileDbContext_;
    MakeHashService MakeHashService_;
    IConfiguration Config_;


    public FileController(FileDbContext context, MakeHashService service, IConfiguration config)
	{
        FileDbContext_ = context;
        MakeHashService_ = service;
        Config_ = config;
	}


    /// <summary>
    /// Uploads a .txt file, checks for duplicates by content hash, saves the file in the database.
    /// </summary>
    /// <param name="file">The .txt file to be uploaded.</param>
    /// <returns>Uploaded file id.</returns>
    /// <response code="200">File was successfully uploaded. Returns the analysis result.</response>
    /// <response code="400">The file to upload is empty, not a .txt, or there is the same file in db.</response>
    /// <response code="500">An internal server error occurred while analysing the file.</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            if (file.Length == 0)
            {
                return BadRequest("File can't be empty.");
            }

            if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("File can't be not txt.");
            }

            using var fileContentStream = new MemoryStream();
            await file.CopyToAsync(fileContentStream);
            var contentHash = await MakeHashService_.GetHashAsync(fileContentStream);

            var isFileIn = await FileDbContext_.Files.FirstOrDefaultAsync(f => f.Hash == contentHash);
            if (isFileIn != null)
            {
                return BadRequest("There can't be two files with the same info.");
            }

            var path = Config_.GetValue<string>("FilePath") ?? "/app/files";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            var filePath = Path.GetFullPath(Path.Combine(path, $"{contentHash}{extension}"));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                fileContentStream.Position = 0;
                await fileContentStream.CopyToAsync(stream);
            }

            var info = new FileDto
            {
                Id = Guid.NewGuid(),
                Hash = contentHash,
                Name = file.FileName,
                Location = $"{contentHash}{extension}",
                Type = file.ContentType,
                FileSize = file.Length
            };

            FileDbContext_.Files.Add(info);
            await FileDbContext_.SaveChangesAsync();

            return Ok(new UploadResultDto{ FileId = info.Id });
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Can't upload file: {exception.Message}");
        }
    }


    /// <summary>
    /// Gets uploaded .txt file by its id.
    /// </summary>
    /// <param name="fileId">The id of the file to get.</param>
    /// <returns>The requested .txt file as a stream.</returns>
    /// <response code="200">The file was found and returned successfully.</response>
    /// <response code="404">No file was found for the given id or it is missing in storage.</response>
    [HttpGet("get")]
    [ProducesResponseType(typeof(File), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile([FromQuery] Guid fileId)
    {
        var file = await FileDbContext_.Files.FindAsync(fileId);
        if (file == null)
        {
            return NotFound();
        }

        try
        {
            var path = Path.Combine(Config_.GetValue<string>("FilePath") ?? "/app/files", file.Location);

            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException($"Can't find file in {path}.");
            }

            var stream = new MemoryStream();
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                await fileStream.CopyToAsync(stream);
            }

            stream.Position = 0;
            return File(stream, "text/plain", file.Name);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
