using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using TrainingVideoAPI.Models;

[ApiController]
[Route("api/videos")]
public class VideosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public VideosController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _blobServiceClient = new BlobServiceClient(config["AzureBlob:ConnectionString"]);
        _containerName = config["AzureBlob:ContainerName"];
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadVideoDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
            return BadRequest("Invalid file.");

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();

        var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
        var blobClient = containerClient.GetBlobClient(fileName);

        using var stream = dto.File.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        var video = new TrainingVideo
        {
            Title = dto.Title,
            Description = dto.Description,
            BlobFileName = fileName
        };

        _context.TrainingVideos.Add(video);
        await _context.SaveChangesAsync();

        return Ok(video);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var videos = await _context.TrainingVideos.ToListAsync();
        return Ok(videos);
    }


    [HttpGet("stream/{id}")]
    public async Task<IActionResult> Stream(int id)
    {
        var video = await _context.TrainingVideos.FindAsync(id);
        if (video == null)
            return NotFound();

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(video.BlobFileName); // stored as filename

        if (!await blobClient.ExistsAsync())
            return NotFound("Video not found in blob storage.");

        var stream = await blobClient.OpenReadAsync();
        var contentType = "video/mp4";

        Response.Headers.Add("Content-Disposition", "inline; filename=" + video.BlobFileName);
        return File(stream, contentType, enableRangeProcessing: true);


    }



    [HttpPost("chunk-upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadChunk([FromForm] IFormFile chunk,
                                             [FromForm] string fileId,
                                             [FromForm] string fileName,
                                             [FromForm] int chunkIndex,
                                             [FromForm] int totalChunks,
                                             [FromForm] string title,
                                             [FromForm] string description)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "video-chunks", fileId);
        Directory.CreateDirectory(tempDir);

        var chunkPath = Path.Combine(tempDir, $"{chunkIndex}.part");
        using (var stream = new FileStream(chunkPath, FileMode.Create))
        {
            await chunk.CopyToAsync(stream);
        }

        var uploadedChunks = Directory.GetFiles(tempDir).Length;
        if (uploadedChunks < totalChunks)
        {
            return Ok(new { message = $"Chunk {chunkIndex + 1}/{totalChunks} uploaded" });
        }


        var finalFileName = Guid.NewGuid() + Path.GetExtension(fileName);
        var mergedPath = Path.Combine(tempDir, finalFileName);

        using (var fs = new FileStream(mergedPath, FileMode.Create))
        {
            for (int i = 0; i < totalChunks; i++)
            {
                var partPath = Path.Combine(tempDir, $"{i}.part");
                var bytes = await System.IO.File.ReadAllBytesAsync(partPath);
                await fs.WriteAsync(bytes);
            }
        }

      
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();
        var blobClient = containerClient.GetBlobClient(finalFileName);

        using (var uploadStream = System.IO.File.OpenRead(mergedPath))
        {
            await blobClient.UploadAsync(uploadStream, overwrite: true);
        }

      
        var video = new TrainingVideo
        {
            Title = title,
            Description = description,
            BlobFileName = finalFileName
        };

        _context.TrainingVideos.Add(video);
        await _context.SaveChangesAsync();

    
        Directory.Delete(tempDir, true);

        return Ok(new { message = "Upload complete", video });
    }

}
