using System.Collections.ObjectModel;
using IncomingFileDetector.Service.Model;

namespace IncomingFileDetector.ViewModel;

/// <summary>
/// Определяет интерфейс для ViewModel главного окна приложения.
/// </summary>
public interface IMainViewModel : IDisposable
{
    /// <summary>
    /// Возвращает коллекцию зарегистрированных входящих файлов.
    /// </summary>
    ObservableCollection<IncomingFile> RegisteredFiles { get; }
}