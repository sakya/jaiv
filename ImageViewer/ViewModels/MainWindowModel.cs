namespace ImageViewer.ViewModels;

public class MainWindowModel : BaseModel
{
    private string? _filename;
    public string? Filename
    {
        get => _filename;
        set => SetField(ref _filename, value);
    }

    private bool _displayingImage;
    public bool DisplayingImage
    {
        get => _displayingImage;
        set => SetField(ref _displayingImage, value);
    }

    private double _zoom;
    public double Zoom
    {
        get => _zoom;
        set => SetField(ref _zoom, value);
    }

    private bool _showFolder;
    public bool ShowFolder
    {
        get => _showFolder;
        set => SetField(ref _showFolder, value);
    }

}