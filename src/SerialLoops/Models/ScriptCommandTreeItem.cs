using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views;
using SerialLoops.Views.Dialogs;

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

    public ScriptCommandTreeItem(ScriptItemCommand command, ILogger log, MainWindow window)
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
            Command = ReactiveCommand.Create(SetQuickSaveHere),
        });
        _panel.ContextMenu.Items.Add(new MenuItem
        {
            Header = Strings.ScriptCommandSetQuickSaveAdvancedText,
            Command = ReactiveCommand.CreateFromTask(async Task () =>
            {
                if (!SetQuickSaveHere())
                {
                    return;
                }
                SaveSlotEditorDialogViewModel quickSaveEditorDialogVm = new(ViewModel.Project.ProjectSaveFile,
                    ViewModel.Project.ProjectSaveFile!.Save.QuickSaveSlot,
                    ViewModel.Project.ProjectSaveFile.Name, Strings.SaveEditorQuickSaveTab, ViewModel.Project, log, dontTreatAsQuickSave: true);
                await new SaveSlotEditorDialog { DataContext = quickSaveEditorDialogVm }.ShowDialog(window);
            }),
        });

        return;

        bool SetQuickSaveHere()
        {
            if (ViewModel?.Project.ProjectSaveFile is null)
            {
                if (!(ViewModel?.Project.LoadProjectSave() ?? false))
                {
                    log.LogError("");
                    return false;
                }
            }

            ScriptItem script = (ScriptItem)ViewModel.Project.Items.First(i =>
                i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == ViewModel.Script.Index);
            List<ItemDescription> references = script.GetReferencesTo(ViewModel.Project);
            ScenarioItem scenarioRef = (ScenarioItem)references.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Scenario);
            GroupSelectionItem groupSelectionRef = (GroupSelectionItem)references.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Group_Selection);

            QuickSaveSlotData quickSave = ViewModel.Project.ProjectSaveFile!.Save.QuickSaveSlot;
            quickSave.SaveTime = DateTimeOffset.Now;

            if (scenarioRef is not null)
            {
                quickSave.ScenarioPosition = (short)(scenarioRef.Scenario.Commands.FindIndex(c =>
                    c.Verb == ScenarioCommand.ScenarioVerb.LOAD_SCENE && c.Parameter == ViewModel.Script.Index) + 1);
            }
            else if (groupSelectionRef is not null)
            {
                quickSave.ScenarioPosition = (short)(ViewModel.Project.Scenario.Commands.FindIndex(c =>
                    c.Verb == ScenarioCommand.ScenarioVerb.ROUTE_SELECT && c.Parameter == groupSelectionRef.Index) + 1);
            }
            else
            {
                log.LogWarning($"Unable to find references to current script '{ViewModel.Script.Name}' (0x{ViewModel.Script.Index:X3}' within other scenario. Could be dangerous!");
                quickSave.ScenarioPosition = 1;
            }
            quickSave.EpisodeNumber = 1;
            quickSave.CurrentScript = ViewModel.Script.Index;
            quickSave.CurrentScriptBlock = ViewModel.Script.ScriptSections.IndexOf(ViewModel.Section);
            quickSave.CurrentScriptCommand = ViewModel.Index;

            quickSave.ApplyScriptPreview(ViewModel.CurrentPreview, script, ViewModel.Index, ViewModel.Project, log);

            File.WriteAllBytes(ViewModel.Project.ProjectSaveFile.SaveLoc, ViewModel.Project.ProjectSaveFile.Save.GetBytes());

            return true;
        }
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
