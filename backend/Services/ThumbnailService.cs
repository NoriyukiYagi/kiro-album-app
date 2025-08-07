using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using FFMpegCore;
using FFMpegCore.Enums;

namespace AlbumApp.Services;

public class ThumbnailService : IThumbnailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ThumbnailService> _logger;
    private readonly string _thumbnailDirectory;
    private const int MaxThumbnailSize = 300;

    public ThumbnailService(IConfiguration configuration, ILogger<ThumbnailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _thumbnailDirectory = _configuration["FileStorage:ThumbnailDirectory"] ?? "/data/thumb";
        
        // サムネイルディレクトリが存在しない場合は作成
        if (!Directory.Exists(_thumbnailDirectory))
        {
            Directory.CreateDirectory(_thumbnailDirectory);
            _logger.LogInformation("Created thumbnail directory: {ThumbnailDirectory}", _thumbnailDirectory);
        }
    }

    public async Task<string> GenerateImageThumbnailAsync(string sourceFilePath, string fileName, DateTime? dateTaken = null)
    {
        try
        {
            // 撮影日が指定されていない場合は現在日時を使用
            var targetDate = dateTaken ?? DateTime.Now;
            
            // 日付ベースのディレクトリパスを生成
            var dateBasedPath = GenerateDateBasedPath(targetDate);
            var targetDirectory = Path.Combine(_thumbnailDirectory, dateBasedPath);
            
            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
                _logger.LogInformation("Created thumbnail directory: {Directory}", targetDirectory);
            }

            // サムネイルファイル名を生成（拡張子をjpgに変更）
            var thumbnailFileName = Path.ChangeExtension(fileName, ".jpg");
            var thumbnailFilePath = Path.Combine(targetDirectory, thumbnailFileName);
            
            // ファイル名の重複を避けるため、必要に応じて番号を付加
            thumbnailFileName = await GetUniqueFileNameAsync(targetDirectory, thumbnailFileName);
            thumbnailFilePath = Path.Combine(targetDirectory, thumbnailFileName);

            // ImageSharpを使用してサムネイルを生成
            using var image = await Image.LoadAsync(sourceFilePath);
            
            // アスペクト比を保持しながら300ピクセル以下にリサイズ
            var resizeOptions = new ResizeOptions
            {
                Size = new Size(MaxThumbnailSize, MaxThumbnailSize),
                Mode = ResizeMode.Max
            };
            
            image.Mutate(x => x.Resize(resizeOptions));
            
            // JPEGとして保存
            await image.SaveAsJpegAsync(thumbnailFilePath);

            // 相対パスを返す
            var relativePath = Path.Combine(dateBasedPath, thumbnailFileName);
            _logger.LogInformation("Image thumbnail generated successfully: {RelativePath}", relativePath);
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image thumbnail: {SourceFilePath} -> {FileName}", sourceFilePath, fileName);
            throw;
        }
    }

    public async Task<string> GenerateVideoThumbnailAsync(string sourceFilePath, string fileName, DateTime? dateTaken = null)
    {
        try
        {
            // 撮影日が指定されていない場合は現在日時を使用
            var targetDate = dateTaken ?? DateTime.Now;
            
            // 日付ベースのディレクトリパスを生成
            var dateBasedPath = GenerateDateBasedPath(targetDate);
            var targetDirectory = Path.Combine(_thumbnailDirectory, dateBasedPath);
            
            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
                _logger.LogInformation("Created thumbnail directory: {Directory}", targetDirectory);
            }

            // サムネイルファイル名を生成（拡張子をjpgに変更）
            var thumbnailFileName = Path.ChangeExtension(fileName, ".jpg");
            var thumbnailFilePath = Path.Combine(targetDirectory, thumbnailFileName);
            
            // ファイル名の重複を避けるため、必要に応じて番号を付加
            thumbnailFileName = await GetUniqueFileNameAsync(targetDirectory, thumbnailFileName);
            thumbnailFilePath = Path.Combine(targetDirectory, thumbnailFileName);

            // FFMpegCoreを使用して動画の最初のフレームからサムネイルを生成
            await FFMpegArguments
                .FromFileInput(sourceFilePath)
                .OutputToFile(thumbnailFilePath, true, options => options
                    .WithFrameOutputCount(1)
                    .Resize(MaxThumbnailSize, MaxThumbnailSize)
                    .WithCustomArgument("-q:v 2")) // 高品質JPEG
                .ProcessAsynchronously();

            // 相対パスを返す
            var relativePath = Path.Combine(dateBasedPath, thumbnailFileName);
            _logger.LogInformation("Video thumbnail generated successfully: {RelativePath}", relativePath);
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate video thumbnail: {SourceFilePath} -> {FileName}", sourceFilePath, fileName);
            throw;
        }
    }

    public Task<Stream?> GetThumbnailAsync(string relativePath)
    {
        try
        {
            var fullPath = GetFullPath(relativePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Thumbnail not found: {FullPath}", fullPath);
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thumbnail: {RelativePath}", relativePath);
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task<bool> DeleteThumbnailAsync(string relativePath)
    {
        try
        {
            var fullPath = GetFullPath(relativePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Thumbnail not found for deletion: {FullPath}", fullPath);
                return Task.FromResult(false);
            }

            File.Delete(fullPath);
            _logger.LogInformation("Thumbnail deleted successfully: {RelativePath}", relativePath);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete thumbnail: {RelativePath}", relativePath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ThumbnailExistsAsync(string relativePath)
    {
        try
        {
            var fullPath = GetFullPath(relativePath);
            return Task.FromResult(File.Exists(fullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check thumbnail existence: {RelativePath}", relativePath);
            return Task.FromResult(false);
        }
    }

    public string GenerateDateBasedPath(DateTime date)
    {
        return date.ToString("yyyyMMdd");
    }

    private string GetFullPath(string relativePath)
    {
        return Path.Combine(_thumbnailDirectory, relativePath);
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
            _logger.LogInformation("Thumbnail file name changed to avoid conflict: {Original} -> {New}", originalFileName, fileName);
        }

        return Task.FromResult(fileName);
    }
}