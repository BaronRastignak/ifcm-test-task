using IncomingFileDetector.Service.Model;

namespace IncomingFileDetector.Service;

/// <summary>
/// Реализует функциональность для регистрации входящих файлов и форматирования их метаданных.
/// </summary>
public sealed class FileRegistrator : IFileRegistrator
{
    private readonly HashSet<string> _registeredFiles = [];
    
    #region Implementation of IFileRegistrator

    /// <inheritdoc />
    public bool RegisterFile(FileInfo fileInfo, out IncomingFile registeredFile)
    {
        registeredFile = new IncomingFile(
            fileInfo.FullName,
            DateTimeOffset.UtcNow,
            fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc,
            fileInfo.Length
        );
        return _registeredFiles.Add(fileInfo.FullName);
    }

    #endregion
}