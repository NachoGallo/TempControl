using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using TempControl.ViewModels;

namespace TempControl;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainWindowViewModel();
            _mainWindow = new MainWindow { DataContext = vm };
            _mainWindow.Closing += OnWindowClosing;

            desktop.MainWindow = _mainWindow;
            desktop.Exit += (_, _) =>
            {
                vm.Dispose();
                _trayIcon?.Dispose();
            };

            SetupTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void SetupTrayIcon()
    {
        var icon = new WindowIcon(AssetLoader.Open(new Uri("avares://TempControl/Assets/app.ico")));

        var showItem = new NativeMenuItem("Abrir TempControl");
        showItem.Click += (_, _) => ShowWindow();

        var exitItem = new NativeMenuItem("Cerrar");
        exitItem.Click += (_, _) => ExitApp();

        var menu = new NativeMenu();
        menu.Add(showItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "TempControl",
            Menu = menu,
        };
        _trayIcon.Clicked += (_, _) => ShowWindow();

        var icons = new TrayIcons { _trayIcon };
        TrayIcon.SetIcons(this, icons);
    }

    private void ShowWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApp()
    {
        _mainWindow!.Closing -= OnWindowClosing;
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}
