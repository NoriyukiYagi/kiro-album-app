namespace AlbumApp.Services;

public interface IFileStorageService
{
    /// <summary>
    /// ファイルを日付ベースのディレクトリ構造で保存する
    /// </summary>
    /// <param name="sourceFilePath">元ファイルのパス</param>
    /// <param name="fileName">保存するファイル名</param>
    /// <param name="dateTaken">撮影日（nullの場合はアップロード日を使用）</param>
    /// <returns>保存されたファイルの相対パス</returns>
    Task<string> SaveFileAsync(string sourceFilePath, string fileName, DateTime? dateTaken = null);

    /// <summary>
    /// ファイルを取得する
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <returns>ファイルストリーム</returns>
    Task<Stream?> GetFileAsync(string relativePath);

    /// <summary>
    /// ファイルを削除する
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <returns>削除が成功したかどうか</returns>
    Task<bool> DeleteFileAsync(string relativePath);

    /// <summary>
    /// ファイルが存在するかチェックする
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <returns>ファイルが存在するかどうか</returns>
    Task<bool> FileExistsAsync(string relativePath);

    /// <summary>
    /// 日付ベースのディレクトリパスを生成する
    /// </summary>
    /// <param name="date">日付</param>
    /// <returns>ディレクトリパス（YYYYMMDD形式）</returns>
    string GenerateDateBasedPath(DateTime date);

    /// <summary>
    /// ファイルの完全パスを取得する
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <returns>完全パス</returns>
    string GetFullPath(string relativePath);
}