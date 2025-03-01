using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SkiaSharp;

namespace SerialLoops.ViewModels.Controls;

public class MapCharactersSubEditorViewModel : ViewModelBase
{
    public ObservableCollection<LayoutEntryWithImage> BgLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> OcclusionLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> ObjectLayer { get; } = [];

    [Reactive]
    public int CanvasWidth { get; set; }
    [Reactive]
    public int CanvasHeight { get; set; }

    private MainWindowViewModel _window;

    private MapItem _map;
    public MapItem Map
    {
        get => _map;
        set
        {
            this.RaiseAndSetIfChanged(ref _map, value);
            BgLayer.Clear();
            OcclusionLayer.Clear();
            ObjectLayer.Clear();

            if (Map is null)
            {
                if (_script is null)
                {
                    CanvasWidth = 0;
                    CanvasHeight = 0;
                }
                else
                {

                }
            }
            else
            {
                SetMap();
            }
        }
    }

    private ScriptItem _script;

    public MapCharactersSubEditorViewModel(ScriptItem script, MapItem map, MainWindowViewModel window)
    {
        _script = script;
        Map = map;
        _window = window;
    }

    private void SetMap()
    {
        LayoutItem layout = new(_map.Layout,
            [.. _map.Map.Settings.TextureFileIndices.Select(idx => _window.OpenProject.Grp.GetFileByIndex(idx))],
            0, _map.Layout.LayoutEntries.Count, _map.DisplayName);
        CanvasWidth = _map.Layout.LayoutEntries.Max(l => (l.ScreenX + l.ScreenW) / 2);
        CanvasHeight = _map.Layout.LayoutEntries.Max(l => (l.ScreenY + l.ScreenH) / 2);

        for (int i = 0; i < _map.Layout.LayoutEntries.Count; i++)
        {
            if (_map.Map.ObjectMarkers[..^1].Select(o => o.LayoutIndex).Contains((short)i))
            {
                ObjectLayer.Add(new(layout, i) { Layer = _map.Layout.LayoutEntries[i].RelativeShtxIndex, HitTestVisible = false });
                continue;
            }

            switch (_map.Layout.LayoutEntries[i].RelativeShtxIndex)
            {
                case 0:
                case 1:
                    if (_map.Map.Settings.LayoutOcclusionLayerStartIndex > 0 && i >= _map.Map.Settings.LayoutOcclusionLayerStartIndex && i <= _map.Map.Settings.LayoutOcclusionLayerEndIndex)
                    {
                        OcclusionLayer.Add(new(layout, i) { Layer = _map.Layout.LayoutEntries[i].RelativeShtxIndex, HitTestVisible = false });
                    }
                    else if (_map.Map.Settings.LayoutOcclusionLayerStartIndex > 0 && i > _map.Map.Settings.LayoutOcclusionLayerStartIndex
                             || _map.Map.Settings.LayoutOcclusionLayerStartIndex == 0 && i > _map.Map.Settings.LayoutBgLayerEndIndex)
                    {
                        // do nothing (BG junk)
                    }
                    else
                    {
                        BgLayer.Add(new(layout, i) { Layer = _map.Layout.LayoutEntries[i].RelativeShtxIndex });
                    }
                    break;
            }
        }
    }
}

public class ReactiveMapCharacter : LayoutEntryWithImage
{
    public MapCharactersSectionEntry MapCharacter { get; set; }

    public ReactiveMapCharacter(MapCharactersSectionEntry mapCharacter, SKPoint origin, Project project)
    {
        MapCharacter = mapCharacter;
        SKPoint mapPos = MapItem.GetPositionFromGrid(mapCharacter.X, mapCharacter.Y, origin, slgMode: false);
        ScreenX = (short)(mapPos.X / 2);
        ScreenY = (short)(mapPos.Y / 2);

        SKBitmap chibiBitmap = ((ChibiItem)project.Items.First(i =>
                i.Type == ItemDescription.ItemType.Chibi && ((ChibiItem)i).ChibiIndex == mapCharacter.CharacterIndex))
            .ChibiAnimations.First().Value[0].Frame;
        CroppedImage = new(chibiBitmap.Width/ 2, chibiBitmap.Height / 2);
        ScreenWidth = (short)CroppedImage.Width;
        ScreenHeight = (short)CroppedImage.Height;
        chibiBitmap.ScalePixels(chibiBitmap, SKSamplingOptions.Default);
    }
}
