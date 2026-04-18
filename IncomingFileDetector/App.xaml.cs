using System.Windows;
using IncomingFileDetector.Service;
using IncomingFileDetector.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IncomingFileDetector;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private IHost? _host;
    
    /// <summary>
    /// Обрабатывает событие запуска приложения WPF.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события запуска <see cref="StartupEventArgs"/>.</param>
    private async void AppOnStartup(object sender, StartupEventArgs e)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.Configure<FileDetectorOptions>(builder.Configuration.GetSection(nameof(FileDetectorOptions)));
        builder.Services.AddSingleton<IFileRegistrator, FileRegistrator>();
        builder.Services.AddSingleton<FileDetectorService>();
        builder.Services.AddHostedService<FileDetectorService>(p => p.GetRequiredService<FileDetectorService>());
        builder.Services.AddSingleton<IMainViewModel, MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();

        await _host.StartAsync();
        
        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    /// <summary>
    /// Обрабатывает событие завершения работы приложения WPF.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события завершения <see cref="ExitEventArgs"/>.</param>
    /// <remarks>
    /// Этот метод останавливает и освобождает ресурсы, связанные с хостом приложения.
    /// </remarks>
    private async void AppOnExit(object sender, ExitEventArgs e)
    {
        if (_host is null)
            return;
        
        using (_host)
        {
            await _host.StopAsync();
        }
    }
}

