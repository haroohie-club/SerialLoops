using System.IO;
using Avalonia.Platform;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class PlaceEditorViewModel : EditorViewModel
{
    private PlaceItem _place;
    private SKTypeface _msGothicHaruhi;

    [Reactive]
    public SKBitmap Preview { get; set; }

    public string PlaceName
    {
        get => _place.PlaceName;
        set
        {
            _place.PlaceName = value;
            this.RaisePropertyChanged();
            Preview = _place.GetNewPlaceGraphic(_msGothicHaruhi);
            _place.PlaceGraphic.SetImage(Preview);
            _place.UnsavedChanges = true;
        }
    }

    public PlaceEditorViewModel(PlaceItem place, MainWindowViewModel window, ILogger log) : base(place, window, log)
    {
        _place = place;
        if (string.IsNullOrEmpty(_place.PlaceName))
        {
            _place.PlaceName = _place.DisplayName[4..];
        }
        Preview = _place.GetPreview(window.OpenProject);

        using Stream typefaceStream = AssetLoader.Open(new("avares://SerialLoops/Assets/Graphics/MS-Gothic-Haruhi.ttf"));
        _msGothicHaruhi = SKTypeface.FromStream(typefaceStream);
        if (!PlaceItem.CustomFontMapper.HasFont())
        {
            PlaceItem.CustomFontMapper.AddFont(_msGothicHaruhi);
        }
    }
}
