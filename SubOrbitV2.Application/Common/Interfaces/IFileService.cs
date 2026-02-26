using Microsoft.AspNetCore.Http;

namespace SubOrbitV2.Application.Common.Interfaces;

public interface IFileService
{
    Task<string> UploadFileAsync(IFormFile file, string folderName);
    Task DeleteFileAsync(string filePath);
}