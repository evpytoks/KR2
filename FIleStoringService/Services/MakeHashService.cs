using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FIleStoringService.Services;

public class MakeHashService
{
    public async Task<string> GetHashAsync(Stream stream)
    {
        stream.Position = 0;
        using var sha256 = SHA256.Create();
        var bytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToBase64String(bytes)
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "");
    }
}
