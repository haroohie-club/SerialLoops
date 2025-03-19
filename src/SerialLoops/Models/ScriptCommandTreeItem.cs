using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Utility;
using SoftCircuits.Collections;

namespace SerialLoops.Models;

public class ScriptCommandTreeItem : ITreeItem, IViewFor<ScriptItemCommand>
{
    private TextBlock _textBlock = new();
    StackPanel _panel = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 3,
        Margin = new(2),
    };

    public string Text { get; set; }
    public Avalonia.Svg.Svg Icon { get; set; } = null;
    public ObservableCollection<ITreeItem> Children { get; set; } = null;
    public bool IsExpanded { get; set; } = false;

    public ScriptCommandTreeItem(ScriptItemCommand command, ILogger log)
    {
        ViewModel = command;
        this.OneWayBind(ViewModel, vm => vm.Display, v => v._textBlock.Text);
        _textBlock.VerticalAlignment = VerticalAlignment.Center;
        _panel.Children.Add(_textBlock);
        _panel[ToolTip.TipProperty] = Shared.GetScriptVerbHelp(ViewModel?.Verb ?? EventFile.CommandVerb.NOOP1);
        _panel.ContextMenu = new();
        _panel.ContextMenu.Items.Add(new MenuItem
        {
            Header = Strings.ScriptCommandSetQuickSaveText,
            Command = ReactiveCommand.Create(() =>
            {
                if (ViewModel?.Project.ProjectSaveFile is null)
                {
                    if (!(ViewModel?.Project.LoadProjectSave() ?? false))
                    {
                        log.LogError("");
                        return;
                    }
                }

                ScriptItem script = (ScriptItem)ViewModel.Project.Items.First(i =>
                    i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == ViewModel.Script.Index);
                List<ItemDescription> references = script.GetReferencesTo(ViewModel.Project);
                ScenarioItem scenarioRef = (ScenarioItem)references.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Scenario);
                GroupSelectionItem groupSelectionRef = (GroupSelectionItem)references.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Group_Selection);
                TopicItem topicRef = (TopicItem)references.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic);

                QuickSaveSlotData quickSave = ViewModel.Project.ProjectSaveFile!.Save.QuickSaveSlot;
                quickSave.SaveTime = DateTimeOffset.Now;

                if (scenarioRef is null && groupSelectionRef is null && topicRef is null)
                {
                    log.LogWarning($"Unable to find references to current script '{ViewModel.Script.Name}' (0x{ViewModel.Script.Index:X3}' within other items. Could be dangerous!");
                    quickSave.ScenarioPosition = 1;
                }
                else
                {
                    if (scenarioRef is not null)
                    {
                        quickSave.ScenarioPosition = (short)scenarioRef.Scenario.Commands.FindIndex(c =>
                            c.Verb == ScenarioCommand.ScenarioVerb.LOAD_SCENE && c.Parameter == ViewModel.Script.Index);
                    }
                    else if (groupSelectionRef is not null)
                    {
                        quickSave.ScenarioPosition = (short)ViewModel.Project.Scenario.Commands.FindIndex(c =>
                            c.Verb == ScenarioCommand.ScenarioVerb.ROUTE_SELECT && c.Parameter == groupSelectionRef.Index);
                    }
                    else
                    {
                        PuzzleItem puzzleRef = (PuzzleItem)topicRef.GetReferencesTo(ViewModel.Project).FirstOrDefault(i => i.Type == ItemDescription.ItemType.Puzzle);
                        if (puzzleRef is null)
                        {
                            log.LogWarning($"Current script '{ViewModel.Script.Name}' (0x{ViewModel.Script.Index:X3}' is referenced by topic {topicRef.Name} ({topicRef.DisplayName}, but this topic is not referenced by any puzzles. Could be dangerous!");
                            quickSave.ScenarioPosition = 1;
                        }
                        else
                        {
                            quickSave.ScenarioPosition = (short)ViewModel.Project.Scenario.Commands.FindIndex(c =>
                                c.Verb == ScenarioCommand.ScenarioVerb.PUZZLE_PHASE && c.Parameter == puzzleRef.Puzzle.Index);
                        }
                    }
                }
                quickSave.EpisodeNumber = 1;
                quickSave.CurrentScript = ViewModel.Script.Index;
                quickSave.CurrentScriptBlock = ViewModel.Script.ScriptSections.IndexOf(ViewModel.Section);
                quickSave.CurrentScriptCommand = ViewModel.Index;

                quickSave.KbgIndex = (short)(ViewModel.CurrentPreview.Kbg?.Id ?? 0);
                quickSave.Place = (short)(ViewModel.CurrentPreview.Place?.Index ?? 0);
                if (ViewModel.CurrentPreview.Background.BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_BG)
                {
                    quickSave.BgIndex = (short)ViewModel.CurrentPreview.Background.Id;
                    quickSave.CgIndex = 0;
                }
                else
                {
                    OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commandTree = script.GetScriptCommandTree(ViewModel.Project, log);
                    ScriptItemCommand currentCommand = commandTree[ViewModel.Script.ScriptSections[quickSave.CurrentScriptBlock]][ViewModel.Index];
                    List<ScriptItemCommand> commands = currentCommand.WalkCommandGraph(commandTree, script.Graph);
                    for (int i = commands.Count - 1; i >= 0; i--)
                    {
                        if (commands[i].Verb == EventFile.CommandVerb.BG_DISP || commands[i].Verb == EventFile.CommandVerb.BG_DISP2 || (commands[i].Verb == EventFile.CommandVerb.BG_FADE && ((BgScriptParameter)commands[i].Parameters[0]).Background is not null))
                        {
                            quickSave.BgIndex = (short)((BgScriptParameter)commands[i].Parameters[0]).Background.Id;
                        }
                    }
                    quickSave.CgIndex = (short)ViewModel.CurrentPreview.Background.Id;
                }
                quickSave.BgPalEffect = (short)ViewModel.CurrentPreview.BgPalEffect;
                quickSave.EpisodeHeader = ViewModel.CurrentPreview.EpisodeHeader;
                for (int i = 1; i <= 5; i++)
                {
                    if (ViewModel.CurrentPreview.TopScreenChibis.Any(c => c.Chibi.TopScreenIndex == i))
                    {
                        quickSave.TopScreenChibis |= (CharacterMask)(1 << i);
                    }
                }
                quickSave.FirstCharacterSprite = ViewModel.CurrentPreview.Sprites.ElementAtOrDefault(0).Sprite?.Index ?? 0;
                quickSave.SecondCharacterSprite = ViewModel.CurrentPreview.Sprites.ElementAtOrDefault(1).Sprite?.Index ?? 0;
                quickSave.ThirdCharacterSprite = ViewModel.CurrentPreview.Sprites.ElementAtOrDefault(2).Sprite?.Index ?? 0;
                quickSave.Sprite1XOffset = (short)(ViewModel.CurrentPreview.Sprites.ElementAtOrDefault(0).Positioning?.X ?? 0);
                quickSave.Sprite2XOffset = (short)(ViewModel.CurrentPreview.Sprites.ElementAtOrDefault(1).Positioning?.X ?? 0);
                quickSave.Sprite3XOffset = (short)(ViewModel.CurrentPreview.Sprites.ElementAtOrDefault(2).Positioning?.X ?? 0);

                File.WriteAllBytes(ViewModel.Project.ProjectSaveFile.SaveLoc, ViewModel.Project.ProjectSaveFile.Save.GetBytes());
            }),
        });
    }

    public Control GetDisplay()
    {
        return _panel;
    }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (ScriptItemCommand)value;
    }

    public ScriptItemCommand ViewModel { get; set; }
}
