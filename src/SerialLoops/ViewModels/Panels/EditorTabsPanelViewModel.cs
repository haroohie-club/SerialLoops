using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.SaveFile;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Editors;

namespace SerialLoops.ViewModels.Panels;

public class EditorTabsPanelViewModel : ViewModelBase
{
    private readonly Project _project;
    private readonly ILogger _log;

    public MainWindowViewModel MainWindow { get; private set; }

    [Reactive]
    public EditorViewModel SelectedTab { get; set; }
    [Reactive]
    public bool ShowTabsPanel { get; set; }

    public ObservableCollection<EditorViewModel> Tabs { get; set; } = [];
    private readonly DropOutStack<EditorViewModel> _closedTabs = new(20);

    public ICommand CloseCurrentTabCommand { get; }
    public ICommand ReopenTabCommand { get; }

    [Reactive]
    public KeyGesture CloseTabKeyGesture { get; set; }
    [Reactive]
    public KeyGesture ReopenTabKeyGesture { get; set; }

    public EditorTabsPanelViewModel(MainWindowViewModel mainWindow, Project project, ILogger log)
    {
        CloseCurrentTabCommand = ReactiveCommand.Create(CloseCurrentTab);
        ReopenTabCommand = ReactiveCommand.Create(ReopenTab);

        CloseTabKeyGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.W);
        ReopenTabKeyGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.T, KeyModifiers.Shift);

        MainWindow = mainWindow;
        _project = project;
        _log = log;
    }

    public void OpenTab(ItemDescription item)
    {
        foreach (EditorViewModel tab in Tabs)
        {
            if (tab.Description.DisplayName.Equals(item.DisplayName))
            {
                SelectedTab = tab;
                ShowTabsPanel = Tabs.Any();
                return;
            }
        }

        EditorViewModel newTab = CreateTab(item);
        if (newTab is not null)
        {
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }
        ShowTabsPanel = Tabs.Any();
    }

    private void CloseCurrentTab()
    {
        Tabs.Remove(SelectedTab);
        ShowTabsPanel = Tabs.Any();
    }

    private void ReopenTab()
    {
        EditorViewModel mostRecentTab = _closedTabs.Pop();
        if (mostRecentTab is not null)
        {
            Tabs.Insert(mostRecentTab.ClosedIndex > Tabs.Count ? Tabs.Count : mostRecentTab.ClosedIndex, mostRecentTab);
            SelectedTab = mostRecentTab;
        }
        ShowTabsPanel = Tabs.Any();
    }

    private EditorViewModel CreateTab(ItemDescription item)
    {
        switch (item.Type)
        {
            case ItemDescription.ItemType.Background:
                return new BackgroundEditorViewModel((BackgroundItem)item, MainWindow, _project, _log);
            case ItemDescription.ItemType.BGM:
#if !WINDOWS
                try
                {
#endif
                    return new BackgroundMusicEditorViewModel((BackgroundMusicItem)item, MainWindow, _project, _log);
#if !WINDOWS
                }
                catch (NAudio.Sdl2.Structures.SdlException ex)
                {
                    _log.LogWarning($"SDL Exception: {ex.Message}\n{ex.StackTrace}");
                    _log.LogError(Strings.SdlExceptionTooManyDevicesText);
                    return null;
                }
#endif
            case ItemDescription.ItemType.Character:
                return new CharacterEditorViewModel((CharacterItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Character_Sprite:
                return new CharacterSpriteEditorViewModel((CharacterSpriteItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Chess_Puzzle:
                return new ChessPuzzleEditorViewModel((ChessPuzzleItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Chibi:
                return new ChibiEditorViewModel((ChibiItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Group_Selection:
                return new GroupSelectionEditorViewModel((GroupSelectionItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Item:
                return new ItemEditorViewModel((ItemItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Layout:
                return new LayoutEditorViewModel((LayoutItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Map:
                return new MapEditorViewModel((MapItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Place:
                return new PlaceEditorViewModel((PlaceItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Puzzle:
                return new PuzzleEditorViewModel((PuzzleItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Scenario:
                return new ScenarioEditorViewModel((ScenarioItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Script:
                return new ScriptEditorViewModel((ScriptItem)item, MainWindow, _log);
            case ItemDescription.ItemType.SFX:
#if !WINDOWS
                try
                {
#endif
                    return new SfxEditorViewModel((SfxItem)item, MainWindow, _log);
#if !WINDOWS
                }
                catch (NAudio.Sdl2.Structures.SdlException ex)
                {
                    _log.LogWarning($"SDL Exception: {ex.Message}\n{ex.StackTrace}");
                    _log.LogError(Strings.SdlExceptionTooManyDevicesText);
                    return null;
                }
#endif
            case ItemDescription.ItemType.System_Texture:
                return new SystemTextureEditorViewModel((SystemTextureItem)item, MainWindow, _project, _log);
            case ItemDescription.ItemType.Topic:
                return new TopicEditorViewModel((TopicItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Voice:
#if !WINDOWS
                try
                {
#endif
                    return new VoicedLineEditorViewModel((VoicedLineItem)item, MainWindow, _log);
#if !WINDOWS
                }
                catch (NAudio.Sdl2.Structures.SdlException ex)
                {
                    _log.LogWarning($"SDL Exception: {ex.Message}\n{ex.StackTrace}");
                    _log.LogError(Strings.SdlExceptionTooManyDevicesText);
                    return null;
                }
#endif
            case ItemDescription.ItemType.Save:
                return new SaveEditorViewModel((SaveItem)item, MainWindow, _log);
            default:
                _log.LogError(Strings.ErrorInvalidItemType);
                return null;
        }
    }

    public async Task OnTabClosed(EditorViewModel closedEditor, int closedIndex)
    {
        if (closedEditor.Description.Type is ItemDescription.ItemType.BGM or ItemDescription.ItemType.Voice or ItemDescription.ItemType.SFX)
        {
            switch (closedEditor.Description.Type)
            {
                case ItemDescription.ItemType.BGM:
                    ((BackgroundMusicEditorViewModel)closedEditor).BgmPlayer.Dispose();
                    break;
                case ItemDescription.ItemType.SFX:
                    ((SfxEditorViewModel)closedEditor).SfxPlayerPanel.Dispose();
                    break;
                case ItemDescription.ItemType.Voice:
                    ((VoicedLineEditorViewModel)closedEditor).VcePlayer.Dispose();
                    break;
            }
        }
        else if (closedEditor.Description.Type == ItemDescription.ItemType.Character_Sprite)
        {
            ((CharacterSpriteEditorViewModel)closedEditor).AnimatedImage.Stop();
        }
        else if (closedEditor.Description.Type == ItemDescription.ItemType.Save)
        {
            ButtonResult result = await MainWindow.Window.ShowMessageBoxAsync(Strings.SaveEditorSaveChangesPromptTitle,
                Strings.SaveEditorSaveChangesPrompt,
                ButtonEnum.YesNoCancel, Icon.Question, _log);
            SaveEditorViewModel saveEditor = (SaveEditorViewModel)closedEditor;
            switch (result)
            {
                case ButtonResult.Yes:
                    File.WriteAllBytes(saveEditor.Save.SaveLoc, saveEditor.Save.Save.GetBytes());
                    _project.Items.Remove(saveEditor.Save);
                    break;
                case ButtonResult.No:
                    _project.Items.Remove(saveEditor.Save);
                    break;
                default:
                case ButtonResult.Cancel:
                    OpenTab(saveEditor.Save);
                    break;
            }
        }

        closedEditor.ClosedIndex = closedIndex;
        _closedTabs.Push(closedEditor);
        ShowTabsPanel = Tabs.Any();
    }
}
