using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using API_Gateway.DTOs;

namespace API_Gateway.Controllers;


[ApiController]
[Route("api/[controller]")]
public class PlagiarismDetectorController : ControllerBase
{
    private readonly IConfiguration Сonfig_;
    private readonly HttpClient HttpClient_;


    public PlagiarismDetectorController(IConfiguration config, HttpClient client)
	{
        Сonfig_ = config;
        HttpClient_ = client;
    }


    /// <summary>
    /// Upload file to FileStoringService.
    /// </summary>
    /// <param name="file">The file to be uploaded.</param>
    /// <returns>
    /// Returns an <see cref="AnswerDto"/> containing information about the uploaded file if successful.
    /// </returns>
    /// <response code="200">File was successfully uploaded and a response was received from the file storage service.</response>
    /// <response code="400">The uploaded file is empty.</response>
    /// <response code="500">An internal error occurred during file upload or response processing.</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(AnswerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("Can't upload empty file.");
        }

        try
        {
            using var stream = file.OpenReadStream();
            long length = file.Length;
            var bytes = new byte[length];
            await stream.ReadExactlyAsync(bytes, 0, (int)length);

            var info = new ByteArrayContent(bytes);
            info.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            using var uploadData = new MultipartFormDataContent();
            uploadData.Add(info, "file", file.FileName);

            var fileStoringServiceUrl = Сonfig_["FileStoringServiceUrl"];
            var answer = await HttpClient_.PostAsync($"{fileStoringServiceUrl}/api/File/upload", uploadData);
            var body = await answer.Content.ReadAsStringAsync();

            if (!answer.IsSuccessStatusCode)
            {
                return StatusCode((int)answer.StatusCode, $"Can't upload file: {body}");
            }

            var result = await answer.Content.ReadFromJsonAsync<AnswerDto>();
            if (result is null)
            {
                return StatusCode(500, "Can't deserialize upload result.");
            }

            return Ok(result);
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Can't upload file: {exception.Message}.");
        }
    }


    /// <summary>
    /// Sends a request to the File Analysis Service to analyse file.
    /// </summary>
    /// <param name="fileId">The id of the file to analyse.</param>
    /// <returns>The result of the file analysis.</returns>
    /// <response code="200">File was successfully analysed. Returns the analysis result.</response>
    /// <response code="404">The file to analyze was not found.</response>
    /// <response code="500">An internal server error occurred while analysing the file.</response>
    [HttpPost("analyse")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Analyse(Guid fileId)
    {
        var fileAnalysisServiceUrl = Сonfig_["FileAnalysisServiceUrl"];
        var answer = await HttpClient_.GetAsync($"{fileAnalysisServiceUrl}/api/Analyse/proceed?fileId={fileId}");

        if (!answer.IsSuccessStatusCode)
        {
            return StatusCode((int)answer.StatusCode, "Can't analyse file.");
        }

        var result = await answer.Content.ReadFromJsonAsync<object>();
        return Ok(result);
    }


    /// <summary>
    /// Gets file from the FileStoringService by Id.
    /// </summary>
    /// <param name="fileId">The id of the file to get.</param>
    /// <returns>The requested file as a stream with its original name and content type.</returns>
    /// <response code="200">Returns the requested file.</response>
    /// <response code="404">File not found.</response>
    /// <response code="500">An internal error occurred while getting the file.</response>
    [HttpGet("get")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(Guid fileId)
    {
        var fileStoringServiceUrl = Сonfig_["FileStoringServiceUrl"];
        var answer = await HttpClient_.GetAsync($"{fileStoringServiceUrl}/api/File/get?fileId={fileId}");

        if (!answer.IsSuccessStatusCode)
        {
            return StatusCode((int)answer.StatusCode, "Can't get file.");
        }

        var stream = await answer.Content.ReadAsStreamAsync();
        var name = answer.Content.Headers.ContentDisposition?.FileName ?? $"{fileId}.txt";
        var type = answer.Content.Headers.ContentType?.ToString() ?? "text/plain";

        return File(stream, type, name);
    }
}
