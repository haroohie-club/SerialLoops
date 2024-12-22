using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class MapEditorViewModel : EditorViewModel
{
    private MapItem _map;
    public LayoutItem Layout { get; }
    public ObservableCollection<LayoutEntryWithImage> InfoLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> BgLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> ObjectLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> ScrollingBg { get; } = [];

    private bool _bgLayerDisplayed = true;
    public bool BgLayerDisplayed
    {
        get => _bgLayerDisplayed;
        set
        {
            this.RaiseAndSetIfChanged(ref _bgLayerDisplayed, value);
            foreach (LayoutEntryWithImage layoutEntry in BgLayer.Where(l => l.Layer == 0))
            {
                layoutEntry.IsVisible = _bgLayerDisplayed;
            }
        }
    }
    private bool _bgObjectLayerDisplayed = true;
    public bool BgObjectLayerDisplayed
    {
        get => _bgObjectLayerDisplayed;
        set
        {
            this.RaiseAndSetIfChanged(ref _bgObjectLayerDisplayed, value);
            foreach (LayoutEntryWithImage layoutEntry in BgLayer.Where(l => l.Layer == 1))
            {
                layoutEntry.IsVisible = _bgObjectLayerDisplayed;
            }
        }
    }
    [Reactive]
    public bool ObjectLayerDisplayed { get; set; } = true;
    [Reactive]
    public bool InfoLayerDisplayed { get; set; }
    [Reactive]
    public bool ScrollingBgDisplayed { get; set; }
    [Reactive]
    public bool DrawPathingMap { get; set; }
    [Reactive]
    public bool DrawStartingPoint { get; set; }

    [Reactive]
    public int CanvasWidth { get; set; }
    [Reactive]
    public int CanvasHeight { get; set; }
    [Reactive]
    public int ScrollingBgTileWidth { get; set; }
    [Reactive]
    public int ScrollingBgTileHeight { get; set; }
    [Reactive]
    public int ScrollingBgHorizontalTileCount { get; set; }
    public ObservableCollection<ScrollingBgWrapper> ScrollingBgTileSource { get; } = [];

    [Reactive]
    public int StartingPointX { get; set; }
    [Reactive]
    public int StartingPointY { get; set; }

    public ObservableCollection<PathingMapIndicator> PathingMap { get; }

    public ICommand ExportCommand { get; }

    public MapEditorViewModel(MapItem map, MainWindowViewModel window, ILogger log) : base(map, window, log)
    {
        _map = map;
        Layout = new(map.Layout,
            [.. map.Map.Settings.TextureFileIndices.Select(idx => window.OpenProject.Grp.GetFileByIndex(idx))],
            0, map.Layout.LayoutEntries.Count, map.DisplayName);
        CanvasWidth = map.Layout.LayoutEntries.Max(l => l.ScreenX + l.ScreenW);
        CanvasHeight = map.Layout.LayoutEntries.Max(l => l.ScreenY + l.ScreenH);
        for (int i = 0; i < map.Layout.LayoutEntries.Count; i++)
        {
            if (map.Map.Settings.BackgroundLayoutStartIndex > 0)
            {
                if (i >= map.Map.Settings.BackgroundLayoutStartIndex && i <= map.Map.Settings.BackgroundLayoutEndIndex)
                {
                    ScrollingBg.Add(new(Layout, i));
                    continue;
                }
            }
            switch (map.Layout.LayoutEntries[i].RelativeShtxIndex)
            {
                default:
                    InfoLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
                    break;
                case 0:
                case 1:
                    BgLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
                    break;
                case 2:
                    ObjectLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
                    break;
            }
        }

        if (ScrollingBg.Count > 0)
        {
            int minX = ScrollingBg.Min(l => l.ScreenX);
            int minY = ScrollingBg.Min(l => l.ScreenY);
            ScrollingBgTileWidth = ScrollingBg.Max(l => l.ScreenX - minX + l.ScreenWidth);
            ScrollingBgTileHeight = ScrollingBg.Max(l => l.ScreenY - minY + l.ScreenHeight);

            int tileX = CanvasWidth / ScrollingBgTileWidth + 1;
            int tileY = CanvasHeight / ScrollingBgTileHeight + 1;
            int tiles = tileX * tileY;

            for (int i = 0; i < tiles; i++)
            {
                ScrollingBgTileSource.Add(new(ScrollingBg, ScrollingBgTileWidth, ScrollingBgTileHeight));
            }

            ScrollingBgHorizontalTileCount = tileX;
        }

        PathingMap = [];
        SKPoint skGridZero = map.GetOrigin(window.OpenProject.Grp);
        Point gridZero = new(skGridZero.X, skGridZero.Y);
        for (int y = 0; y < map.Map.PathingMap.Length; y++)
        {
            for (int x = 0; x < map.Map.PathingMap[y].Length; x++)
            {
                PathingMap.Add(new(map.Map.PathingMap, x, y, gridZero, map.Map.Settings.SlgMode));
            }
        }

        if (map.Map.Settings.SlgMode)
        {
            StartingPointX = (int)gridZero.X - map.Map.Settings.StartingPosition.x * 32 + map.Map.Settings.StartingPosition.y * 32;
            StartingPointY = (int)gridZero.Y + map.Map.Settings.StartingPosition.x * 16 + map.Map.Settings.StartingPosition.y * 16 + 16;
        }
        else
        {
            StartingPointX = (int)gridZero.X - map.Map.Settings.StartingPosition.x * 16 + map.Map.Settings.StartingPosition.y * 16;
            StartingPointY = (int)gridZero.Y + map.Map.Settings.StartingPosition.x * 8 + map.Map.Settings.StartingPosition.y * 8 + 8;
        }

        ExportCommand = ReactiveCommand.CreateFromTask(Export);
    }

    private async Task Export()
    {
        string exportPath = (await Window.Window.ShowSaveFilePickerAsync("Export Map",
            [new("JSON") { Patterns = ["*.json"] }], _map.DisplayName))?.TryGetLocalPath();

        if (!string.IsNullOrEmpty(exportPath))
        {
            await File.WriteAllTextAsync(exportPath, JsonSerializer.Serialize(_map.Map));
            await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(exportPath), $"{Path.GetFileNameWithoutExtension(exportPath)}_lay.json"),
                JsonSerializer.Serialize(_map.Layout.LayoutEntries, Project.SERIALIZER_OPTIONS));
        }
    }
}

public class ScrollingBgWrapper(ObservableCollection<LayoutEntryWithImage> scrollingBg, int tileWidth, int tileHeight) : ReactiveObject
{
    public ObservableCollection<LayoutEntryWithImage> ScrollingBg { get; set; } = scrollingBg;
    [Reactive] public int TileWidth { get; set; } = tileWidth;
    [Reactive] public int TileHeight { get; set; } = tileHeight;
}
