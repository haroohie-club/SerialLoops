using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using AvaloniaEdit.Utils;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Editors;
using SkiaSharp;

namespace SerialLoops.ViewModels.Controls;

public class MapCharactersSubEditorViewModel : ViewModelBase
{
    [Reactive]
    public bool HasMapCharacters { get; set; }

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
            ChibiDirectionSelector = new(_selectedMapCharacter?.FacingDirection ?? ChibiItem.Direction.DOWN_LEFT,
                direction =>
                {
                    _selectedMapCharacter.FacingDirection = direction;
                });
            ChibiDirectionSelector.SetAvailableDirections(_selectedMapCharacter?.Chibi.ChibiEntries);
        }
    }

    public ObservableCollection<ChibiItem> Chibis { get; }


    [Reactive]
    public int CanvasWidth { get; set; }
    [Reactive]
    public int CanvasHeight { get; set; }

    private readonly MainWindowViewModel _window;
    public ScriptEditorViewModel ScriptEditor { get; }

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
                    CanvasWidth = MapCharacters.Max(c => c.ScreenX + c.ScreenWidth);
                    CanvasHeight = MapCharacters.Max(c => c.ScreenY + c.ScreenHeight);
                }
            }
            else
            {
                Origin = Map.GetOrigin(_window.OpenProject.Grp);
                SetMap();
            }
        }
    }

    private readonly ScriptItem _script;

    public ICommand RefreshMapsCommand { get; }
    public ICommand RemoveTalkLinkCommand { get; }
    public ICommand AddMapCharacterCommand { get; }
    public ICommand RemoveMapCharacterCommand { get; }

    public ICommand AddMapCharactersCommand { get; }
    public ICommand RemoveMapCharactersCommand { get; }

    public MapCharactersSubEditorViewModel(ScriptItem script, ScriptEditorViewModel scriptEditor)
    {
        _script = script;
        ScriptEditor = scriptEditor;
        _window = scriptEditor.Window;
        Chibis = new(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).Cast<ChibiItem>());

        HasMapCharacters = _script.Event.MapCharactersSection is not null;
        if (HasMapCharacters)
        {
            RefreshMaps();
            LoadMapCharacters(refresh: true);
        }

        RemoveMapCharactersCommand = ReactiveCommand.CreateFromTask(async Task () => await RemoveMapCharactersSection(true));
        AddMapCharactersCommand = ReactiveCommand.Create(() =>
        {
            _script.Event.MapCharactersSection = new() { Name = "MAPCHARACTERS" };
            _script.Event.MapCharactersSection.Objects.Add(new());
            _script.Event.NumSections += 2;
            RefreshMaps();
            LoadMapCharacters(refresh: true);
            _script.UnsavedChanges = true;
            ScriptEditor.UpdatePreview();
            HasMapCharacters = true;
        });

        RefreshMapsCommand = ReactiveCommand.Create(RefreshMaps);
        RemoveTalkLinkCommand = ReactiveCommand.Create(() => SelectedMapCharacter.TalkSection = null);
        AddMapCharacterCommand = ReactiveCommand.Create(() =>
        {
            ReactiveMapCharacter newCharacter = new(new()
            {
                CharacterIndex = 1,
                FacingDirection = (short)ChibiItem.Direction.DOWN_LEFT,
                TalkScriptBlock = 0,
                X = (short)(Map.Map.Settings.MapWidth / 2),
                Y = (short)(Map.Map.Settings.MapHeight / 2),
            }, Origin, _window.OpenProject, _script, this);
            _script.Event.MapCharactersSection.Objects.Insert(_script.Event.MapCharactersSection.Objects.Count - 1,
                newCharacter.MapCharacter);
            MapCharacters.Add(newCharacter);
            ObjectLayer.Add(newCharacter);
            SelectedMapCharacter = newCharacter;
            ScriptEditor.UpdatePreview();
            _script.UnsavedChanges = true;
        });
        RemoveMapCharacterCommand = ReactiveCommand.Create(() =>
        {
            _script.Event.MapCharactersSection.Objects.Remove(SelectedMapCharacter.MapCharacter);
            MapCharacters.Remove(SelectedMapCharacter);
            ObjectLayer.Remove(SelectedMapCharacter);
            SelectedMapCharacter = null;
            ScriptEditor.UpdatePreview();
            _script.UnsavedChanges = true;
        });
    }

    private void RefreshMaps()
    {
        Maps.Clear();
        Maps.AddRange(ScriptEditor.Commands.Values
            .SelectMany(c => c)
            .Where(c => c.Verb == EventFile.CommandVerb.LOAD_ISOMAP)
            .Select(c => ((MapScriptParameter)c.Parameters[0]).Map));
        Map ??= Maps.FirstOrDefault();
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

    private void LoadMapCharacters(bool refresh = false, bool clean = false, ReactiveMapCharacter selected = null)
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

        List<ObjectMarker> markers = new(Map?.Map.ObjectMarkers[..^1] ?? []);
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

        if (selected is not null)
        {
            SelectedMapCharacter = MapCharacters.FirstOrDefault(c => c.MapCharacter == selected.MapCharacter);
        }
    }

    public void UpdateMapCharacter(ReactiveMapCharacter mapCharacter, short x, short y)
    {
        bool noChange = mapCharacter.MapCharacter.X == x && mapCharacter.MapCharacter.Y == y;
        mapCharacter.MapCharacter.X = x;
        mapCharacter.MapCharacter.Y = y;
        LoadMapCharacters(refresh: true, clean: true, selected: mapCharacter);

        if (noChange)
        {
            return;
        }

        _script.UnsavedChanges = true;
    }

    public async Task RemoveMapCharactersSection(bool prompt)
    {
        if (prompt)
        {
            if (await ScriptEditor.Window.Window.ShowMessageBoxAsync(Strings.ScriptEditorDeleteMapChacatersPromptTitle,
                    Strings.MapCharactersEditorDeletePrompt,
                    ButtonEnum.YesNo, Icon.Warning, ScriptEditor.Window.Log) != ButtonResult.Yes)
            {
                return;
            }
        }
        HasMapCharacters = false;
        _script.Event.MapCharactersSection = null;
        _script.Event.NumSections -= 2;
        SelectedMapCharacter = null;
        MapCharacters.Clear();
        Map = null;
        Maps.Clear();
        ScriptEditor.UpdatePreview();
        _script.UnsavedChanges = true;
    }
}

public class ReactiveMapCharacter : LayoutEntryWithImage
{
    private readonly ScriptItem _script;
    private readonly SKPoint _origin;
    private readonly MapCharactersSubEditorViewModel _parent;

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

    public ReactiveScriptSection TalkSection
    {
        get => _parent.ScriptEditor.ScriptSections.FirstOrDefault(s => s.Section.Name.Equals(
            _script.Event.LabelsSection.Objects
                .FirstOrDefault(l => l.Id == MapCharacter.TalkScriptBlock)?.Name.Replace("/", "")));
        set
        {
            MapCharacter.TalkScriptBlock =
                _script.Event.LabelsSection.Objects.FirstOrDefault(l => l.Name.Replace("/", "")
                    .Equals(value?.Name))?.Id ?? 0;
            this.RaisePropertyChanged();
            _parent.ScriptEditor.UpdatePreview();
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
