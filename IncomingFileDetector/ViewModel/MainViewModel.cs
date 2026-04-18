using System.Collections.ObjectModel;
using System.Windows.Data;
using IncomingFileDetector.Service;
using IncomingFileDetector.Service.Model;
using Microsoft.Extensions.Logging;

namespace IncomingFileDetector.ViewModel;

/// <summary>
/// Представляет ViewModel для главного окна приложения.
/// </summary>
internal sealed class MainViewModel : IMainViewModel
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly FileDetectorService _fileDetectorService;
    private readonly object _syncRoot = new();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MainViewModel"/>.
    /// </summary>
    /// <param name="logger">Экземпляр <see cref="ILogger{MainViewModel}"/> для логирования действий в приложении.</param>
    /// <param name="fileDetectorService">Экземпляр <see cref="FileDetectorService"/> для регистрации входящих файлов.</param>
    public MainViewModel(ILogger<MainViewModel> logger, FileDetectorService fileDetectorService)
    {
        _logger = logger;
        _fileDetectorService = fileDetectorService;
        
        _fileDetectorService.FileRegistered += FileDetectorServiceOnFileRegistered;
        BindingOperations.EnableCollectionSynchronization(RegisteredFiles, _syncRoot);
    }

    /// <summary>
    /// Обрабатывает событие регистрации нового входящего файла.
    /// </summary>
    /// <param name="sender">Источник события, который инициировал вызов.</param>
    /// <param name="e">Аргументы события <see cref="FileRegisteredEventArgs"/>, содержащие информацию о зарегистрированном файле.</param>
    private void FileDetectorServiceOnFileRegistered(object? sender, FileRegisteredEventArgs e)
    {
        RegisteredFiles.Add(e.RegisteredFile);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Added a new registered file to the collection: {FileName}", e.RegisteredFile.FileName);
    }

    /// <inheritdoc />
    public ObservableCollection<IncomingFile> RegisteredFiles { get; } = [];

    /// <inheritdoc />
    public string ObservedDirectory
    {
        get => _fileDetectorService.ObservedDirectory.FullName;
        set => _fileDetectorService.UpdateObservedDirectory(value);
    }

    /// <inheritdoc />
    public long PollingTimeout
    {
        get => (long) (_fileDetectorService.PollingTimeout / 1000d);
        set => _fileDetectorService.UpdatePollingTimeout(value * 1000);
    }

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        _fileDetectorService.FileRegistered -= FileDetectorServiceOnFileRegistered;
    }

    #endregion
}