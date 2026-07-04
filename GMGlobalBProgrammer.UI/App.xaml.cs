using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Threading;
using GMGlobalBProgrammer.Core.J2534;
using GMGlobalBProgrammer.Core.UDS;
using GMGlobalBProgrammer.Core.CAN;
using GMGlobalBProgrammer.Core.Functions;
using GMGlobalBProgrammer.Core.Parsers;
using System.Runtime.InteropServices;

namespace GMGlobalBProgrammer.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;
    
    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();
    
    static App()
    {
        AllocConsole();
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            AllocConsole();
            System.Console.WriteLine("=== Application Startup ===");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
                    
            base.OnStartup(e);
            System.Console.WriteLine("Starting application...");
            ConfigureServices();
                    
            System.Console.WriteLine("Getting main window...");
            var mainWindow = _serviceProvider?.GetService<MainWindow>();
            if (mainWindow == null)
            {
                System.Console.WriteLine("MainWindow is null!");
                MessageBox.Show("Failed to create main window", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
                    
            System.Console.WriteLine("Showing main window...");
            mainWindow.Show();
            System.Console.WriteLine("MainWindow shown successfully.");
            System.Console.WriteLine("Application is now running. Check device dropdown for J2534 devices.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error starting application: {ex}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        System.Console.WriteLine($"Unhandled exception: {e.ExceptionObject}");
        if (e.ExceptionObject is Exception ex)
        {
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
    
    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        System.Console.WriteLine($"Dispatcher unhandled exception: {e.Exception}");
        System.Console.WriteLine($"Stack trace: {e.Exception.StackTrace}");
        e.Handled = true;
    }
    
    private void ConfigureServices()
    {
        try
        {
            System.Console.WriteLine("Configuring services...");
            var services = new ServiceCollection();
            
            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            // Register specific loggers needed for services
            services.AddLogging(); // This registers ILogger<T> for all types
            
            System.Console.WriteLine("Adding core services...");
            // Core services
            services.AddSingleton<MockJ2534Manager>();
            services.AddSingleton<IJ2534Manager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<J2534Manager>>();
                var vendorLogger = provider.GetRequiredService<ILogger<VendorDetector>>();
                
                // Check if real hardware is available, otherwise use mock
                var detector = new VendorDetector(vendorLogger);
                var devices = detector.DetectAvailableDevices();
                
                if (devices.Count > 0)
                {
                    System.Console.WriteLine("Real J2534 devices detected, using real implementation.");
                    return new J2534Manager(logger, vendorLogger);
                }
                else
                {
                    System.Console.WriteLine("No real J2534 devices found, using mock implementation.");
                    var mockLogger = provider.GetRequiredService<ILogger<MockJ2534Manager>>();
                    return new MockJ2534Manager(mockLogger);
                }
            });
            services.AddSingleton<ICANController, CANController>();
            services.AddSingleton<IISOTPTransport, ISOTPTransport>();
            services.AddSingleton<IUDSClient, UDSClient>();
            services.AddSingleton<IGMSecurityAccess, GMSecurityAccess>();
            
            System.Console.WriteLine("Adding function services...");
            // Function services
            services.AddTransient<ISBATFunction, SBATFunction>();
            services.AddTransient<IVINWriter, VINWriter>();
            services.AddTransient<IInjectorProgrammer, InjectorProgrammer>();
            
            System.Console.WriteLine("Adding parser services...");
            // Parser services - register individual parsers first
            services.AddTransient<ASCLogParser>();
            services.AddTransient<PCANLogParser>();
            services.AddTransient<ICANLogParser, GenericCANLogParser>();
            
            System.Console.WriteLine("Adding UI services...");
            // UI
            services.AddTransient<MainWindow>();
            
            System.Console.WriteLine("Building service provider...");
            _serviceProvider = services.BuildServiceProvider();
            
            System.Console.WriteLine("Initializing services...");
            // Initialize services with error handling
            try
            {
                var j2534Manager = _serviceProvider.GetService<IJ2534Manager>();
                if (j2534Manager != null)
                {
                    j2534Manager.InitializeAsync().Wait();
                    System.Console.WriteLine("J2534Manager initialized successfully.");
                }
                else
                {
                    System.Console.WriteLine("ERROR: J2534Manager is null!");
                }
            }
            catch (Exception initEx)
            {
                System.Console.WriteLine($"Error initializing services: {initEx}");
                System.Console.WriteLine($"Stack trace: {initEx.StackTrace}");
                // Don't throw here - let the app continue even if initialization fails
            }
            System.Console.WriteLine("Services initialization completed.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error in ConfigureServices: {ex}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

