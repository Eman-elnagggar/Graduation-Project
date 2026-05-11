using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Graduation_Project.Services
{
    public class UltrasoundImageStorage
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        private readonly string _originalRoot;
        private readonly string _resultsRoot;

        public UltrasoundImageStorage(IWebHostEnvironment env)
        {
            _originalRoot = Path.Combine(env.WebRootPath, "uploads", "ultrasound", "original");
            _resultsRoot = Path.Combine(env.WebRootPath, "uploads", "ultrasound", "results");
        }

        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(_originalRoot);
            Directory.CreateDirectory(_resultsRoot);
        }

        public bool IsValid(IFormFile file, out string errorMessage)
        {
            errorMessage = null;

            if (file == null || file.Length == 0)
            {
                errorMessage = "Please select an image file.";
                return false;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                errorMessage = "File size exceeds the 10 MB limit.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                errorMessage = "Only JPG and PNG images are allowed.";
                return false;
            }

            return true;
        }

        public async Task<string> SaveOriginalAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            EnsureDirectoriesExist();

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(_originalRoot, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            return $"/uploads/ultrasound/original/{fileName}";
        }

        public async Task<string> SaveResultAsync(byte[] imageBytes, string extension = ".png", CancellationToken cancellationToken = default)
        {
            EnsureDirectoriesExist();

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".png";
            }

            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(_resultsRoot, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await stream.WriteAsync(imageBytes, 0, imageBytes.Length, cancellationToken);

            return $"/uploads/ultrasound/results/{fileName}";
        }
    }
}
