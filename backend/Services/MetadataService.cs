using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using SixLabors.ImageSharp;
using FFMpegCore;
using AlbumApp.Models;

namespace AlbumApp.Services;

public class MetadataService : IMetadataService
{
    private readonly ILogger<MetadataService> _logger;

    public MetadataService(ILogger<MetadataService> logger)
    {
        _logger = logger;
    }

    public async Task<DateTime?> ExtractDateTakenAsync(string filePath, string contentType)
    {
        try
        {
            var metadata = await ExtractMetadataAsync(filePath, contentType);
            return metadata.DateTaken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract date taken from file: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<MediaMetadata> ExtractMetadataAsync(string filePath, string contentType)
    {
        var metadata = new MediaMetadata();

        try
        {
            if (IsImageFile(contentType))
            {
                await ExtractImageMetadataAsync(filePath, metadata);
            }
            else if (IsVideoFile(contentType))
            {
                await ExtractVideoMetadataAsync(filePath, metadata);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract metadata from file: {FilePath}", filePath);
        }

        return metadata;
    }

    private async Task ExtractImageMetadataAsync(string filePath, MediaMetadata metadata)
    {
        // MetadataExtractorを使用してEXIFデータを抽出
        var directories = ImageMetadataReader.ReadMetadata(filePath);

        // 撮影日の抽出
        ExtractDateTaken(directories, metadata);

        // カメラ情報の抽出
        ExtractCameraInfo(directories, metadata);

        // GPS情報の抽出
        ExtractGpsInfo(directories, metadata);

        // 画像サイズの抽出
        await ExtractImageDimensionsAsync(filePath, metadata);
    }

    private async Task ExtractVideoMetadataAsync(string filePath, MediaMetadata metadata)
    {
        try
        {
            // FFMpegCoreを使用して動画メタデータを抽出
            var mediaInfo = await FFProbe.AnalyseAsync(filePath);
            
            if (mediaInfo.PrimaryVideoStream != null)
            {
                metadata.Width = mediaInfo.PrimaryVideoStream.Width;
                metadata.Height = mediaInfo.PrimaryVideoStream.Height;
                metadata.Duration = mediaInfo.Duration;
            }

            // 作成日時の抽出（動画ファイルの場合）
            if (mediaInfo.Format.Tags?.ContainsKey("creation_time") == true)
            {
                if (DateTime.TryParse(mediaInfo.Format.Tags["creation_time"], out var creationTime))
                {
                    metadata.DateTaken = creationTime;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract video metadata from file: {FilePath}", filePath);
        }
    }

    private void ExtractDateTaken(IEnumerable<MetadataExtractor.Directory> directories, MediaMetadata metadata)
    {
        // EXIF SubIFD ディレクトリから撮影日時を取得
        var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        if (subIfdDirectory?.HasTagName(ExifDirectoryBase.TagDateTimeOriginal) == true)
        {
            if (subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTime))
            {
                metadata.DateTaken = dateTime;
                return;
            }
        }

        // EXIF IFD0 ディレクトリから日時を取得
        var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        if (ifd0Directory?.HasTagName(ExifDirectoryBase.TagDateTime) == true)
        {
            if (ifd0Directory.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime))
            {
                metadata.DateTaken = dateTime;
                return;
            }
        }

        // PNGの場合、tEXtチャンクから日時を取得
        var pngDirectory = directories.OfType<PngDirectory>().FirstOrDefault();
        if (pngDirectory != null)
        {
            var textualData = pngDirectory.GetPngChunkType()?.ToString();
            // PNG固有の日時抽出ロジックがあれば実装
        }
    }

    private void ExtractCameraInfo(IEnumerable<MetadataExtractor.Directory> directories, MediaMetadata metadata)
    {
        var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        if (ifd0Directory?.HasTagName(ExifDirectoryBase.TagMake) == true && 
            ifd0Directory?.HasTagName(ExifDirectoryBase.TagModel) == true)
        {
            var make = ifd0Directory.GetString(ExifDirectoryBase.TagMake);
            var model = ifd0Directory.GetString(ExifDirectoryBase.TagModel);
            metadata.CameraModel = $"{make} {model}".Trim();
        }
    }

    private void ExtractGpsInfo(IEnumerable<MetadataExtractor.Directory> directories, MediaMetadata metadata)
    {
        var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
        if (gpsDirectory != null)
        {
            var location = gpsDirectory.GetGeoLocation();
            if (location != null)
            {
                metadata.Latitude = location.Latitude;
                metadata.Longitude = location.Longitude;
            }
        }
    }

    private async Task ExtractImageDimensionsAsync(string filePath, MediaMetadata metadata)
    {
        try
        {
            using var image = await Image.LoadAsync(filePath);
            metadata.Width = image.Width;
            metadata.Height = image.Height;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract image dimensions from file: {FilePath}", filePath);
        }
    }

    private static bool IsImageFile(string contentType)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVideoFile(string contentType)
    {
        return contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }
}