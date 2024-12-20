﻿using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.SaveFile;
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

    public ICommand TabSwitchedCommand { get; set; }

    public ObservableCollection<EditorViewModel> Tabs { get; set; } = [];

    public EditorTabsPanelViewModel(MainWindowViewModel mainWindow, Project project, ILogger log)
    {
        TabSwitchedCommand = ReactiveCommand.Create(OnTabSwitched);
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
                return;
            }
        }

        EditorViewModel newTab = CreateTab(item);
        if (newTab is not null)
        {
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }
    }

    private EditorViewModel CreateTab(ItemDescription item)
    {
        switch (item.Type)
        {
            case ItemDescription.ItemType.Background:
                return new BackgroundEditorViewModel((BackgroundItem)item, MainWindow, _project, _log);
            case ItemDescription.ItemType.BGM:
                return new BackgroundMusicEditorViewModel((BackgroundMusicItem)item, MainWindow, _project, _log);
            case ItemDescription.ItemType.Scenario:
                return new ScenarioEditorViewModel((ScenarioItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Character_Sprite:
                return new CharacterSpriteEditorViewModel((CharacterSpriteItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Chess_Puzzle:
                return new ChessPuzzleEditorViewModel((ChessPuzzleItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Group_Selection:
                return new GroupSelectionEditorViewModel((GroupSelectionItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Script:
                return new ScriptEditorViewModel((ScriptItem)item, MainWindow, _log);
            case ItemDescription.ItemType.SFX:
                return new SfxEditorViewModel((SfxItem)item, MainWindow, _log);
            case ItemDescription.ItemType.System_Texture:
                return new SystemTextureEditorViewModel((SystemTextureItem)item, MainWindow, _project, _log);
            case ItemDescription.ItemType.Topic:
                return new TopicEditorViewModel((TopicItem)item, MainWindow, _log);
            case ItemDescription.ItemType.Voice:
                return new VoicedLineEditorViewModel((VoicedLineItem)item, MainWindow, _log);
            default:
                _log.LogError(Strings.Invalid_item_type_);
                return null;
        }
    }

    private void OnTabSwitched()
    {

    }

    public async Task OnTabClosed(EditorViewModel closedEditor)
    {
        if (closedEditor.Description.Type == ItemDescription.ItemType.BGM)
        {
            ((BackgroundMusicEditorViewModel)closedEditor).BgmPlayer.Stop();
        }
        else if (closedEditor.Description.Type == ItemDescription.ItemType.Character_Sprite)
        {
            ((CharacterSpriteEditorViewModel)closedEditor).AnimatedImage.Stop();
        }
        else if (closedEditor.Description.Type == ItemDescription.ItemType.SFX)
        {
            ((SfxEditorViewModel)closedEditor).SfxPlayerPanel.Stop();
        }
        else if (closedEditor.Description.Type == ItemDescription.ItemType.Voice)
        {
            ((VoicedLineEditorViewModel)closedEditor).VcePlayer.Stop();
        }
    }

    public void OnTabMiddleClicked()
    {
        Tabs.Remove(SelectedTab);
    }
}
