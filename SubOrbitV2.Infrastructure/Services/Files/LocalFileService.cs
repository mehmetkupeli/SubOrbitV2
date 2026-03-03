using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SubOrbitV2.Application.Common.Interfaces;

namespace SubOrbitV2.Infrastructure.Services.Files;

public class LocalFileService : IFileService
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0) return string.Empty;

        // 1. Klasör Yolunu Hazırla
        var uploadPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", folderName);
        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

        // 2. Dosya İsimlendirme
        var extension = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, fileName);

        // 3. Dosyayı Önce Belleğe Al (Daha güvenli ve hızlı analiz için)
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0; // Başa dönmeyi unutma!

        // 4. Resim İşleme Kontrolü
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (imageExtensions.Contains(extension))
        {
            try
            {
                // Artık ImageSharp'a hazır ve temiz bir memoryStream veriyoruz
                using var image = await Image.LoadAsync(memoryStream);

                if (image.Width > 800)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(800, 0), Mode = ResizeMode.Max }));
                }

                await image.SaveAsync(filePath);
            }
            catch (UnknownImageFormatException)
            {
                // Eğer ImageSharp "bu ne abi?" derse, zorlamıyoruz; olduğu gibi kaydediyoruz (veya hata fırlatıyoruz)
                // Şimdilik güvenlik için olduğu gibi kaydedelim:
                memoryStream.Position = 0;
                using var fileStream = new FileStream(filePath, FileMode.Create);
                await memoryStream.CopyToAsync(fileStream);
            }
        }
        else
        {
            // Resim dışındaki (PDF vb.) dosyalar için direkt kayıt
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await memoryStream.CopyToAsync(fileStream);
        }

        return $"/uploads/{folderName}/{fileName}";
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        var fullPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, filePath.TrimStart('/'));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public async Task<string> SaveFileAsync(byte[] content, string fileName, string folderName, string payerId)
    {
        if (content == null || content.Length == 0)
            return string.Empty;

        var uploadPath = Path.Combine(
            _environment.WebRootPath ?? _environment.ContentRootPath,
            "uploads",
            folderName,               
            payerId                   
        );

        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, fileName);

        await File.WriteAllBytesAsync(filePath, content);

        // Web'den erişim için dönen yol (URL)
        return $"/uploads/{folderName}/{payerId}/{fileName}";
    }
}