using IncomingFileDetector.ViewModel;

namespace IncomingFileDetector;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MainWindow"/>.
    /// </summary>
    /// <param name="viewModel">
    /// Экземпляр <see cref="IMainViewModel"/>, предоставляющий данные и логику для главного окна.
    /// </param>
    public MainWindow(IMainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        
        ListRegisteredFiles.ItemsSource = ViewModel.RegisteredFiles;
    }

    /// <summary>
    /// Возвращает экземпляр ViewModel, реализующий интерфейс <see cref="IMainViewModel"/>, 
    /// который предоставляет данные и логику для главного окна приложения.
    /// </summary>
    /// <value>
    /// Экземпляр <see cref="IMainViewModel"/>, используемый для управления состоянием 
    /// и взаимодействием с данными в главном окне.
    /// </value>
    public IMainViewModel ViewModel { get; }
}