using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;

namespace ImageViewer.Controls;

public partial class ImageControl : UserControl
{
    private Bitmap? _bitmap;
    private static HashSet<string> SupportedByBitmap { get; } = new() { ".bmp", ".jpg", ".jpeg", ".png" };

    public ImageControl()
    {
        InitializeComponent();
        ResizeQuality = BitmapInterpolationMode.HighQuality;
    }

    public BitmapInterpolationMode ResizeQuality
    {
        get;
        set;
    }

    public static HashSet<string> SupportedFiles { get; } = new() { ".bmp", ".jpg", ".jpeg", ".png", ".tiff", ".tga", ".webp" };

    public double Zoom { get; private set; } = 1.0;

    public string? Filename { get; private set; }

    public async Task<bool> LoadImage(string filename)
    {
        if (!File.Exists(filename))
            return false;

        var fi = new FileInfo(filename);
        filename = fi.FullName;

        Spinner.IsVisible = true;
        await Task.Delay(1);
        if (_bitmap != null) {
            _bitmap.Dispose();
            _bitmap = null;
        }

        try {
            await using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (SupportedByBitmap.Contains(fi.Extension.ToLower())) {
                _bitmap = await Task.Run(() => new Bitmap(fs));
            } else {
                var tempImage = await SixLabors.ImageSharp.Image.LoadAsync(fs);
                using var ms = new MemoryStream();
                await tempImage.SaveAsPngAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                _bitmap = await Task.Run(() => new Bitmap(ms));
            }

            Zoom = ScrollViewer.Bounds.Width / _bitmap.Size.Width;
            if (_bitmap.Size.Height * Zoom > ScrollViewer.Bounds.Height)
                Zoom = ScrollViewer.Bounds.Height / _bitmap.Size.Height;

            await SetZoomedBitmap();
        } catch {
            if (Image.Source is Bitmap bmp)
                bmp.Dispose();
            Image.Source = null;
            throw;
        } finally {
            Filename = filename;
            Spinner.IsVisible = false;
        }
        return true;
    }

    public async Task SetZoom(double zoom, bool center = true)
    {
        Zoom = zoom;
        await SetZoomedBitmap();

        if (center) {
            ScrollViewer.Offset = new Vector(
                ScrollViewer.Extent.Width / 2 - ScrollViewer.Bounds.Width / 2,
                ScrollViewer.Extent.Height / 2 - ScrollViewer.Bounds.Height / 2);
        }
    }

    public async Task FitImage()
    {
        if (_bitmap == null)
            return;

        Zoom = ScrollViewer.Bounds.Width / _bitmap.Size.Width;
        if (_bitmap.Size.Height * Zoom > ScrollViewer.Bounds.Height)
            Zoom = ScrollViewer.Bounds.Height / _bitmap.Size.Height;

        await SetZoomedBitmap();
    }

    private async Task SetZoomedBitmap()
    {
        if (_bitmap == null)
            return;

        var iBmp = Image.Source as Bitmap;
        var scaledSize = new PixelSize((int)Math.Round(_bitmap.Size.Width * Zoom, 0),
            (int)Math.Round(_bitmap.Size.Height * Zoom, 0));
        Image.Source = await Task.Run(() => _bitmap.CreateScaledBitmap(scaledSize, ResizeQuality));

        iBmp?.Dispose();
        Image.UpdateLayout();
    }
}