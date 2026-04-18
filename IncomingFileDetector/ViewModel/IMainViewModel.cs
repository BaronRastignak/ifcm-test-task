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
    
    /// <summary>
    /// Возвращает или задает путь к директории, которая наблюдается для обнаружения входящих файлов.
    /// </summary>
    string ObservedDirectory { get; set; }
    
    /// <summary>
    /// Возвращает или задает интервал опроса директории в секундах.
    /// </summary>
    long PollingTimeout { get; set; }
}