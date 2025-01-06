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
using Path = System.IO.Path;

namespace SerialLoops.ViewModels.Editors;

public class MapEditorViewModel : EditorViewModel
{
    private MapItem _map;
    public LayoutItem Layout { get; }
    public ObservableCollection<LayoutEntryWithImage> InfoLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> BgLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> BgOcclusionLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> ObjectLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> ScrollingBg { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> CameraTruckingDefinitions { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> BgJunkLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> ObjectJunkLayer { get; } = [];

    public ObservableCollection<HighlightedSpace> InteractableObjects { get; } = [];
    public ObservableCollection<HighlightedSpace> Unknown2s { get; } = [];
    public ObservableCollection<HighlightedSpace> ObjectPositions { get; } = [];

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
    public bool BgOcclusionLayerDisplayed { get; set; } = true;
    [Reactive]
    public bool ScrollingBgDisplayed { get; set; }
    [Reactive]
    public bool InfoLayerDisplayed { get; set; }
    [Reactive]
    public bool BgJunkLayerDisplayed { get; set; }
    [Reactive]
    public bool ObjectJunkLayerDisplayed { get; set; }

    [Reactive]
    public bool InteractableObjectsDisplayed { get; set; }
    [Reactive]
    public bool Unknown2sDisplayed { get; set; }
    [Reactive]
    public bool ObjectPositionsDisplayed { get; set; }

    [Reactive]
    public bool DrawPathingMap { get; set; }
    [Reactive]
    public bool DrawStartingPoint { get; set; }
    [Reactive]
    public bool DrawOrigin { get; set; }
    [Reactive]
    public bool DrawBoundary { get; set; }
    [Reactive]
    public bool DrawCameraTruckingDefinitions { get; set; }

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

    private LayoutEntryWithImage _origin;
    public short OriginX
    {
        get => _origin.ScreenX;
        set
        {
            _origin.ScreenX = value;
            this.RaisePropertyChanged();
        }
    }
    public short OriginY
    {
        get => _origin.ScreenY;
        set
        {
            _origin.ScreenY = value;
            this.RaisePropertyChanged();
        }
    }
    public SKColor OriginColor
    {
        get => _origin.Tint;
        set
        {
            _origin.Tint = value;
            this.RaisePropertyChanged();
        }
    }

    private LayoutEntryWithImage _boundary;
    public short BoundaryX
    {
        get => _boundary.ScreenX;
        set
        {
            _boundary.ScreenX = value;
            this.RaisePropertyChanged();
        }
    }
    public short BoundaryY
    {
        get => _boundary.ScreenY;
        set
        {
            _boundary.ScreenY = value;
            this.RaisePropertyChanged();
        }
    }
    public short BoundaryWidth
    {
        get => _boundary.ScreenWidth;
        set
        {
            _boundary.ScreenWidth = value;
            this.RaisePropertyChanged();
        }
    }
    public short BoundaryHeight
    {
        get => _boundary.ScreenHeight;
        set
        {
            _boundary.ScreenHeight = value;
            this.RaisePropertyChanged();
        }
    }
    public SKColor BoundaryColor
    {
        get => _boundary.Tint;
        set
        {
            _boundary.Tint = value;
            this.RaisePropertyChanged();
        }
    }

    public ObservableCollection<HighlightedSpace> PathingMap { get; }

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
            if (map.Map.Settings.IntroCameraTruckingDefsStartIndex > 0 && i >= map.Map.Settings.IntroCameraTruckingDefsStartIndex
                && i <= map.Map.Settings.IntroCameraTruckingDefsEndIndex)
            {
                CameraTruckingDefinitions.Add(new(Layout, i));
                continue;
            }

            if (map.Map.Settings.ScrollingBgDefinitionLayoutIndex > 0)
            {
                if (i >= map.Map.Settings.ScrollingBgLayoutStartIndex && i <= map.Map.Settings.ScrollingBgLayoutEndIndex)
                {
                    ScrollingBg.Add(new(Layout, i));
                    continue;
                }
            }

            if (map.Map.UnknownMapObject3s[..^1].Select(u => u.UnknownShort3).Contains((short)i))
            {
                ObjectLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
                continue;
            }

            switch (map.Layout.LayoutEntries[i].RelativeShtxIndex)
            {
                default:
                    if (i == 0)
                    {
                        _origin = new(Layout, i);
                        continue;
                    }
                    if (i == 1)
                    {
                        _boundary = new(Layout, i);
                        continue;
                    }
                    InfoLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
                    break;
                case 0:
                case 1:
                    if (map.Map.Settings.LayoutOcclusionLayerStartIndex > 0 && i >= map.Map.Settings.LayoutOcclusionLayerStartIndex && i <= map.Map.Settings.LayoutOcclusionLayerEndIndex)
                    {
                        BgOcclusionLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
                    }
                    else if (map.Map.Settings.LayoutOcclusionLayerStartIndex > 0 && i > map.Map.Settings.LayoutOcclusionLayerStartIndex
                             || map.Map.Settings.LayoutOcclusionLayerStartIndex == 0 && i > map.Map.Settings.LayoutBgLayerEndIndex)
                    {
                        BgJunkLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
                    }
                    else
                    {
                        BgLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
                    }
                    break;
                case 2:
                    ObjectJunkLayer.Add(new(Layout, i) { Layer = map.Layout.LayoutEntries[i].RelativeShtxIndex });
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
            int scaleX = 1;
            int scaleY = 1;

            for (int i = 0; i < tiles; i++)
            {
                switch (map.Map.Settings.TransformMode)
                {
                    case 4:
                        if (i % 2 == 1)
                        {
                            scaleX = -1;
                        }
                        break;
                }
                ScrollingBgTileSource.Add(new(ScrollingBg, ScrollingBgTileWidth, ScrollingBgTileHeight, scaleX, scaleY));
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

        InteractableObjects = new(map.Map.InteractableObjects[..^1].Select(io => new HighlightedSpace(io, gridZero, window.OpenProject)));

        Unknown2s = new(map.Map.UnknownMapObject2s[..^1].Select(u => new HighlightedSpace(u, gridZero, map.Map.Settings.SlgMode)));

        ObjectPositions = new(map.Map.UnknownMapObject3s[..^1].Select(u => new HighlightedSpace(u, gridZero, map.Map.Settings.SlgMode)));

        ExportCommand = ReactiveCommand.CreateFromTask(Export);
    }

    private async Task Export()
    {
        string exportPath = (await Window.Window.ShowSaveFilePickerAsync("Export Map",
            [new("JSON") { Patterns = ["*.json"] }], _map.DisplayName))?.TryGetLocalPath();

        if (!string.IsNullOrEmpty(exportPath))
        {
            await File.WriteAllTextAsync(exportPath, JsonSerializer.Serialize(_map.Map, Project.SERIALIZER_OPTIONS));
            await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(exportPath), $"{Path.GetFileNameWithoutExtension(exportPath)}_lay.json"),
                JsonSerializer.Serialize(_map.Layout.LayoutEntries, Project.SERIALIZER_OPTIONS));
        }
    }
}

public class ScrollingBgWrapper(ObservableCollection<LayoutEntryWithImage> scrollingBg, int tileWidth, int tileHeight, int scaleX, int scaleY) : ReactiveObject
{
    public ObservableCollection<LayoutEntryWithImage> ScrollingBg { get; set; } = scrollingBg;
    [Reactive] public int TileWidth { get; set; } = tileWidth;
    [Reactive] public int TileHeight { get; set; } = tileHeight;
    [Reactive] public int ScaleX { get; set; } = scaleX;
    [Reactive] public int ScaleY { get; set; } = scaleY;
}
