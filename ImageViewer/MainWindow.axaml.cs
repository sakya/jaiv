using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ImageViewer.Controls;

namespace ImageViewer;

public partial class MainWindow : Window
{
    private WindowState? _windowState;

    public MainWindow()
    {
        InitializeComponent();

        KeyDown += OnKeyDown;
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key) {
            case Key.Home:
                e.Handled = true;
                await FirstImage();
                break;
            case Key.End:
                e.Handled = true;
                await LastImage();
                break;
            case Key.Left:
            case Key.PageUp:
                e.Handled = true;
                await PreviousImage();
                break;
            case Key.Right:
            case Key.PageDown:
                e.Handled = true;
                await NextImage();
                break;

            case Key.F:
                e.Handled = true;
                ToggleFullscreen();
                break;
            case Key.Escape:
                if (WindowState == WindowState.FullScreen) {
                    e.Handled = true;
                    ToggleFullscreen();
                }
                break;

            case Key.OemPlus:
                e.Handled = true;
                await ZoomIn();
                break;
            case Key.OemMinus:
                e.Handled = true;
                await ZoomOut();
                break;
            case Key.W:
                e.Handled = true;
                await FitImage();
                break;
        }
    }

    private async Task LoadImage(string filename)
    {
        try {
            await ImageControl.LoadImage(filename);
        } catch {
            // ignored
        }
    }

    private async Task ZoomIn()
    {
        var zoom = ImageControl.Zoom;
        zoom += 0.01;
        zoom = Math.Round(zoom, 2);
        await ImageControl.SetZoom(zoom);
        SetInfo();
    }

    private async Task ZoomOut()
    {
        var zoom = ImageControl.Zoom;
        zoom -= 0.01;
        if (zoom < 0.01)
            zoom = 0.01;
        zoom = Math.Round(zoom, 2);
        await ImageControl.SetZoom(zoom);
        SetInfo();
    }

    private async Task FitImage()
    {
        await ImageControl.FitImage();
        SetInfo();
    }

    private async Task NextImage()
    {
        var filename = ImageControl.Filename;
        if (string.IsNullOrEmpty(filename))
            return;

        var dir = Path.GetDirectoryName(filename);
        if (string.IsNullOrEmpty(dir))
            return;

        var files = GetImages(dir);
        var idx = files.IndexOf(filename);
        if (idx >= 0 && idx + 1 < files.Count) {
            await LoadImage(files[idx + 1]);
            SetInfo();
        }
    }

    private async Task PreviousImage()
    {
        var filename = ImageControl.Filename;
        if (string.IsNullOrEmpty(filename))
            return;

        var dir = Path.GetDirectoryName(filename);
        if (string.IsNullOrEmpty(dir))
            return;

        var files = GetImages(dir);
        var idx = files.IndexOf(filename);
        if (idx > 0) {
            await LoadImage(files[idx - 1]);
            SetInfo();
        }
    }

    private async Task FirstImage()
    {
        var filename = ImageControl.Filename;
        if (string.IsNullOrEmpty(filename))
            return;

        var dir = Path.GetDirectoryName(filename);
        if (string.IsNullOrEmpty(dir))
            return;

        var files = GetImages(dir);
        var idx = files.IndexOf(filename);
        if (idx > 0) {
            await LoadImage(files[0]);
            SetInfo();
        }
    }

    private async Task LastImage()
    {
        var filename = ImageControl.Filename;
        if (string.IsNullOrEmpty(filename))
            return;

        var dir = Path.GetDirectoryName(filename);
        if (string.IsNullOrEmpty(dir))
            return;

        var files = GetImages(dir);
        var idx = files.IndexOf(filename);
        if (idx < files.Count - 1) {
            await LoadImage(files[files.Count - 1]);
            SetInfo();
        }
    }

    private List<string> GetImages(string path)
    {
        var res = new List<string>();

        var files = Directory.GetFiles(path);
        foreach (var file in files) {
            var ext = Path.GetExtension(file).ToLower();
            if (ImageControl.SupportedFiles.Contains(ext)) {
                res.Add(file);
            }
        }

        return res;
    }

    private void ToggleFullscreen()
    {
        if (WindowState != WindowState.FullScreen) {
            _windowState = WindowState;
            WindowState = WindowState.FullScreen;
        } else {
            WindowState = _windowState ?? WindowState.Normal;
            _windowState = null;
        }

        Toolbar.IsVisible = WindowState != WindowState.FullScreen;
    }

    private void SetInfo()
    {
        Title = Path.GetFileName(ImageControl.Filename);
        SizeText.Text = $"{Math.Round(ImageControl.Zoom * 100, 1)}%";
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        var opt = new FilePickerOpenOptions()
        {
            AllowMultiple = false,
        };

        var type = new FilePickerFileType("Images")
        {
            Patterns = ImageControl.SupportedFiles.Select(e => $"*{e}").ToArray()
        };


        opt.FileTypeFilter = new[]
        {
            type,
            new FilePickerFileType("All files")
            {
                Patterns = new [] { "*.*" }
            }
        };

        var files = await StorageProvider.OpenFilePickerAsync(opt);

        if (files.Count > 0) {
            await LoadImage(HttpUtility.UrlDecode(files[0].Path.AbsolutePath));
            SetInfo();
        }
    }

    private async void OnZoomOutClick(object? sender, RoutedEventArgs e)
    {
        await ZoomOut();
    }

    private async void OnZoomInClick(object? sender, RoutedEventArgs e)
    {
        await ZoomIn();
    }

    private async void OnFitImageClick(object? sender, RoutedEventArgs e)
    {
        await FitImage();
    }

    private void OnFullscreenClick(object? sender, RoutedEventArgs e)
    {
        ToggleFullscreen();
    }
}