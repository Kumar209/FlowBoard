using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using File.Service.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace File.Service.Infrastructure.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var cloudName = config["Cloudinary:CloudName"] ?? config["Cloudinary__CloudName"] ?? "demo";
        var apiKey = config["Cloudinary:ApiKey"] ?? config["Cloudinary__ApiKey"] ?? "";
        var apiSecret = config["Cloudinary:ApiSecret"] ?? config["Cloudinary__ApiSecret"] ?? "";
        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<(string Url, string PublicId)> UploadAsync(string fileName, Stream fileStream, string contentType, string folder, CancellationToken ct = default)
    {
        var ctLower = (contentType ?? "").ToLowerInvariant();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        // Image + PDF as image (public, previewable via image viewer) — PDF needs image resource to be viewable/downloadable without 401
        // Video as video, others (doc/xls/zip/txt) as raw (public)
        bool isPdf = ctLower == "application/pdf" || ext == ".pdf";
        if (ctLower.StartsWith("image/") || isPdf)
        {
            var imgParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
                EagerTransforms = isPdf ? new List<Transformation>() : new List<Transformation> { new Transformation().Width(300).Crop("scale").FetchFormat("auto").Quality("auto") }
            };
            var imgRes = await _cloudinary.UploadAsync(imgParams);
            if (imgRes.Error != null) throw new InvalidOperationException($"Cloudinary upload failed: {imgRes.Error.Message}");
            return (imgRes.SecureUrl?.ToString() ?? imgRes.Url?.ToString() ?? "", imgRes.PublicId);
        }
        else if (ctLower.StartsWith("video/"))
        {
            var vidParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };
            var vidRes = await _cloudinary.UploadAsync(vidParams);
            if (vidRes.Error != null) throw new InvalidOperationException($"Cloudinary upload failed: {vidRes.Error.Message}");
            return (vidRes.SecureUrl?.ToString() ?? vidRes.Url?.ToString() ?? "", vidRes.PublicId);
        }
        else
        {
            var rawParams = new RawUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };
            var rawRes = await _cloudinary.UploadAsync(rawParams);
            if (rawRes.Error != null) throw new InvalidOperationException($"Cloudinary upload failed: {rawRes.Error.Message}");
            return (rawRes.SecureUrl?.ToString() ?? rawRes.Url?.ToString() ?? "", rawRes.PublicId);
        }
    }

    public async Task DeleteAsync(string publicId, CancellationToken ct = default)
    {
        // Try Image, then Raw, then Video — publicId alone doesn't encode resource type
        foreach (var rt in new[] { ResourceType.Image, ResourceType.Raw, ResourceType.Video, ResourceType.Auto })
        {
            var delParams = new DeletionParams(publicId) { ResourceType = rt };
            var result = await _cloudinary.DestroyAsync(delParams);
            if (result.Error == null || result.Result == "not found" || result.Result == "ok") return;
            // If error is "resource not found" try next type
            if (result.Error != null && result.Error.Message.Contains("not found")) continue;
            throw new InvalidOperationException($"Cloudinary delete failed: {result.Error.Message}");
        }
    }
}
