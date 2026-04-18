using System.Collections.Concurrent;
using IncomingFileDetector.Service.Model;
using Microsoft.Extensions.Logging;
using Moq;

namespace IncomingFileDetector.Service.UnitTests;

[TestClass]
public class FileDetectorServiceTests
{
    /// <summary>
    /// Проверяет, что метод <see cref="IFileRegistrator.RegisterFile"/> вызывается как минимум один раз
    /// после запуска службы <see cref="FileDetectorService"/> при наличии файла в наблюдаемой директории.
    /// </summary>
    /// <remarks>
    /// Метод создает временную директорию с файлом, инициализирует службу с указанным регистратором,
    /// запускает службу и ожидает вызова метода <see cref="IFileRegistrator.RegisterFile"/>.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Выбрасывается, если метод <see cref="IFileRegistrator.RegisterFile"/> не был вызван.
    /// </exception>
    [TestMethod]
    public async Task StartAsync_FileExists_InvokesRegistrator()
    {
        var directory = CreateTempDirectoryWithFile("start.txt");
        var registrator = new RecordingRegistrator(_ => false);
        var service = CreateService(directory, pollingTimeout: 50, registrator);

        try
        {
            await service.StartAsync(CancellationToken.None);

            var called = await WaitUntilAsync(() => registrator.CallCount >= 1, TimeSpan.FromSeconds(2));
            Assert.IsTrue(called, "Expected RegisterFile to be called at least once after service start.");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
            DeleteDirectoryIfExists(directory);
        }
    }

    /// <summary>
    /// Проверяет, генерируется ли событие <see cref="FileDetectorService.FileRegistered"/> 
    /// при успешной регистрации файла с помощью <see cref="IFileRegistrator"/>.
    /// </summary>
    /// <remarks>
    /// Этот тест создает временный каталог с файлом, настраивает <see cref="FileDetectorService"/> 
    /// с регистратором, который всегда возвращает <see langword="true"/>, и проверяет, что событие 
    /// <see cref="FileDetectorService.FileRegistered"/> вызывается с ожидаемыми аргументами.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Выбрасывается, если событие <see cref="FileDetectorService.FileRegistered"/> не вызывается 
    /// или если аргументы события не соответствуют ожидаемым.
    /// </exception>
    [TestMethod]
    public async Task FileRegistered_WhenRegistratorReturnsTrue_EventIsRaised()
    {
        var directory = CreateTempDirectoryWithFile("event.txt");
        var registrator = new RecordingRegistrator(_ => true);
        var service = CreateService(directory, pollingTimeout: 10_000, registrator);

        var eventTask = new TaskCompletionSource<FileRegisteredEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.FileRegistered += (_, args) => eventTask.TrySetResult(args);

        try
        {
            await service.StartAsync(CancellationToken.None);

            var completed = await Task.WhenAny(eventTask.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.AreSame(eventTask.Task, completed, "Expected FileRegistered event to be raised.");

            var args = await eventTask.Task;
            Assert.AreEqual(Path.Combine(directory, "event.txt"), args.RegisteredFile.FullPath);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
            DeleteDirectoryIfExists(directory);
        }
    }

    /// <summary>
    /// Проверяет, что метод <see cref="FileDetectorService.UpdateObservedDirectory"/> корректно обновляет наблюдаемую директорию
    /// после запуска службы <see cref="FileDetectorService"/> и инициирует сканирование новой директории.
    /// </summary>
    /// <remarks>
    /// Метод создает две временные директории с файлами, запускает службу с начальной директорией,
    /// обновляет наблюдаемую директорию на новую и проверяет, что файлы из новой директории были просканированы.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Выбрасывается, если файлы из начальной или обновленной директории не были просканированы.
    /// </exception>
    [TestMethod]
    public async Task UpdateObservedDirectory_AfterStart_ScansNewDirectory()
    {
        var initialDirectory = CreateTempDirectoryWithFile("initial.txt");
        var updatedDirectory = CreateTempDirectoryWithFile("updated.txt");
        var registrator = new RecordingRegistrator(_ => false);
        var service = CreateService(initialDirectory, pollingTimeout: 10_000, registrator);

        try
        {
            await service.StartAsync(CancellationToken.None);
            var initialScanObserved = await WaitUntilAsync(
                () => registrator.Paths.Contains(Path.Combine(initialDirectory, "initial.txt"), StringComparer.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(2));
            Assert.IsTrue(initialScanObserved, "Expected initial directory file to be scanned.");

            service.UpdateObservedDirectory(updatedDirectory);
            // Уменьшаем тайм-аут, чтобы ускорить сканирование новой директории.
            service.UpdatePollingTimeout(20);

            var updatedScanObserved = await WaitUntilAsync(
                () => registrator.Paths.Contains(Path.Combine(updatedDirectory, "updated.txt"), StringComparer.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(2));
            Assert.IsTrue(updatedScanObserved, "Expected updated directory file to be scanned after UpdateObservedDirectory.");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
            DeleteDirectoryIfExists(initialDirectory);
            DeleteDirectoryIfExists(updatedDirectory);
        }
    }

    /// <summary>
    /// Проверяет, что изменение таймаута опроса с помощью метода <see cref="FileDetectorService.UpdatePollingTimeout"/>
    /// применяется немедленно, если служба <see cref="FileDetectorService"/> уже запущена.
    /// </summary>
    /// <remarks>
    /// Метод инициализирует службу с заданным таймаутом опроса, запускает её, затем изменяет таймаут
    /// и проверяет, что новый таймаут применяется немедленно, инициируя следующий опрос.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Выбрасывается, если следующий опрос не происходит в ожидаемое время после изменения таймаута.
    /// </exception>
    [TestMethod]
    public async Task UpdatePollingTimeout_WhenRunning_AppliesImmediately()
    {
        var directory = CreateTempDirectoryWithFile("timeout.txt");
        var registrator = new RecordingRegistrator(_ => false);
        var service = CreateService(directory, pollingTimeout: 60_000, registrator);

        try
        {
            await service.StartAsync(CancellationToken.None);

            var firstCallObserved = await WaitUntilAsync(() => registrator.CallCount >= 1, TimeSpan.FromSeconds(2));
            Assert.IsTrue(firstCallObserved, "Expected first scan after start.");

            var callsBeforeUpdate = registrator.CallCount;
            service.UpdatePollingTimeout(20);

            var secondCallObserved = await WaitUntilAsync(
                () => registrator.CallCount >= callsBeforeUpdate + 1,
                TimeSpan.FromSeconds(1));
            Assert.IsTrue(secondCallObserved, "Expected an immediate scan after UpdatePollingTimeout.");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
            DeleteDirectoryIfExists(directory);
        }
    }

    /// <summary>
    /// Проверяет, что метод <see cref="FileDetectorService.StopAsync"/> корректно завершает выполнение службы,
    /// предотвращая дальнейшее сканирование наблюдаемой директории.
    /// </summary>
    /// <remarks>
    /// Метод запускает службу <see cref="FileDetectorService"/>, ожидает нескольких вызовов метода регистрации,
    /// затем вызывает <see cref="FileDetectorService.StopAsync"/> и проверяет, что после остановки службы
    /// дальнейшие вызовы метода регистрации не происходят.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Выбрасывается, если после вызова <see cref="FileDetectorService.StopAsync"/> продолжаются вызовы метода регистрации.
    /// </exception>
    [TestMethod]
    public async Task StopAsync_AfterRunning_StopsFurtherPolling()
    {
        var directory = CreateTempDirectoryWithFile("stop.txt");
        var registrator = new RecordingRegistrator(_ => false);
        var service = CreateService(directory, pollingTimeout: 20, registrator);

        try
        {
            await service.StartAsync(CancellationToken.None);

            var multipleCallsObserved = await WaitUntilAsync(() => registrator.CallCount >= 2, TimeSpan.FromSeconds(2));
            Assert.IsTrue(multipleCallsObserved, "Expected multiple scans before stop.");

            await service.StopAsync(CancellationToken.None);
            await Task.Delay(50);
            var callsAfterStop = registrator.CallCount;
            await Task.Delay(150);

            Assert.AreEqual(callsAfterStop, registrator.CallCount, "Expected no additional scans after stop.");
        }
        finally
        {
            await service.DisposeAsync();
            DeleteDirectoryIfExists(directory);
        }
    }

    /// <summary>
    /// Проверяет, что метод <see cref="FileDetectorService.DisposeAsync"/> может быть вызван дважды
    /// и при этом останавливает процесс опроса.
    /// </summary>
    /// <remarks>
    /// Метод создает временную директорию с файлом, инициализирует службу с указанным регистратором,
    /// запускает службу, проверяет, что опрос выполняется, вызывает метод <see cref="FileDetectorService.DisposeAsync"/> дважды
    /// и убеждается, что опрос прекращается после первого вызова.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Выбрасывается, если опрос продолжается после вызова метода <see cref="FileDetectorService.DisposeAsync"/>.
    /// </exception>
    [TestMethod]
    public async Task DisposeAsync_CanBeCalledTwice_AndStopsPolling()
    {
        var directory = CreateTempDirectoryWithFile("dispose.txt");
        var registrator = new RecordingRegistrator(_ => false);
        var service = CreateService(directory, pollingTimeout: 20, registrator);

        try
        {
            await service.StartAsync(CancellationToken.None);

            var callsObserved = await WaitUntilAsync(() => registrator.CallCount >= 2, TimeSpan.FromSeconds(2));
            Assert.IsTrue(callsObserved, "Expected service to poll before disposal.");

            await service.DisposeAsync();
            var callsAfterDispose = registrator.CallCount;
            await Task.Delay(150);
            Assert.AreEqual(callsAfterDispose, registrator.CallCount, "Expected no polling after DisposeAsync.");

            await service.DisposeAsync();
        }
        finally
        {
            DeleteDirectoryIfExists(directory);
        }
    }

    /// <summary>
    /// Создает экземпляр <see cref="FileDetectorService"/> с указанными параметрами.
    /// </summary>
    /// <param name="observedDirectory">
    /// Директория, которая будет наблюдаться на наличие новых файлов.
    /// </param>
    /// <param name="pollingTimeout">
    /// Таймаут опроса в миллисекундах, определяющий частоту проверки директории.
    /// </param>
    /// <param name="registrator">
    /// Реализация интерфейса <see cref="IFileRegistrator"/>, используемая для регистрации обнаруженных файлов.
    /// </param>
    /// <returns>
    /// Экземпляр <see cref="FileDetectorService"/>, настроенный для наблюдения за указанной директорией.
    /// </returns>
    private static FileDetectorService CreateService(string observedDirectory, long pollingTimeout, IFileRegistrator registrator)
    {
        var logger = new Mock<ILogger<FileDetectorService>>().Object;
        var options = new FileDetectorOptions
        {
            ObservedDirectory = observedDirectory,
            PollingTimeout = pollingTimeout
        };
        return new FileDetectorService(logger, options, registrator);
    }

    /// <summary>
    /// Создает временную директорию и файл с указанным именем внутри неё.
    /// </summary>
    /// <param name="fileName">Имя файла, который будет создан во временной директории.</param>
    /// <returns>Путь к созданной временной директории.</returns>
    /// <remarks>
    /// Директория создается в системной временной папке с уникальным именем. 
    /// Файл внутри директории содержит тестовое содержимое.
    /// </remarks>
    private static string CreateTempDirectoryWithFile(string fileName)
    {
        var directory = Path.Combine(Path.GetTempPath(), "IncomingFileDetectorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), "test-content");
        return directory;
    }

    /// <summary>
    /// Удаляет указанную директорию, если она существует.
    /// </summary>
    /// <param name="directory">Путь к директории, которую нужно удалить.</param>
    private static void DeleteDirectoryIfExists(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    /// <summary>
    /// Асинхронно ожидает выполнения указанного условия в течение заданного тайм-аута.
    /// </summary>
    /// <param name="condition">
    /// Функция, представляющая условие, которое должно быть выполнено.
    /// </param>
    /// <param name="timeout">
    /// Максимальное время ожидания выполнения условия.
    /// </param>
    /// <returns>
    /// Возвращает <see langword="true"/>, если условие выполнено в течение тайм-аута; 
    /// в противном случае возвращает <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Метод периодически проверяет выполнение условия с интервалом в 10 миллисекунд.
    /// Если условие не выполнено до истечения тайм-аута, возвращается его текущее состояние.
    /// </remarks>
    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < timeout)
        {
            if (condition())
                return true;

            await Task.Delay(10);
        }

        return condition();
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RecordingRegistrator"/> с указанной функцией, которая задаёт условия регистрации файла.
    /// </summary>
    /// <param name="shouldRegister">
    /// Функция, определяющая, следует ли регистрировать переданный файл. Возвращает <see langword="true"/>, если файл должен быть зарегистрирован;
    /// в противном случае - <see langword="false"/>.
    /// </param>
    private sealed class RecordingRegistrator(Func<FileInfo, bool> shouldRegister) : IFileRegistrator
    {
        private int _callCount;

        /// <summary>
        /// Возвращает количество вызовов метода <see cref="RegisterFile"/> для данного экземпляра.
        /// </summary>
        /// <value>
        /// Целое число, представляющее количество вызовов метода <see cref="RegisterFile"/>.
        /// </value>
        public int CallCount => Volatile.Read(ref _callCount);

        /// <summary>
        /// Возвращает очередь путей к файлам, которые были зарегистрированы.
        /// </summary>
        public ConcurrentQueue<string> Paths { get; } = new();

        /// <summary>
        /// Регистрирует указанный файл и возвращает информацию о зарегистрированном файле.
        /// </summary>
        /// <param name="fileInfo">
        /// Информация о файле, который необходимо зарегистрировать.
        /// </param>
        /// <param name="registeredFile">
        /// Выходной параметр, содержащий объект <see cref="IncomingFile"/> с информацией о зарегистрированном файле.
        /// </param>
        /// <returns>
        /// Значение <see langword="true"/>, если файл был успешно зарегистрирован; 
        /// в противном случае — <see langword="false"/>.
        /// </returns>
        public bool RegisterFile(FileInfo fileInfo, out IncomingFile registeredFile)
        {
            Interlocked.Increment(ref _callCount);
            Paths.Enqueue(fileInfo.FullName);

            registeredFile = new IncomingFile(
                fileInfo.FullName,
                DateTimeOffset.UtcNow,
                fileInfo.CreationTimeUtc,
                fileInfo.LastWriteTimeUtc,
                fileInfo.Length);

            return shouldRegister(fileInfo);
        }
    }
}
