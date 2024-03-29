using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;

namespace ImageViewer.Controls;

public partial class ImageControl : UserControl
{
    private static readonly SemaphoreSlim GlobalBitmapSemaphore = new(3, 3);

    private Bitmap? _bitmap;
    private readonly SemaphoreSlim _bitmapSemaphore = new(1, 1);
    private static HashSet<string> SupportedByBitmap { get; } = [".bmp", ".jpg", ".jpeg", ".png"];

    private Modes _mode = Modes.Image;
    public enum Modes
    {
        Image,
        Grid
    }

    public ImageControl()
    {
        InitializeComponent();
        ResizeQuality = BitmapInterpolationMode.HighQuality;
        Mode = Modes.Image;
        ShowSpinner = true;
    }

    public BitmapInterpolationMode ResizeQuality
    {
        get;
        set;
    }

    public Modes Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            if (_mode == Modes.Grid) {
                Image.Stretch = Stretch.Uniform;
                ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                SpinnerBorder.Background = new SolidColorBrush(Colors.Transparent);
            } else {
                ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                SpinnerBorder.Background = new SolidColorBrush(App.GetStyleColor("SystemAltHighColor") ?? Colors.Black, 0.3);
            }
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (Mode == Modes.Grid)
            SpinnerBorder.Background = new SolidColorBrush(App.GetStyleColor("SystemBaseHighColor") ?? Colors.Black, 0.3);
    }

    public bool ShowSpinner
    {
        get;
        set;
    }

    public double SpinnerFontSize
    {
        get => Spinner.FontSize;
        set => Spinner.FontSize = value;
    }

    public static HashSet<string> SupportedFiles { get; } = [".bmp", ".jpg", ".jpeg", ".png", ".tiff", ".tga", ".webp"];

    public double Zoom { get; private set; } = 1.0;

    public string? Filename { get; private set; }

    public async Task LoadImage(string filename)
    {
        if (!File.Exists(filename))
            return;

        var iBmp = Image.Source as Bitmap;
        Image.Source = null;
        if (ShowSpinner)
            SpinnerBorder.IsVisible = true;
        await Task.Delay(1);
        iBmp?.Dispose();

        await GlobalBitmapSemaphore.WaitAsync();
        await _bitmapSemaphore.WaitAsync();
        var fi = new FileInfo(filename);
        filename = fi.FullName;

        if (_bitmap != null) {
            _bitmap.Dispose();
            _bitmap = null;
        }

        try {
            var fromCache = false;
            var tfn = GetThumbnailFilename(filename);
            if (File.Exists(tfn)) {
                var cfi = new FileInfo(tfn);
                if (cfi.LastWriteTimeUtc > fi.LastWriteTimeUtc) {
                    await using var tFs = new FileStream(tfn, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try {
                        _bitmap = await Task.Run(() => new Bitmap(tFs));
                        fromCache = true;
                        if (Mode == Modes.Image) {
                            await SetBitmap(fromCache, tfn);
                        }
                    } catch {
                        // ignored
                    }
                }
            }

            if (_bitmap == null || Mode == Modes.Image) {
                await using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (SupportedByBitmap.Contains(fi.Extension.ToLower())) {
                    _bitmap = await Task.Run(() => new Bitmap(fs));
                } else {
                    using var tempImage = await SixLabors.ImageSharp.Image.LoadAsync(fs);
                    using var ms = new MemoryStream();
                    await Task.Run(() => tempImage.SaveAsPng(ms));
                    ms.Seek(0, SeekOrigin.Begin);
                    _bitmap = await Task.Run(() => new Bitmap(ms));
                }
            }

            await SetBitmap(fromCache, tfn);
        } catch {
            if (Image.Source is Bitmap bmp)
                bmp.Dispose();
            Image.Source = null;
            throw;
        } finally {
            Filename = filename;
            SpinnerBorder.IsVisible = false;
            _bitmapSemaphore.Release();
            GlobalBitmapSemaphore.Release();
        }
    }

    public async Task Clear()
    {
        Filename = null;
        await _bitmapSemaphore.WaitAsync();
        if (_bitmap != null) {
            _bitmap.Dispose();
            _bitmap = null;
        }
        _bitmapSemaphore.Release();

        if (Image.Source is Bitmap bmp)
            bmp.Dispose();
        Image.Source = null;
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

    private async Task SetBitmap(bool fromCache, string thumbnailFilename)
    {
        if (_bitmap == null)
            return;

        if (Mode == Modes.Image) {
            Zoom = ScrollViewer.Bounds.Width / _bitmap.Size.Width;
            if (_bitmap.Size.Height * Zoom > ScrollViewer.Bounds.Height)
                Zoom = ScrollViewer.Bounds.Height / _bitmap.Size.Height;

            await SetZoomedBitmap();
        } else {
            var ratio = _bitmap.Size.Width / _bitmap.Size.Height;
            var scaled =
                _bitmap.CreateScaledBitmap(new PixelSize(800, (int)Math.Round(800 / ratio, 0)), ResizeQuality);
            Image.Source = await Task.Run(() => scaled);
            if (!fromCache)
                await Task.Run(async () => await SaveThumbnail(thumbnailFilename, scaled));

            _bitmap.Dispose();
            _bitmap = null;
        }
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

    private string GetThumbnailFilename(string filename)
    {
        var file = Path.GetFileName(filename);
        var dir = Path.GetDirectoryName(filename);
        if (!string.IsNullOrEmpty(dir))
            return Path.Combine(App.ThumbnailsPath, GetMd5(dir), GetMd5(file));
        return Path.Combine(App.ThumbnailsPath, GetMd5(file));
    }

    private async Task SaveThumbnail(string filename, Bitmap image)
    {
        var dir = Path.GetDirectoryName(filename);
        if (string.IsNullOrEmpty(dir))
            return;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        await using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);

        using var ms = new MemoryStream();
        image.Save(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tempImage = await SixLabors.ImageSharp.Image.LoadAsync(ms);
        await tempImage.SaveAsJpegAsync(fs);
    }

    private string GetMd5(string input)
    {
        var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        var hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes);
    }
}