using IncomingFileDetector.Service.Model;

namespace IncomingFileDetector.Service;

/// <summary>
/// Предоставляет данные события, возникающего при регистрации нового входящего файла.
/// </summary>
public sealed class FileRegisteredEventArgs(IncomingFile registeredFile) : EventArgs
{
    /// <summary>
    /// Возвращает зарегистрированный входящий файл, связанный с событием.
    /// </summary>
    /// <value>
    /// Экземпляр <see cref="IncomingFile"/>, представляющий зарегистрированный файл.
    /// </value>
    public IncomingFile RegisteredFile { get; } = registeredFile;
}