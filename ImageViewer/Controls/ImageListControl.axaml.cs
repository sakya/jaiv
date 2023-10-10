using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImageViewer.Controls;

public partial class ImageListControl : UserControl
{
    public ImageListControl()
    {
        InitializeComponent();
    }

    private void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {

    }
}