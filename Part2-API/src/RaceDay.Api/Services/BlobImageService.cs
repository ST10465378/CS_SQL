using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.Api.Services;

public class BlobImageService(IConfiguration config)
{
    private BlobContainerClient GetContainer(string name)
    {
        var connectionString = config["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage has not been configured.");
        return new BlobContainerClient(connectionString, name);
    }

    public async Task<string> Upload(IFormFile file, string container, CancellationToken ct)
    {
        if (file == null || file.Length <= 0 || file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("Image must be between 1 byte and 5 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var expected = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
        if (expected == null || !string.Equals(file.ContentType, expected, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only JPG, PNG and WEBP files are supported.");

        await using (var stream = file.OpenReadStream())
        {
            var header = new byte[12];
            var count = 0;
            while (count < header.Length)
            {
                var n = await stream.ReadAsync(header.AsMemory(count, header.Length - count), ct);
                if (n == 0) break;
                count += n;
            }
            var valid = expected switch
            {
                "image/jpeg" => count >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
                "image/png" => count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
                "image/webp" => count >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
                _ => false
            };
            if (!valid) throw new ArgumentException("The file contents do not match the selected image type.");
        }

        var client = GetContainer(container);
        await client.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        var blob = client.GetBlobClient($"{Guid.NewGuid():N}{ext}");
        await using var input = file.OpenReadStream();
        await blob.UploadAsync(input, new BlobHttpHeaders { ContentType = expected }, cancellationToken: ct);
        // Persist only the permanent URL. Do not save expiring SAS tokens in SQL Server.
        return blob.Uri.ToString();
    }

    // Only call this for images the current API endpoint is permitted to reveal.
    public string? GetReadUrl(string? savedUrl, string container)
    {
        if (string.IsNullOrWhiteSpace(savedUrl)) return savedUrl;
        var client = GetContainer(container);
        if (!Uri.TryCreate(savedUrl, UriKind.Absolute, out var uri)) return null;

        // Restrict signing to the expected container in OUR storage account.
        var containerPrefix = client.Uri.AbsolutePath.TrimEnd('/') + "/";
        if (!string.Equals(uri.Scheme, client.Uri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Authority, client.Uri.Authority, StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith(containerPrefix, StringComparison.Ordinal)) return null;

        var blobName = Uri.UnescapeDataString(uri.AbsolutePath[containerPrefix.Length..]);
        if (string.IsNullOrWhiteSpace(blobName) || blobName.Contains('/') || blobName.Contains('\\')) return null;

        var blob = client.GetBlobClient(blobName);
        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException("Storage credentials cannot generate a read SAS URL.");

        var sas = new BlobSasBuilder
        {
            BlobContainerName = client.Name,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30),
            Protocol = SasProtocol.Https
        };
        sas.SetPermissions(BlobSasPermissions.Read);
        return blob.GenerateSasUri(sas).ToString();
    }
}
