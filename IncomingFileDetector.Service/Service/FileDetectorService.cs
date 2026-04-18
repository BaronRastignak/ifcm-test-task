using IncomingFileDetector.Service.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IncomingFileDetector.Service;

/// <summary>
/// Служба для обнаружения входящих файлов в указанной директории.
/// </summary>
/// <remarks>
/// Класс реализует функциональность для наблюдения за директорией, регистрации новых файлов
/// и уведомления о зарегистрированных файлах через событие <see cref="FileRegistered"/>.
/// </remarks>
/// <param name="logger">Экземпляр <see cref="ILogger{FileDetectorService}"/> для логирования информации и ошибок.</param>
/// <param name="options">Экземпляр <see cref="IOptions{FileDetectorOptions}"/> для получения настроек службы.</param>
/// <param name="fileRegistrator">Экземпляр <see cref="IFileRegistrator"/> для регистрации обнаруженных файлов.</param>
public sealed class FileDetectorService(
    ILogger<FileDetectorService> logger,
    IOptions<FileDetectorOptions> options,
    IFileRegistrator fileRegistrator) : IHostedService, IAsyncDisposable
{
    private Timer? _timer;

    /// <summary>
    /// Возвращает или задаёт директорию, которая отслеживается для обнаружения входящих файлов.
    /// </summary>
    public DirectoryInfo ObservedDirectory
    {
        get => Volatile.Read(ref field);
        private set => Volatile.Write(ref field, value);
    } = new(options.Value.ObservedDirectory);

    /// <summary>
    /// Получает или задает тайм-аут проверки каталога в миллисекундах.
    /// </summary>
    public long PollingTimeout { get; private set; } = options.Value.PollingTimeout;

    /// <summary>
    /// Событие, возникающее при успешной регистрации нового файла в наблюдаемой директории.
    /// </summary>
    /// <remarks>
    /// Это событие вызывается, когда файл успешно обнаружен и зарегистрирован с помощью 
    /// <see cref="IFileRegistrator"/>. Подписчики могут использовать это событие для выполнения 
    /// дополнительных действий, связанных с обработкой зарегистрированных файлов.
    /// </remarks>
    /// <example>
    /// Пример подписки на событие:
    /// <code>
    /// var service = new FileDetectorService(logger, options, fileRegistrator);
    /// service.FileRegistered += (sender, args) =>
    /// {
    ///     Console.WriteLine($"Файл зарегистрирован: {args.RegisteredFile.FullPath}");
    /// };
    /// </code>
    /// </example>
    /// <seealso cref="FileRegisteredEventArgs"/>
    public event EventHandler<FileRegisteredEventArgs>? FileRegistered;

    /// <summary>
    /// Обновляет наблюдаемую директорию для обнаружения входящих файлов.
    /// </summary>
    /// <param name="observedDirectory">
    /// Новый путь к директории, которая будет наблюдаться.
    /// </param>
    public void UpdateObservedDirectory(string observedDirectory)
    {
        ObservedDirectory = new DirectoryInfo(observedDirectory);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Observed directory updated to: {ObservedDirectory}", ObservedDirectory.FullName);
    }
    
    /// <summary>
    /// Обновляет таймаут опроса для службы <see cref="FileDetectorService"/>.
    /// </summary>
    /// <param name="pollingTimeout">
    /// Новый таймаут опроса в миллисекундах.
    /// </param>
    /// <remarks>
    /// Если таймер уже запущен, метод немедленно применяет новый таймаут,
    /// перезапуская таймер с указанным значением. Это позволяет ускорить
    /// или замедлить частоту опроса в зависимости от новых требований.
    /// </remarks>
    public void UpdatePollingTimeout(long pollingTimeout)
    {
        PollingTimeout = pollingTimeout;
        // Обновляем таймер, если он уже запущен.
        _timer?.Change(0, PollingTimeout);
        
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Polling timeout updated to: {PollingTimeout} ms", PollingTimeout);
    }

    /// <summary>
    /// Регистрирует входящие файлы из наблюдаемой директории.
    /// </summary>
    /// <param name="state">
    /// Состояние, переданное в таймер. Не используется.
    /// </param>
    private void RegisterIncomingFiles(object? state)
    {
        try
        {
            foreach (var file in ObservedDirectory.EnumerateFiles())
            {
                try
                {
                    // Пропускаем файлы, которые были зарегистрированы ранее.
                    if (!fileRegistrator.RegisterFile(file, out var registeredFile))
                        continue;

                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("File registered: {RegisteredFile}", registeredFile);

                    OnFileRegistered(registeredFile);
                }
                catch (IOException ex)
                {
                    logger.LogError(ex, "Error accessing file {FilePath}", file.FullName);
                }
            }
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError(ex, "Observed directory was not found or is no longer available: {ObservedDirectory}", ObservedDirectory.FullName);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Access to observed directory was denied: {ObservedDirectory}", ObservedDirectory.FullName);
        }
    }

    /// <summary>
    /// Инициализирует начальное состояние, регистрируя все файлы, уже существующие
    /// в наблюдаемой директории.
    /// </summary>
    private void PopulateInitialState()
    {
        try
        {
            var filesCount = 0;
            foreach (var file in ObservedDirectory.EnumerateFiles())
            {
                filesCount++;
                
                try
                {
                    fileRegistrator.RegisterFile(file, out _);
                }
                catch (IOException ex)
                {
                    logger.LogError(ex, "Error accessing file {FilePath}", file.FullName);
                }
            }
            
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Initial number of files in the directory: {InitialCount}", filesCount);
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError(ex, "Observed directory was not found or is no longer available: {ObservedDirectory}", ObservedDirectory.FullName);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Access to observed directory was denied: {ObservedDirectory}", ObservedDirectory.FullName);
        }
    }
    
    /// <summary>
    /// Вызывает событие <see cref="FileRegistered"/> для уведомления о зарегистрированном файле.
    /// </summary>
    /// <param name="registeredFile">Объект <see cref="IncomingFile"/>, представляющий зарегистрированный файл.</param>
    private void OnFileRegistered(IncomingFile registeredFile)
    {
        FileRegistered?.Invoke(this, new FileRegisteredEventArgs(registeredFile));
    }

    #region Implementation of IHostedService

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{ServiceName} is starting.", nameof(FileDetectorService));
        PopulateInitialState();
        _timer = new Timer(RegisterIncomingFiles, null, 0, PollingTimeout);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{ServiceName} is stopping.", nameof(FileDetectorService));
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Асинхронно освобождает ресурсы, используемые службой <see cref="FileDetectorService"/>.
    /// </summary>
    private async ValueTask DisposeAsyncCore()
    {
        if (_timer is not null)
        {
            await _timer.DisposeAsync();
            _timer = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}