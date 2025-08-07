using AlbumApp.Models;

namespace AlbumApp.Services;

public interface IMetadataService
{
    /// <summary>
    /// ファイルのメタデータから撮影日を抽出する
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="contentType">ファイルのContent-Type</param>
    /// <returns>撮影日。取得できない場合はnull</returns>
    Task<DateTime?> ExtractDateTakenAsync(string filePath, string contentType);
    
    /// <summary>
    /// ファイルから基本的なメタデータ情報を抽出する
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="contentType">ファイルのContent-Type</param>
    /// <returns>メタデータ情報</returns>
    Task<MediaMetadata> ExtractMetadataAsync(string filePath, string contentType);
}

/// <summary>
/// メディアファイルのメタデータ情報
/// </summary>
public class MediaMetadata
{
    public DateTime? DateTaken { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? CameraModel { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}