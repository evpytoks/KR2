using System;
using FileAnalysisService.Data;
using Microsoft.AspNetCore.Mvc;
using FileAnalysisService.Services;
using FileAnalysisService.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace FileAnalysisService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyseController : ControllerBase
{
	AnalysisService AnalysisService_;
	ResultStoringService ResultStoringService_;
    private readonly IConfiguration Сonfig_;
    private readonly HttpClient HttpClient_;

    public AnalyseController(AnalysisService analysisService, ResultStoringService resultStoringService, HttpClient client, IConfiguration config)
    {
        AnalysisService_ = analysisService;
        ResultStoringService_ = resultStoringService;
        Сonfig_ = config;
        HttpClient_ = client;
    }


    /// <summary>
    /// Tries to get an existing file analysis result by FileId using File Storing Service.
    /// If not found, performs analysis, stores the result, and returns it.
    /// </summary>
    /// <param name="fileId">The id of the file to analyse.</param>
    /// <returns>The analysis result if successful.</returns>
    /// <response code="200">Returns the analysis result when found or successfully generated.</response>
    /// <response code="404">Returns a message string if the file with the given FileId was not found in the File Storing Service.</response>
    /// <response code="500">Returns an error message if an internal server error occurs.</response>
    [HttpGet("proceed")]
    [ProducesResponseType(typeof(AnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Analyse([FromQuery] Guid fileId)
    {
        try
        {
            var result = await ResultStoringService_.GetResultAsync(fileId);
            return Ok(result);
        }
        catch (Exception)
        {
            try
            {
                var fileStoringServiceUrl = Сonfig_.GetValue<string>("FileStoringServiceUrl") ?? "http://file-storing-service";
                var answer = await HttpClient_.GetAsync($"{fileStoringServiceUrl}/api/File/get?fileId={fileId}");
                var body = await answer.Content.ReadAsStringAsync();

                if (!answer.IsSuccessStatusCode)
                {
                    return StatusCode((int)answer.StatusCode, $"Can't get file: {body}");
                }

                var result = AnalysisService_.Analyse(fileId, body);
                await ResultStoringService_.UploadResult(result);
                return Ok(result);
            }
            catch (Exception exception)
            {
                return StatusCode(500, $"Can't analyse file: {exception.Message}.");
            }
        }
    }
}
