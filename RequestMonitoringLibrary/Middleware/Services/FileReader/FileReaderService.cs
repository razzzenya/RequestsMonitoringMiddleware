using System.Text.Json;

namespace RequestMonitoringLibrary.Middleware.Services.FileReader;

public class FileReaderService : IFileReaderService
{
    public async Task<List<string>> ReadFileLinesAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        List<string>? domains = JsonSerializer.Deserialize<List<string>>(json);
        return domains ?? [];
    }
}
