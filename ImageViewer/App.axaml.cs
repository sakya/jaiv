using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ImageViewer;

public class App : Application
{
    private Window? MainWindow { get; set; }
    public static readonly string AppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "jaiv");
    public static readonly string ThumbnailsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "jaiv", "thumbnails");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow();
            this.MainWindow = desktop.MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static Color? GetStyleColor(string name)
    {
        if (Current is App app && app.MainWindow != null) {
            var resource = app.MainWindow.FindResource(name);
            if (resource is Color col)
                return col;
        }
        return null;
    }
}