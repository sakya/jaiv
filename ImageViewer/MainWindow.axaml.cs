using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ImageViewer.Controls;
using ImageViewer.ViewModels;

namespace ImageViewer;

public partial class MainWindow : Window
{
    private WindowState? _windowState;
    private readonly MainWindowModel _model = new() { Filename = "Jaiv" };

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _model;
        KeyDown += OnKeyDown;
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_model.ShowFolder) {
            switch (e.Key) {
                case Key.O:
                    e.Handled = true;
                    await OpenFile();
                    break;
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
    }

    private async Task LoadImage(string filename)
    {
        try {
            _model.Filename = $"Jaiv [{Path.GetFileName(filename)}]";
            await ImageControl.LoadImage(filename);
            _model.DisplayingImage = true;
            _model.GridViewEnabled = true;
            _model.Zoom = ImageControl.Zoom;
        } catch {
            // ignored
        }
    }

    private async Task ZoomIn(int multiplier = 1)
    {
        if (string.IsNullOrEmpty(ImageControl.Filename))
            return;
        var zoom = ImageControl.Zoom;
        zoom += 0.01 * multiplier;
        zoom = Math.Round(zoom, 2);
        await ImageControl.SetZoom(zoom);
        _model.Zoom = ImageControl.Zoom;
    }

    private async Task ZoomOut(int multiplier = 1)
    {
        if (string.IsNullOrEmpty(ImageControl.Filename))
            return;
        var zoom = ImageControl.Zoom;
        zoom -= 0.01 * multiplier;
        if (zoom < 0.01)
            zoom = 0.01;
        zoom = Math.Round(zoom, 2);
        await ImageControl.SetZoom(zoom);
        _model.Zoom = ImageControl.Zoom;
    }

    private async Task FitImage()
    {
        if (string.IsNullOrEmpty(ImageControl.Filename))
            return;
        await ImageControl.FitImage();
        _model.Zoom = ImageControl.Zoom;
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

    private async Task OpenFile()
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
        }
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        await OpenFile();
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

    private async void OnPreviousClick(object? sender, RoutedEventArgs e)
    {
        await PreviousImage();
    }

    private async void OnNextClick(object? sender, RoutedEventArgs e)
    {
        await NextImage();
    }

    private void OnGridViewIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton btn) {
            var dir = Path.GetDirectoryName(ImageControl.Filename);
            if (string.IsNullOrEmpty(dir))
                return;

            ImageListControl.SetPath(dir);
            _model.ShowFolder = btn.IsChecked == true;
            if (_model.ShowFolder) {
                _model.DisplayingImage = false;
            } else {
                _model.DisplayingImage = !string.IsNullOrEmpty(ImageControl.Filename);
            }
        }
    }
}