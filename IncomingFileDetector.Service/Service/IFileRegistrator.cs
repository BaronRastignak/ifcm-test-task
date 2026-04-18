using IncomingFileDetector.Service.Model;

namespace IncomingFileDetector.Service;

/// <summary>
/// Определяет интерфейс для регистрации файлов и получения метаданных о них.
/// </summary>
public interface IFileRegistrator
{
    /// <summary>
    /// Регистрирует указанный файл и возвращает объект <see cref="IncomingFile"/>, 
    /// содержащий метаданные о зарегистрированном файле.
    /// </summary>
    /// <param name="fileInfo">Информация о файле, который необходимо зарегистрировать.</param>
    /// <param name="registeredFile">
    /// Выходной параметр, содержащий объект <see cref="IncomingFile"/> с метаданными 
    /// о зарегистрированном файле.
    /// </param>
    /// <returns>
    /// Значение <see langword="true"/>, если файл был успешно зарегистрирован; 
    /// в противном случае (если файл уже был зарегистрирован ранее) — <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="fileInfo"/> равен <see langword="null"/>.
    /// </exception>
    /// <exception cref="IOException">
    /// Выбрасывается, если произошла ошибка при доступе к файлу.
    /// </exception>
    bool RegisterFile(FileInfo fileInfo, out IncomingFile registeredFile);
}