using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ImageViewer.Utils;

namespace ImageViewer.Controls;

public partial class ImageListControl : UserControl
{
    private List<string> _files = new();
    private int? _selectedIndex;
    private string? _selectedFile;

    public ImageListControl()
    {
        InitializeComponent();
    }

    public void SetPath(string path, string? selectedFile = null)
    {
        WrapPanel.ItemsSource = null;
        ScrollViewer.ScrollToHome();
        LoadImages(path, selectedFile);
    }

    private void LoadImages(string path, string? selectedFile = null)
    {
        _files = new List<string>();
        var files = Directory.GetFiles(path);
        Array.Sort(files, new NaturalComparer());
        foreach (var file in files) {
            var ext = Path.GetExtension(file).ToLower();
            if (ImageControl.SupportedFiles.Contains(ext)) {
                _files.Add(file);
            }
        }

        if (!string.IsNullOrEmpty(selectedFile)) {
            _selectedIndex = _files.IndexOf(selectedFile);
            _selectedFile = selectedFile;
        }

        if (_selectedIndex is null or < 0) {
            _selectedIndex = _files.Count > 0 ? 0 : null;
            _selectedFile = _selectedIndex != null ? _files[_selectedIndex.Value] : null;
        }

        WrapPanel.ItemsSource = _files;
        BringIntoViewSelectedFile();
    }

    private void BringIntoViewSelectedFile()
    {
        if (_selectedIndex == null)
            return;
        var ctrl = WrapPanel.TryGetElement(_selectedIndex.Value) as Border;
        if (ctrl == null)
            ctrl = WrapPanel.GetOrCreateElement(_selectedIndex.Value) as Border;
        if (ctrl != null) {
            WrapPanel.UpdateLayout();
            ctrl.BringIntoView();
        }
    }

    private async void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is Border { Child: Grid grid } border) {
            var selected = e.Index == _selectedIndex;
            border.Background = selected ? new SolidColorBrush(Colors.Black) : SolidColorBrush.Parse("#28000000");

            var f = grid.DataContext as string;
            if (grid.Children[0] is ImageControl img && !string.IsNullOrEmpty(f))
                await img.LoadImage(f);
            if (grid.Children[1] is TextBlock txt)
                txt.Text = Path.GetFileName(f);
        }
    }

    private async void OnElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element is Border { Child: Grid grid } border) {
            border.Background = SolidColorBrush.Parse("#28000000");
            if (grid.Children[0] is ImageControl img)
                await img.Clear();
        }
    }

    private void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border ctrl) {
            _selectedFile = ctrl.DataContext as string;
            if (_selectedFile != null)
                _selectedIndex = _files.IndexOf(_selectedFile);
            else
                _selectedIndex = null;
        }
    }
}