using IncomingFileDetector.Service.Model;

namespace IncomingFileDetector.Service.UnitTests;

[TestClass]
public class FileRegistratorTests
{
    /// <summary>
    /// Проверяет, что метод <see cref="FileRegistrator.RegisterFile"/> заполняет выходной параметр метаданными, взятыми из предоставленного
    /// <see cref="FileInfo"/>, и возвращает <see langword="true"/> при регистрации существующего файла.
    /// Условия ввода:
    /// - Создается реальный временный файл с известной длиной, временем создания и временем последнего изменения (UTC).
    /// Ожидаемый результат:
    /// - Метод возвращает <see langword="true"/>.
    /// - Свойство <see cref="IncomingFile.FullPath"/> возвращенного объекта <see cref="IncomingFile"/> равно <see cref="FileInfo.FullName"/>.
    /// - Свойства <see cref="IncomingFile.CreatedAt"/> и <see cref="IncomingFile.ModifiedAt"/> возвращенного объекта <see cref="IncomingFile"/>
    /// соответствуют временам UTC из <see cref="FileInfo"/>.
    /// - Свойство <see cref="IncomingFile.Length"/> соответствует <see cref="FileInfo.Length"/>.
    /// - Свойство <see cref="IncomingFile.RegisteredAt"/> устанавливается в метку времени между зафиксированными значениями before/after.
    /// </summary>
    [TestMethod]
    public void RegisterFile_FileExists_PopulatesRegisteredFileAndReturnsTrue()
    {
        var registrator = new FileRegistrator();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
        
        try
        {
            // Создаем реальный временный файл известного размера.
            var data = new byte[128];
            new Random(42).NextBytes(data);
            File.WriteAllBytes(path, data);

            // Устанавливаем известные времена создания и последнего изменения (UTC) для файла.
            var creationUtc = DateTime.UtcNow.AddDays(-2).AddSeconds(-10);
            var lastWriteUtc = DateTime.UtcNow.AddDays(-1).AddMinutes(-5);
            File.SetCreationTimeUtc(path, creationUtc);
            File.SetLastWriteTimeUtc(path, lastWriteUtc);

            // Обновляем FileInfo, чтобы он отразил изменения на диске.
            var fileInfo = new FileInfo(path);
            fileInfo.Refresh();

            var before = DateTimeOffset.UtcNow;
            var result = registrator.RegisterFile(fileInfo, out var registeredFile);
            var after = DateTimeOffset.UtcNow;

            Assert.IsTrue(result, "Expected RegisterFile to return true for a new registration.");
            Assert.AreEqual(fileInfo.FullName, registeredFile.FullPath, "FullPath should be copied from FileInfo.FullName.");
            Assert.AreEqual(fileInfo.Length, registeredFile.Length, "Length should be copied from FileInfo.Length.");
            // Сравниваем времена создания и изменения в UTC.
            Assert.AreEqual(fileInfo.CreationTimeUtc, registeredFile.CreatedAt.UtcDateTime, "CreatedAt should match FileInfo.CreationTimeUtc.");
            Assert.AreEqual(fileInfo.LastWriteTimeUtc, registeredFile.ModifiedAt.UtcDateTime, "ModifiedAt should match FileInfo.LastWriteTimeUtc.");
            Assert.IsTrue(before <= registeredFile.RegisteredAt && registeredFile.RegisteredAt <= after,
                "RegisteredAt should be a timestamp between before and after registration calls.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Проверяет, что при повторной регистрации одного и того же файла метод <see cref="FileRegistrator.RegisterFile"/> 
    /// возвращает <see langword="false"/> при втором вызове.
    /// Условия:
    /// - Создаётся реальный временный файл.
    /// Ожидаемый результат:
    /// - Первый вызов возвращает <see langword="true"/>.
    /// - Второй вызов возвращает <see langword="false"/>.
    /// - Свойства <see cref="IncomingFile.FullPath"/>, <see cref="IncomingFile.Length"/>, 
    ///   <see cref="IncomingFile.CreatedAt"/> и <see cref="IncomingFile.ModifiedAt"/> совпадают для обеих регистраций.
    /// - Свойства <see cref="IncomingFile.RegisteredAt"/> отличаются, так как они зависят от времени вызова метода.
    /// </summary>
    [TestMethod]
    public void RegisterFile_SameFileRegisteredTwice_ReturnsFalseSecondTime()
    {
        var registrator = new FileRegistrator();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
        
        try
        {
            File.WriteAllBytes(path, new byte[16]);
            var fileInfo = new FileInfo(path);
            fileInfo.Refresh();

            var firstResult = registrator.RegisterFile(fileInfo, out var firstRegistered);
            // Небольшая задержка, чтобы гарантировать, что RegisteredAt будет отличаться между двумя регистрациями.
            Thread.Sleep(10);
            var secondResult = registrator.RegisterFile(fileInfo, out var secondRegistered);

            // Assert
            Assert.IsTrue(firstResult, "Expected first registration to return true.");
            Assert.IsFalse(secondResult, "Expected second registration to return false.");
            
            Assert.AreEqual(firstRegistered.FullPath, secondRegistered.FullPath, "FullPath should be identical across registrations.");
            Assert.AreEqual(firstRegistered.Length, secondRegistered.Length, "Length should be identical across registrations.");
            Assert.AreEqual(firstRegistered.CreatedAt.UtcDateTime, secondRegistered.CreatedAt.UtcDateTime, "CreatedAt should be identical across registrations.");
            Assert.AreEqual(firstRegistered.ModifiedAt.UtcDateTime, secondRegistered.ModifiedAt.UtcDateTime, "ModifiedAt should be identical across registrations.");
            Assert.AreNotEqual(firstRegistered.RegisteredAt, secondRegistered.RegisteredAt, "RegisteredAt should differ between registrations because UtcNow is captured each call.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Проверяет, что метод <see cref="FileRegistrator.RegisterFile"/> успешно регистрирует несколько различных файлов.
    /// Условия ввода:
    /// - Создаются два различных временных файла с разными размерами и путями.
    /// Ожидаемый результат:
    /// - Оба вызова RegisterFile возвращают <see langword="true"/>.
    /// - Каждый возвращенный <see cref="IncomingFile"/> содержит уникальные метаданные (путь, размер, время).
    /// - Метаданные каждого файла соответствуют его <see cref="FileInfo"/>.
    /// </summary>
    [TestMethod]
    public void RegisterFile_MultipleDistinctFiles_AllRegisteredSuccessfully()
    {
        var registrator = new FileRegistrator();
        var path1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
        var path2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
        
        try
        {
            // Создаем первый файл размером 100 байт
            File.WriteAllBytes(path1, new byte[100]);
            var fileInfo1 = new FileInfo(path1);
            fileInfo1.Refresh();

            // Создаем второй файл размером 256 байт
            File.WriteAllBytes(path2, new byte[256]);
            var fileInfo2 = new FileInfo(path2);
            fileInfo2.Refresh();

            // Регистрируем оба файла
            var result1 = registrator.RegisterFile(fileInfo1, out var registered1);
            var result2 = registrator.RegisterFile(fileInfo2, out var registered2);

            // Assert
            Assert.IsTrue(result1, "Expected first file registration to return true.");
            Assert.IsTrue(result2, "Expected second file registration to return true.");
            
            Assert.AreEqual(fileInfo1.FullName, registered1.FullPath, "First file FullPath should match FileInfo.FullName.");
            Assert.AreEqual(fileInfo2.FullName, registered2.FullPath, "Second file FullPath should match FileInfo.FullName.");
            
            Assert.AreEqual(100, registered1.Length, "First file length should be 100 bytes.");
            Assert.AreEqual(256, registered2.Length, "Second file length should be 256 bytes.");
            
            Assert.AreNotEqual(registered1.FullPath, registered2.FullPath, "Files should have different paths.");
            Assert.AreNotEqual(registered1.Length, registered2.Length, "Files should have different sizes.");
        }
        finally
        {
            if (File.Exists(path1))
            {
                File.Delete(path1);
            }
            if (File.Exists(path2))
            {
                File.Delete(path2);
            }
        }
    }
}