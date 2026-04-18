namespace IncomingFileDetector.Service.Model;

/// <summary>
/// Представляет входящий файл с метаданными, такими как полный путь, время регистрации, 
/// время создания, время изменения и размер. Предоставляет дополнительные свойства 
/// для получения имени файла и MIME-типа.
/// </summary>
/// <param name="FullPath">Полный путь к файлу.</param>
/// <param name="RegisteredAt">Время регистрации входящего файла.</param>
/// <param name="CreatedAt">Время создания файла.</param>
/// <param name="ModifiedAt">Время последнего изменения файла.</param>
/// <param name="Length">Размер файла в байтах.</param>
public record IncomingFile(
    string FullPath,
    DateTimeOffset RegisteredAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    long Length)
{
    /// <summary>
    /// Возвращает имя файла, включая его расширение, из полного пути.
    /// </summary>
    /// <value>
    /// Строка (<see cref="string"/>), представляющая имя файла, извлечённое из свойства <see cref="FullPath"/>.
    /// </value>
    public string FileName => Path.GetFileName(FullPath);
    
    /// <summary>
    /// Возвращает MIME-тип файла на основе его расширения.
    /// </summary>
    /// <value>
    /// Строка (<see cref="string"/>), представляющая MIME-тип файла.
    /// </value>
    public string MimeType => MimeTypes.GetMimeType(FullPath);
}