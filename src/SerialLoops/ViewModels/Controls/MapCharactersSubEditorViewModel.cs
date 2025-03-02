using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using AvaloniaEdit.Utils;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Models;
using SkiaSharp;
using SoftCircuits.Collections;

namespace SerialLoops.ViewModels.Controls;

public class MapCharactersSubEditorViewModel : ViewModelBase
{
    public Dictionary<Point, SKPoint> AllGridPositions { get; } = [];

    [Reactive]
    public SKPoint Origin { get; set; }

    public ObservableCollection<LayoutEntryWithImage> BgLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> OcclusionLayer { get; } = [];
    public ObservableCollection<LayoutEntryWithImage> ObjectLayer { get; } = [];

    public ObservableCollection<ReactiveMapCharacter> MapCharacters { get; } = [];

    private ReactiveMapCharacter _selectedMapCharacter;
    public ReactiveMapCharacter SelectedMapCharacter
    {
        get => _selectedMapCharacter;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMapCharacter, value);
            ChibiDirectionSelector = new(_selectedMapCharacter.FacingDirection, direction =>
            {
                _selectedMapCharacter.FacingDirection = direction;
            });
            ChibiDirectionSelector.SetAvailableDirections(_selectedMapCharacter.Chibi.ChibiEntries);
        }
    }
    public ObservableCollection<ChibiItem> Chibis { get; } = [];


    [Reactive]
    public int CanvasWidth { get; set; }
    [Reactive]
    public int CanvasHeight { get; set; }

    private MainWindowViewModel _window;

    [Reactive]
    public ChibiDirectionSelectorViewModel ChibiDirectionSelector { get; set; }

    public ObservableCollection<MapItem> Maps { get; } = [];

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
                if (MapCharacters.Count == 0)
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
                Origin = Map.GetOrigin(_window.OpenProject.Grp);
                SetMap();
            }
        }
    }

    private ScriptItem _script;

    public MapCharactersSubEditorViewModel(ScriptItem script, OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commands, MainWindowViewModel window)
    {
        _script = script;
        _window = window;
        Chibis = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).Cast<ChibiItem>());

        Maps.AddRange(commands.Values
            .SelectMany(c => c)
            .Where(c => c.Verb == EventFile.CommandVerb.LOAD_ISOMAP)
            .Select(c => ((MapScriptParameter)c.Parameters[0]).Map));
        Map = Maps.FirstOrDefault();
        LoadMapCharacters(refresh: true);
    }

    private void SetMap()
    {
        AllGridPositions.Clear();
        for (int x = 0; x < Map.Map.Settings.MapWidth; x++)
        {
            for (int y = 0; y < Map.Map.Settings.MapHeight; y++)
            {
                SKPoint skPoint = Map.GetPositionFromGrid(x, y, Origin);
                AllGridPositions.Add(new(skPoint.X, skPoint.Y), new(x, y));
            }
        }

        LayoutItem layout = new(_map.Layout,
            [.. _map.Map.Settings.TextureFileIndices.Select(idx => _window.OpenProject.Grp.GetFileByIndex(idx))],
            0, _map.Layout.LayoutEntries.Count, _map.DisplayName);
        CanvasWidth = _map.Layout.LayoutEntries.Max(l => l.ScreenX + l.ScreenW);
        CanvasHeight = _map.Layout.LayoutEntries.Max(l => l.ScreenY + l.ScreenH);

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

        for (int i = 0; i < _map.Map.ObjectMarkers[..^1].Count; i++)
        {
            ObjectLayer.Swap(i, ObjectLayer.ToList()[i..].FindIndex(o => o.Index == _map.Map.ObjectMarkers[i].LayoutIndex) + i);
        }

        LoadMapCharacters();
    }

    private void LoadMapCharacters(bool refresh = false, bool clean = false)
    {
        if (clean)
        {
            for (int i = ObjectLayer.Count - 1; i >= 0; i--)
            {
                if (ObjectLayer[i] is ReactiveMapCharacter character)
                {
                    ObjectLayer.Remove(character);
                }
            }
        }

        if (refresh)
        {
            MapCharacters.Clear();
            MapCharacters.AddRange(_script.Event.MapCharactersSection.Objects
                .Where(c => c.CharacterIndex != 0)
                .Select(c => new ReactiveMapCharacter(c, Origin, _window.OpenProject, _script, this)));
        }

        List<ObjectMarker> markers = new(Map.Map.ObjectMarkers[..^1]);
        foreach (ReactiveMapCharacter character in MapCharacters)
        {
            int spaceIndex = markers.FindIndex(m => m.ObjectX > character.MapCharacter.X && m.ObjectY > character.MapCharacter.Y);

            if (spaceIndex < 0)
            {
                ObjectLayer.Add(character);
            }
            else
            {
                int layoutIndex = ObjectLayer.ToList().FindIndex(l => l.Index == markers[spaceIndex].LayoutIndex);
                ObjectLayer.Insert(layoutIndex, character);
            }
        }
    }

    public void UpdateMapCharacter(ReactiveMapCharacter mapCharacter, short x, short y)
    {
        bool noChange = mapCharacter.MapCharacter.X == x && mapCharacter.MapCharacter.Y == y;
        mapCharacter.MapCharacter.X = x;
        mapCharacter.MapCharacter.Y = y;
        LoadMapCharacters(refresh: true, clean: true);

        if (noChange)
        {
            return;
        }

        _script.UnsavedChanges = true;
    }
}

public class ReactiveMapCharacter : LayoutEntryWithImage
{
    private ScriptItem _script;
    private SKPoint _origin;
    private MapCharactersSubEditorViewModel _parent;

    public List<bool> ValidDirections { get; private set; }

    public MapCharactersSectionEntry MapCharacter { get; set; }

    private ChibiItem _chibi;
    public ChibiItem Chibi
    {
        get => _chibi;
        set
        {
            this.RaiseAndSetIfChanged(ref _chibi, value);
            MapCharacter.CharacterIndex = _chibi.ChibiIndex;
            _parent.ChibiDirectionSelector?.SetAvailableDirections(_chibi.ChibiEntries);
            if (MapCharacter.FacingDirection >= _chibi.ChibiEntries.Count)
            {
                FacingDirection = ChibiItem.Direction.DOWN_LEFT;
            }
            SetUpChibi();
            _script.UnsavedChanges = true;
        }
    }

    public ChibiItem.Direction FacingDirection
    {
        get => (ChibiItem.Direction)MapCharacter.FacingDirection;
        set
        {
            MapCharacter.FacingDirection = (short)value;
            this.RaisePropertyChanged();
            SetUpChibi();
            _script.UnsavedChanges = true;
        }
    }

    public ReactiveMapCharacter(MapCharactersSectionEntry mapCharacter, SKPoint origin, Project project, ScriptItem script,
        MapCharactersSubEditorViewModel parent)
    {
        _script = script;
        _origin = origin;
        _parent = parent;
        MapCharacter = mapCharacter;
        _chibi = (ChibiItem)project.Items.First(i =>
            i.Type == ItemDescription.ItemType.Chibi && ((ChibiItem)i).ChibiIndex == MapCharacter.CharacterIndex);
        SetUpChibi();
    }

    private void SetUpChibi()
    {
        SKPoint mapPos = MapItem.GetPositionFromGrid(MapCharacter.X, MapCharacter.Y, _origin, slgMode: false);
        CroppedImage = _chibi.ChibiAnimations.ElementAt(MapCharacter.FacingDirection).Value[0].Frame;
        ScreenX = (short)(mapPos.X - CroppedImage.Width / 2);
        ScreenY = (short)(mapPos.Y - CroppedImage.Height / 2 - 24);
        ScreenWidth = (short)CroppedImage.Width;
        ScreenHeight = (short)CroppedImage.Height;
    }
}
