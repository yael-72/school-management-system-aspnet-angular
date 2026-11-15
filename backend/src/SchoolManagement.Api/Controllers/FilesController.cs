using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SchoolManagement.Application.Files;

namespace SchoolManagement.Api.Controllers;

/// <summary>The logged-in user's private files (profile page).</summary>
[ApiController]
[Route("api/files")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class FilesController(IFileService fileService) : ControllerBase
{
    /// <summary>Requests bigger than this are cut off by the server before the service sees them.</summary>
    private const long MaxRequestBytes = FileRules.MaxFileSizeBytes + 1024 * 1024;

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    /// <summary>The user's files, newest first, and her storage usage.</summary>
    [HttpGet]
    [ProducesResponseType<FilesOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FilesOverviewDto>> GetFiles(CancellationToken cancellationToken)
    {
        return Ok(await fileService.GetFilesAsync(cancellationToken));
    }

    /// <summary>Uploads one file (multipart/form-data, field "file"). Documents and images, up to 5 MB.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<UploadedFileDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<UploadedFileDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var uploaded = await fileService.UploadAsync(new FileUpload(file.FileName, file.Length, content), cancellationToken);
        return CreatedAtAction(nameof(Download), new { id = uploaded.Id }, uploaded);
    }

    /// <summary>Downloads a file. Always sent as an attachment, so the browser never runs it as a page.</summary>
    [HttpGet("{id:int}/download")]
    [Produces("application/octet-stream")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var download = await fileService.DownloadAsync(id, cancellationToken);

        if (!ContentTypes.TryGetContentType(download.FileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        // File(...) disposes the stream when the response is done.
        return File(download.Content, contentType, download.FileName);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await fileService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
