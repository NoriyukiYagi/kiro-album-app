namespace AlbumApp.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _baseDirectory;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseDirectory = _configuration["FileStorage:PictureDirectory"] ?? "/data/pict";
        
        // ベースディレクトリが存在しない場合は作成
        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
            _logger.LogInformation("Created base directory: {BaseDirectory}", _baseDirectory);
        }
    }

    public async Task<string> SaveFileAsync(string sourceFilePath, string fileName, DateTime? dateTaken = null)
    {
        try
        {
            // 撮影日が指定されていない場合は現在日時を使用
            var targetDate = dateTaken ?? DateTime.Now;
            
            // 日付ベースのディレクトリパスを生成
            var dateBasedPath = GenerateDateBasedPath(targetDate);
            var targetDirectory = Path.Combine(_baseDirectory, dateBasedPath);
            
            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
                _logger.LogInformation("Created directory: {Directory}", targetDirectory);
            }

            // ファイル名の重複を避けるため、必要に応じて番号を付加
            var targetFilePath = Path.Combine(targetDirectory, fileName);
            var finalFileName = await GetUniqueFileNameAsync(targetDirectory, fileName);
            var finalFilePath = Path.Combine(targetDirectory, finalFileName);

            // ファイルをコピー
            await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
            await using var targetStream = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(targetStream);

            // 相対パスを返す
            var relativePath = Path.Combine(dateBasedPath, finalFileName);
            _logger.LogInformation("File saved successfully: {RelativePath}", relativePath);
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {SourceFilePath} -> {FileName}", sourceFilePath, fileName);
            throw;
        }
    }

    public Task<Stream?> GetFileAsync(string relativePath)
    {
        try
        {
            var fullPath = GetFullPath(relativePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {FullPath}", fullPath);
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file: {RelativePath}", relativePath);
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task<bool> DeleteFileAsync(string relativePath)
    {
        try
        {
            var fullPath = GetFullPath(relativePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for deletion: {FullPath}", fullPath);
                return Task.FromResult(false);
            }

            File.Delete(fullPath);
            _logger.LogInformation("File deleted successfully: {RelativePath}", relativePath);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {RelativePath}", relativePath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string relativePath)
    {
        try
        {
            var fullPath = GetFullPath(relativePath);
            return Task.FromResult(File.Exists(fullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence: {RelativePath}", relativePath);
            return Task.FromResult(false);
        }
    }

    public string GenerateDateBasedPath(DateTime date)
    {
        return date.ToString("yyyyMMdd");
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_baseDirectory, relativePath);
    }

    private Task<string> GetUniqueFileNameAsync(string directory, string fileName)
    {
        var originalFileName = fileName;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var counter = 1;

        while (File.Exists(Path.Combine(directory, fileName)))
        {
            fileName = $"{fileNameWithoutExtension}_{counter}{extension}";
            counter++;
        }

        if (fileName != originalFileName)
        {
            _logger.LogInformation("File name changed to avoid conflict: {Original} -> {New}", originalFileName, fileName);
        }

        return Task.FromResult(fileName);
    }
}