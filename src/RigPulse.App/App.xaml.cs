using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RigPulse.App.Services;
using RigPulse.App.Settings;
using RigPulse.App.ViewModels;
using RigPulse.App.Views;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Collections;
using RigPulse.Core.Models;
using RigPulse.Sensors;

namespace RigPulse.App;

/// <summary>
/// Composition root: builds the Generic Host, starts background services and shows the widget.
/// </summary>
public partial class App : Application
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private IHost? _host;
    private ILogger<App>? _logger;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Startup failures are reported to the user instead of crashing silently.")]
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _host = BuildHost(e.Args);
            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            RegisterGlobalExceptionHandlers();

            await _host.StartAsync();

            var window = _host.Services.GetRequiredService<WidgetWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            LogUnhandledException(ex);
            MessageBox.Show($"RigPulse could not start:\n\n{ex.Message}", "RigPulse", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            // The monitor loop never touches the UI thread synchronously, so blocking here is safe.
            _host.StopAsync(ShutdownTimeout).GetAwaiter().GetResult();
            _host.Dispose(); // disposes singletons, including providers and their performance counters
        }

        base.OnExit(e);
    }

    private IHost BuildHost(string[] args)
    {
        var configuration = new ConfigurationManager();
        // The app never edits appsettings.json at runtime, so skip the file watcher.
        configuration.AddInMemoryCollection([new("hostBuilder:reloadConfigOnChange", "false")]);

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
            Configuration = configuration,
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();

        ConfigureServices(builder.Services);
        return builder.Build();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services
            .AddOptions<MonitorOptions>()
            .BindConfiguration(MonitorOptions.SectionName)
            .Validate(o => o.PollInterval >= TimeSpan.FromMilliseconds(250), "Monitor:PollInterval must be at least 250 ms.")
            .Validate(o => o.HistoryCapacity > 0, "Monitor:HistoryCapacity must be positive.")
            .ValidateOnStart();

        // Metrics and monitoring
        services.AddRigPulseSensors();
        services.AddSingleton(sp => new RingBuffer<SystemSnapshot>(sp.GetRequiredService<IOptions<MonitorOptions>>().Value.HistoryCapacity));
        services.AddSingleton<ISnapshotPublisher>(new DispatcherSnapshotPublisher(Dispatcher));
        services.AddHostedService<MonitorService>();

        // UI
        services.AddSingleton<IWidgetSettingsStore, JsonWidgetSettingsStore>();
        services.AddSingleton<WidgetViewModel>();
        services.AddSingleton<WidgetWindow>();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogUnhandledException(args.Exception);
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // A glitch in the widget shouldn't take the whole app down; log it and keep running.
        LogUnhandledException(e.Exception);
        e.Handled = true;
    }

    private void LogUnhandledException(Exception exception)
    {
        if (_logger is not null)
        {
            Log.UnhandledException(_logger, exception);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception.")]
        public static partial void UnhandledException(ILogger logger, Exception exception);
    }
}
