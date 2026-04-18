using Microsoft.Extensions.Options;

namespace IncomingFileDetector.Service;

/// <summary>
/// Представляет параметры конфигурации для обнаружения входящих файлов.
/// </summary>
/// <param name="observedDirectory">Каталог, в котором будет отслеживаться появление входящих файлов.</param>
/// <param name="pollingTimeout">Тайм-аут проверки каталога в миллисекундах.</param>

public sealed class FileDetectorOptions(string observedDirectory, long pollingTimeout) : IOptions<FileDetectorOptions>
{
    /// <summary>
    /// Возвращает каталог, в котором будет отслеживаться появление входящих файлов.
    /// </summary>
    public string ObservedDirectory { get; init; } = observedDirectory;

    /// <summary>
    /// Возвращает значение тайм-аута проверки каталога в миллисекундах.
    /// </summary>
    public long PollingTimeout { get; init; } = pollingTimeout;

    #region Implementation of IOptions<out FileDetectorOptions>

    /// <inheritdoc />
    public FileDetectorOptions Value => this;

    #endregion
}