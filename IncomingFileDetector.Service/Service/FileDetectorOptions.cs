using Microsoft.Extensions.Options;

namespace IncomingFileDetector.Service;

/// <summary>
/// Представляет параметры конфигурации для обнаружения входящих файлов.
/// </summary>
public sealed class FileDetectorOptions : IOptions<FileDetectorOptions>
{
    /// <summary>
    /// Возвращает каталог, в котором будет отслеживаться появление входящих файлов.
    /// </summary>
    public required string ObservedDirectory { get; init; }

    /// <summary>
    /// Возвращает значение тайм-аута проверки каталога в миллисекундах.
    /// </summary>
    public required long PollingTimeout { get; init; }

    #region Implementation of IOptions<out FileDetectorOptions>

    /// <inheritdoc />
    public FileDetectorOptions Value => this;

    #endregion
}