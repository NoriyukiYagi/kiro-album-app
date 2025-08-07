namespace AlbumApp.Services;

public interface IThumbnailService
{
    /// <summary>
    /// 画像ファイルからサムネイルを生成する
    /// </summary>
    /// <param name="sourceFilePath">元画像ファイルのパス</param>
    /// <param name="fileName">サムネイルファイル名</param>
    /// <param name="dateTaken">撮影日（nullの場合はアップロード日を使用）</param>
    /// <returns>生成されたサムネイルの相対パス</returns>
    Task<string> GenerateImageThumbnailAsync(string sourceFilePath, string fileName, DateTime? dateTaken = null);

    /// <summary>
    /// 動画ファイルの最初のフレームからサムネイルを生成する
    /// </summary>
    /// <param name="sourceFilePath">元動画ファイルのパス</param>
    /// <param name="fileName">サムネイルファイル名</param>
    /// <param name="dateTaken">撮影日（nullの場合はアップロード日を使用）</param>
    /// <returns>生成されたサムネイルの相対パス</returns>
    Task<string> GenerateVideoThumbnailAsync(string sourceFilePath, string fileName, DateTime? dateTaken = null);

    /// <summary>
    /// サムネイルを取得する
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <returns>サムネイルファイルストリーム</returns>
    Task<Stream?> GetThumbnailAsync(string relativePath);

    /// <summary>
    /// サムネイルを削除する
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <returns>削除が成功したかどうか</returns>
    Task<bool> DeleteThumbnailAsync(string relativePath);

    /// <summary>
    /// サムネイルが存在するかチェックする
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <returns>サムネイルが存在するかどうか</returns>
    Task<bool> ThumbnailExistsAsync(string relativePath);

    /// <summary>
    /// 日付ベースのサムネイルディレクトリパスを生成する
    /// </summary>
    /// <param name="date">日付</param>
    /// <returns>ディレクトリパス（YYYYMMDD形式）</returns>
    string GenerateDateBasedPath(DateTime date);
}