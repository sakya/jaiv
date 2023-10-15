﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ImageViewer.Utils;

namespace ImageViewer.Controls;

public partial class ImageListControl : UserControl
{
    private readonly ObservableCollection<string> _files = new();
    private int? _selectedIndex;
    private string? _selectedFile;
    private Border? _selectedControl;

    #region events
    public class OpenImageArgs : EventArgs
    {
        public string Filename { get; set; } = null!;
    }

    public delegate void OpenImageHandler(object sender, OpenImageArgs e);
    public event OpenImageHandler? OpenImage;
    #endregion

    public ImageListControl()
    {
        InitializeComponent();
        WrapPanel.ItemsSource = _files;
    }

    public void SetPath(string path, string? selectedFile = null)
    {
        _files.Clear();
        ScrollViewer.ScrollToHome();
        LoadImages(path, selectedFile);
    }

    public void Clear()
    {
        _files.Clear();
        _selectedIndex = null;
        _selectedFile = null;
        _selectedControl = null;
    }

    private void LoadImages(string path, string? selectedFile = null)
    {
        _files.Clear();
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

        Dispatcher.UIThread.InvokeAsync(BringIntoViewSelectedFile, DispatcherPriority.Background);
    }

    private void BringIntoViewSelectedFile()
    {
        if (_selectedIndex == null)
            return;

        WrapPanel.UpdateLayout();
        var ctrl = WrapPanel.TryGetElement(_selectedIndex.Value) as Border;
        if (ctrl == null)
            ctrl = WrapPanel.GetOrCreateElement(_selectedIndex.Value) as Border;
        if (ctrl != null) {
            WrapPanel.UpdateLayout();
            ctrl.BringIntoView();
        }
    }

    private void SetSelectedItemStyle(Border ctrl, bool selected)
    {
        ctrl.Background = selected ? SolidColorBrush.Parse("#28FFFFFF") : SolidColorBrush.Parse("#28000000");
        if (selected) {
            if (_selectedControl != null && _selectedControl != ctrl) {
                SetSelectedItemStyle(_selectedControl, false);
            }
            _selectedControl = ctrl;
        }
    }

    private async void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is Border { Child: Grid grid } border) {
            var selected = e.Index == _selectedIndex;
            SetSelectedItemStyle(border, selected);

            var f = border.DataContext as string;
            if (grid.Children[0] is ImageControl img && !string.IsNullOrEmpty(f))
                await img.LoadImage(f);
            if (grid.Children[1] is TextBlock txt)
                txt.Text = Path.GetFileName(f);
        }
    }

    private async void OnElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element is Border { Child: Grid grid } border) {
            SetSelectedItemStyle(border, false);
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
            SetSelectedItemStyle(ctrl, true);
        }
    }

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border && !string.IsNullOrEmpty(_selectedFile)) {
            OpenImage?.Invoke(this, new OpenImageArgs()
            {
                Filename = _selectedFile
            });
        }
    }
}