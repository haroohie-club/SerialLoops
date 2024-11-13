using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Platform;
using AvaloniaEdit.Utils;
using DynamicData;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors.ScriptCommandEditors;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using SoftCircuits.Collections;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.ViewModels.Editors;

public class ScriptEditorViewModel : EditorViewModel
{
    private ScriptItem _script;
    private ScriptItemCommand _selectedCommand;
    private OrderedDictionary<ScriptSection, List<ScriptItemCommand>> _commands = [];

    public ICommand AddScriptCommandCommand { get; }
    public ICommand AddScriptSectionCommand { get; }
    public ICommand DeleteScriptCommandOrSectionCommand { get; }
    public ICommand ClearScriptCommand { get; }

    public ScriptItemCommand SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCommand, value);

            UpdateCommandViewModel();
            UpdatePreview();
        }
    }

    [Reactive]
    public ReactiveScriptSection SelectedSection { get; set; }
    [Reactive]
    public ITreeItem SelectedTreeItem { get; set; }

    [Reactive]
    public SKBitmap PreviewBitmap { get; set; }
    [Reactive]
    public ScriptCommandEditorViewModel CurrentCommandViewModel { get; set; }

    public ObservableCollection<ReactiveScriptSection> ScriptSections { get; }

    public OrderedDictionary<ScriptSection, List<ScriptItemCommand>> Commands
    {
        get => _commands;
        set
        {
            this.RaiseAndSetIfChanged(ref _commands, value);
            Source = new(_commands.Keys.Select(s => new ScriptSectionTreeItem(ScriptSections[_script.Event.ScriptSections.IndexOf(s)], _commands[s])))
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<ITreeItem>(
                        new TemplateColumn<ITreeItem>(null,
                            new FuncDataTemplate<ITreeItem>((val, namescope) => val?.GetDisplay()),
                            options: new() { IsTextSearchEnabled = true }),
                        i => i.Children
                    ),
                },
            };

            Source.RowSelection!.SingleSelect = true;
            Source.RowSelection.SelectionChanged += RowSelection_SelectionChanged;
            Source.ExpandAll();
        }
    }

    [Reactive]
    public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }

    public ScriptEditorViewModel(ScriptItem script, MainWindowViewModel window, ILogger log) : base(script, window, log)
    {
        _script = script;
        ScriptSections = new(script.Event.ScriptSections.Select(s => new ReactiveScriptSection(s)));
        _project = window.OpenProject;
        PopulateScriptCommands();
        _script.CalculateGraphEdges(_commands, _log);
        foreach (ReactiveScriptSection section in ScriptSections)
        {
            section.SetCommands(_commands[section.Section]);
        }
        Source.ExpandAll();
        AddScriptCommandCommand = ReactiveCommand.CreateFromTask(AddCommand);
        AddScriptSectionCommand = ReactiveCommand.CreateFromTask(AddSection);
        DeleteScriptCommandOrSectionCommand = ReactiveCommand.CreateFromTask(Delete);
        ClearScriptCommand = ReactiveCommand.CreateFromTask(Clear);
    }

    public void PopulateScriptCommands(bool refresh = false)
    {
        if (refresh)
        {
            _script.Refresh(_project, _log);
        }

        Commands = _script.GetScriptCommandTree(_project, _log);
    }

    private void UpdateCommandViewModel()
    {
        if (_selectedCommand is null)
        {
            CurrentCommandViewModel = null;
        }
        else
        {
            CurrentCommandViewModel = _selectedCommand.Verb switch
            {
                CommandVerb.INIT_READ_FLAG => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.DIALOGUE => new DialogueScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.KBG_DISP => new KbgDispScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.PIN_MNL => new PinMnlScriptCommandEditorViewModel(_selectedCommand, this, _log, _window.OpenProject),
                CommandVerb.BG_DISP => new BgDispScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.SCREEN_FADEIN => new ScreenFadeInScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SCREEN_FADEOUT => new ScreenFadeOutScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SCREEN_FLASH => new ScreenFlashScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SND_PLAY => new SndPlayScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.REMOVED => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SND_STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.BGM_PLAY => new BgmPlayScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.VCE_PLAY => new VcePlayScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.FLAG => new FlagScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.TOPIC_GET => new TopicGetScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.TOGGLE_DIALOGUE => new ToggleDialogueScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SELECT => new SelectScriptCommandEditorViewModel(_selectedCommand, this, _log, _window.OpenProject),
                CommandVerb.SCREEN_SHAKE => new ScreenShakeScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SCREEN_SHAKE_STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.GOTO => new GotoScriptCommandEditorViewModel(_selectedCommand, this, _log, _window.OpenProject),
                CommandVerb.SCENE_GOTO => new SceneGotoScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.WAIT => new WaitScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.HOLD => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.NOOP1 => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.VGOTO => new VgotoScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.HARUHI_METER => new HaruhiMeterScriptCommandEditorViewModel(_selectedCommand, this, _log, noShow: false),
                CommandVerb.HARUHI_METER_NOSHOW => new HaruhiMeterScriptCommandEditorViewModel(_selectedCommand, this, _log, noShow: true),
                CommandVerb.PALEFFECT => new PalEffectScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.BACK => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.NOOP2 => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.INVEST_END => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.NEXT_SCENE => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.AVOID_DISP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SCENE_GOTO_CHESS => new SceneGotoScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.BG_DISP2 => new BgDispScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                _ => new(_selectedCommand, this, _log)
            };
        }
    }

    public void UpdatePreview()
    {
        try
        {
            if (_selectedCommand is null)
            {
                PreviewBitmap = null;
            }
            else
            {
                (SKBitmap previewBitmap, string errorImage) =
                    _script.GeneratePreviewImage(_commands, SelectedCommand, _project, _log);
                if (previewBitmap is null)
                {
                    previewBitmap = new(256, 384);
                    SKCanvas canvas = new(previewBitmap);
                    canvas.DrawColor(SKColors.Black);
                    using Stream noPreviewStream = AssetLoader.Open(new(errorImage));
                    canvas.DrawImage(SKImage.FromEncodedData(noPreviewStream), new SKPoint(0, 0));
                    canvas.Flush();
                }

                PreviewBitmap = previewBitmap;
            }
        }
        catch (Exception ex)
        {
            _log.LogException("Failed to update preview!", ex);
        }
    }

    private void RowSelection_SelectionChanged(object sender, TreeSelectionModelSelectionChangedEventArgs<ITreeItem> e)
    {
        if (e.SelectedIndexes.Count == 0 || e.SelectedIndexes[0].Count == 0)
        {
            SelectedCommand = null;
            SelectedSection = null;
        }

        if (e.SelectedIndexes[0].Count > 1)
        {
            SelectedCommand = Commands[Commands.Keys.ElementAt(e.SelectedIndexes[0][0])][e.SelectedIndexes[0][1]];
            SelectedSection = null;
        }
        else
        {
            SelectedCommand = null;
            SelectedSection = ScriptSections[e.SelectedIndexes[0][0]];
        }
    }

    private async Task AddCommand()
    {
        if (SelectedCommand is null && SelectedSection is null)
        {
            return;
        }

        CommandVerb? newVerb =
            await new AddScriptCommandDialog() { DataContext = new AddScriptCommandDialogViewModel() }
                .ShowDialog<CommandVerb?>(_window.Window);
        if (newVerb is null)
        {
            return;
        }

        ScriptCommandInvocation invocation =
            new(CommandsAvailable.Find(command => command.Mnemonic.Equals(newVerb.ToString())));
        invocation.InitializeWithDefaultValues(_script.Event, _project);
        ScriptItemCommand newCommand;
        if (SelectedCommand is null)
        {
            newCommand =
                ScriptItemCommand.FromInvocation(
                    invocation,
                    SelectedSection.Section,
                    0,
                    _script.Event,
                    _project,
                    Strings.ResourceManager.GetString,
                    _log
                );
            SelectedSection.InsertCommand(newCommand.Index, newCommand, Commands);
        }
        else
        {
            newCommand =
                ScriptItemCommand.FromInvocation(
                    invocation,
                    SelectedCommand.Section,
                    SelectedCommand.Index + 1,
                    _script.Event,
                    _project,
                    Strings.ResourceManager.GetString,
                    _log
                );
            ScriptSections[_script.Event.ScriptSections.IndexOf(SelectedCommand.Section)].InsertCommand(newCommand.Index, newCommand, Commands);
        }

        SelectedCommand = newCommand;
        Source.RowSelection?.Select(new(_script.Event.ScriptSections.IndexOf(SelectedCommand.Section), SelectedCommand.Index));

        _script.Refresh(_project, _log);
        _script.UnsavedChanges = true;
    }

    private async Task AddSection()
    {
        string sectionName = await new AddScriptSectionDialog() { DataContext = new AddScriptSectionDialogViewModel() }
            .ShowDialog<string>(_window.Window);
        if (string.IsNullOrEmpty(sectionName))
        {
            return;
        }

        sectionName = $"NONE{sectionName}";
        if (ScriptSections.Any(s => s.Name.Equals(sectionName)))
        {
            await _window.Window.ShowMessageBoxAsync(Strings.Duplicate_Section_Name,
                Strings.Section_name_already_exists__Please_pick_a_different_name_for_this_section_,
                ButtonEnum.Ok, Icon.Warning, _log);
            return;
        }

        int sectionIndex = 1;
        if (SelectedCommand is not null)
        {
            sectionIndex = _script.Event.ScriptSections.IndexOf(SelectedCommand.Section) + 1;
        }
        else if (SelectedSection is not null)
        {
            sectionIndex = ScriptSections.IndexOf(SelectedSection) + 1;
        }

        ScriptSection section = new()
        {
            Name = sectionName,
            CommandsAvailable = CommandsAvailable,
            SectionType = typeof(ScriptSection),
            ObjectType = typeof(ScriptCommandInvocation),
        };
        ReactiveScriptSection reactiveSection = new(section);

        _script.Event.ScriptSections.Insert(sectionIndex, section);
        _script.Event.NumSections++;
        _script.Event.LabelsSection.Objects.Insert(sectionIndex,
            new()
            {
                Name = $"NONE/{sectionName[4..]}",
                Id = (short)(sectionIndex == _script.Event.LabelsSection.Objects.Count ?
                    _script.Event.LabelsSection.Objects[^1].Id + 1 :
                    _script.Event.LabelsSection.Objects[sectionIndex + 1].Id),
            }
        );
        for (int i = sectionIndex + 1; i < _script.Event.LabelsSection.Objects.Count; i++)
        {
            _script.Event.LabelsSection.Objects[i].Id++;
        }

        ScriptSections.Insert(sectionIndex, reactiveSection);
        _script.Refresh(_project, _log);
        _commands.Insert(sectionIndex, section, []);
        // This forces a complete refresh of the hierarchical tree. This is not as performant as
        // inserting directly into the collection, but is fine since users will not be adding sections
        // as frequently as commands (and is necessary with our current architecture)
        Commands = _commands;
        _script.UnsavedChanges = true;
    }

    private async Task Delete()
    {
        if (SelectedCommand is null && SelectedSection is null)
        {
            return;
        }

        if (SelectedCommand is not null)
        {
            int index = SelectedCommand.Index;
            SelectedCommand = index == 0 ? Commands[SelectedCommand.Section][1] : Commands[SelectedCommand.Section][index - 1];
            Source.RowSelection?.Select(new(_script.Event.ScriptSections.IndexOf(SelectedCommand.Section), SelectedCommand.Index));
            ScriptSections[_script.Event.ScriptSections.IndexOf(SelectedCommand.Section)].DeleteCommand(index, Commands);
        }
        else
        {
            int sectionIndex = ScriptSections.IndexOf(SelectedSection);
            if (sectionIndex == 0)
            {
                await _window.Window.ShowMessageBoxAsync(Strings.Cannot_Delete_Root_Section_,
                    Strings.The_root_section_cannot_be_deleted_, ButtonEnum.Ok,
                    Icon.Warning, _log);
                return;
            }

            SelectedSection = null;
            SelectedCommand = Commands[_script.Event.ScriptSections[sectionIndex - 1]][^1];
            Source.RowSelection?.Select(new(_script.Event.ScriptSections.IndexOf(SelectedCommand.Section), SelectedCommand.Index));

            _script.Event.ScriptSections.RemoveAt(sectionIndex);
            _script.Event.NumSections--;
            _script.Event.LabelsSection.Objects.RemoveAt(sectionIndex);
            for (int i = sectionIndex; i < _script.Event.LabelsSection.Objects.Count; i++)
            {
                _script.Event.LabelsSection.Objects[i].Id--;
            }

            ScriptSections.RemoveAt(sectionIndex);

            _commands.RemoveAt(sectionIndex);
            // This forces a complete refresh of the hierarchical tree. This is not as performant as
            // inserting directly into the collection, but is fine since users will not be adding sections
            // as frequently as commands (and is necessary with our current architecture)
            Commands = _commands;
        }

        _script.Refresh(_project, _log);
        _script.UnsavedChanges = true;
    }

    private async Task Clear()
    {
        if (await _window.Window.ShowMessageBoxAsync(Strings.Clear_Script_,
                Strings.Are_you_sure_you_want_to_clear_the_script__nThis_action_is_irreversible_,
                ButtonEnum.YesNo, Icon.Question, _log) != ButtonResult.Yes)
        {
            return;
        };

        Source.RowSelection?.Select(new());
        SelectedSection = null;
        SelectedCommand = null;
        _script.Event.ScriptSections.Clear();
        _script.Event.ScriptSections.Add(new()
        {
            Name = "SCRIPT00",
            CommandsAvailable = CommandsAvailable,
            SectionType = typeof(ScriptSection),
            ObjectType = typeof(ScriptCommandInvocation),
        });
        _script.Event.LabelsSection.Objects.Clear();
        ScriptSections.Clear();
        ScriptSections.Add(new(_script.Event.ScriptSections[0]));
        _commands.Clear();
        _commands.Add(_script.Event.ScriptSections[0], []);
        Commands = _commands;

        _script.Refresh(_project, _log);
        _script.UnsavedChanges = true;
    }
}

public class ReactiveScriptSection(ScriptSection section) : ReactiveObject
{
    public ScriptSection Section { get; } = section;

    private string _name = section.Name;

    public string Name
    {
        get => _name;
        set
        {
            this.RaiseAndSetIfChanged(ref _name, value);
            Section.Name = _name;
        }
    }

    public ObservableCollection<ITreeItem> Commands { get; private set; } = [];

    public void InsertCommand(int index, ScriptItemCommand command, OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commands)
    {
        Commands.Insert(index, new ScriptCommandTreeItem(command));
        Section.Objects.Insert(index, command.Invocation);
        commands[Section].Insert(index, command);
        for (int i = index + 1; i < commands[Section].Count; i++)
        {
            commands[Section][i].Index++;
        }
    }

    public void DeleteCommand(int index, OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commands)
    {
        Commands.RemoveAt(index);
        Section.Objects.RemoveAt(index);
        commands[Section].RemoveAt(index);
        for (int i = index; i < commands[Section].Count; i++)
        {
            commands[Section][i].Index--;
        }
    }

    internal void SetCommands(IEnumerable<ScriptItemCommand> commands)
    {
        Commands.Clear();
        ExtensionMethods.AddRange(Commands, commands.Select(c => new ScriptCommandTreeItem(c)));
    }
}
