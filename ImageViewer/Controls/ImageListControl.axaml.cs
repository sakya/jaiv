using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;

namespace ImageViewer.Controls;

public partial class ImageListControl : UserControl
{
    private List<string> _files = new();

    public ImageListControl()
    {
        InitializeComponent();
    }

    public void SetPath(string path)
    {
        WrapPanel.ItemsSource = null;
        ScrollViewer.ScrollToHome();
        LoadImages(path);
    }

    private void LoadImages(string path)
    {
        _files = new List<string>();
        var files = Directory.GetFiles(path);
        foreach (var file in files) {
            var ext = Path.GetExtension(file).ToLower();
            if (ImageControl.SupportedFiles.Contains(ext)) {
                _files.Add(file);
            }
        }

        WrapPanel.ItemsSource = _files;
    }

    private async void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is ImageControl img) {
            var f = img.DataContext as string;
            if (!string.IsNullOrEmpty(f)) {
                await img.LoadImage(f);
            }
        }
    }

    private async void OnElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element is ImageControl img) {
            await img.Clear();
        }
    }
}