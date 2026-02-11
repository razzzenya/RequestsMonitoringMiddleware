namespace RequestMonitoringLibrary.Middleware.Services.FileReader;

public interface IFileReaderService
{
    Task<List<string>> ReadFileLinesAsync(string filePath);
}
