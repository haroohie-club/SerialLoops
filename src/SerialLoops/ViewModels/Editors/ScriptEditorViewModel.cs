using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
<<<<<<< HEAD
using System.Text.Json;
using System.Threading.Tasks;
=======
>>>>>>> Avalonia
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
<<<<<<< HEAD
using Avalonia.Input;
using Avalonia.Platform;
using AvaloniaEdit.Utils;
using DynamicData;
=======
using Avalonia.Platform;
>>>>>>> Avalonia
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
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
    public ICommand CutCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand ApplyTemplateCommand { get; }
    public ICommand GenerateTemplateCommand { get; }

    public KeyGesture AddCommandHotKey { get; }
    public KeyGesture CutHotKey { get; }
    public KeyGesture CopyHotKey { get; }
    public KeyGesture PasteHotKey { get; }
    public KeyGesture DeleteHotKey { get; }

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
<<<<<<< HEAD

    [Reactive]
    public ReactiveScriptSection SelectedSection { get; set; }
=======
>>>>>>> Avalonia

    [Reactive] public ScriptSection SelectedSection { get; set; }

    [Reactive] public SKBitmap PreviewBitmap { get; set; }
    [Reactive] public ScriptCommandEditorViewModel CurrentCommandViewModel { get; set; }

    public ObservableCollection<ReactiveScriptSection> ScriptSections { get; }

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
<<<<<<< HEAD
                            options: new() { IsTextSearchEnabled = true }),
=======
                            options: new TemplateColumnOptions<ITreeItem>() { IsTextSearchEnabled = true }),
>>>>>>> Avalonia
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

    public enum ClipboardMode
    {
        None,
        Cut,
        Copy,
    }

    [Reactive]
    public ScriptItemCommand ClipboardCommand { get; set; }
    private ClipboardMode _clipboardMode = ClipboardMode.None;

    public Vector? ScrollPosition { get; set; }

    public ScriptEditorViewModel(ScriptItem script, MainWindowViewModel window, ILogger log) : base(script, window, log)
    {
        _script = script;
        ScriptSections = new(script.Event.ScriptSections.Select(s => new ReactiveScriptSection(s)));
        _project = window.OpenProject;
<<<<<<< HEAD
=======
        PopulateScriptCommands();
        _script.CalculateGraphEdges(_commands, _log);
    }

    public void PopulateScriptCommands(bool refresh = false)
    {
        if (refresh)
        {
            _script.Refresh(_project, _log);
        }

>>>>>>> Avalonia
        Commands = _script.GetScriptCommandTree(_project, _log);
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
        CutCommand = ReactiveCommand.Create(Cut);
        CopyCommand = ReactiveCommand.Create(Copy);
        PasteCommand = ReactiveCommand.Create(Paste);
        ApplyTemplateCommand = ReactiveCommand.CreateFromTask(ApplyTemplate);
        GenerateTemplateCommand = ReactiveCommand.CreateFromTask(GenerateTemplate);

        AddCommandHotKey = new(Key.N, KeyModifiers.Control | KeyModifiers.Shift);
        CutHotKey = new(Key.X, KeyModifiers.Control);
        CopyHotKey = new(Key.C, KeyModifiers.Control);
        PasteHotKey = new(Key.V, KeyModifiers.Control);
        DeleteHotKey = new(Key.Delete);
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
                CommandVerb.DIALOGUE => new DialogueScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.KBG_DISP => new KbgDispScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.PIN_MNL => new PinMnlScriptCommandEditorViewModel(_selectedCommand, this, _log, Window.OpenProject),
                CommandVerb.BG_DISP => new BgDispScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.SCREEN_FADEIN => new ScreenFadeInScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SCREEN_FADEOUT => new ScreenFadeOutScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SCREEN_FLASH => new ScreenFlashScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SND_PLAY => new SndPlayScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.REMOVED => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SND_STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.BGM_PLAY => new BgmPlayScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.VCE_PLAY => new VcePlayScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.FLAG => new FlagScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.TOPIC_GET => new TopicGetScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.TOGGLE_DIALOGUE => new ToggleDialogueScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SELECT => new SelectScriptCommandEditorViewModel(_selectedCommand, this, _log, Window.OpenProject),
                CommandVerb.SCREEN_SHAKE => new ScreenShakeScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SCREEN_SHAKE_STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.GOTO => new GotoScriptCommandEditorViewModel(_selectedCommand, this, _log, Window.OpenProject),
                CommandVerb.SCENE_GOTO => new SceneGotoScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
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
                CommandVerb.CHESS_LOAD => new ChessLoadScriptCommandEditorViewModel(_selectedCommand, this, _window, _log),
                CommandVerb.SCENE_GOTO_CHESS => new SceneGotoScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                CommandVerb.BG_DISP2 => new BgDispScriptCommandEditorViewModel(_selectedCommand, this, _log, _window),
                _ => new ScriptCommandEditorViewModel(_selectedCommand, this, _log)
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

    public void HandleDragDrop(ITreeItem source, ITreeItem target, TreeDataGridRowDropPosition position)
    {
        if (source is ScriptCommandTreeItem sourceCommandItem)
        {
            ScriptItemCommand sourceCommand = sourceCommandItem.ViewModel!;
            int targetSectionIndex, targetCommandIndex;
            if (target is ScriptCommandTreeItem targetCommand)
            {
                targetSectionIndex = _script.Event.ScriptSections.IndexOf(targetCommand.ViewModel!.Section);
                targetCommandIndex = position == TreeDataGridRowDropPosition.Before
                    ? sourceCommand.Section == targetCommand.ViewModel!.Section && targetCommand.ViewModel!.Index >= sourceCommand.Index ?
                        targetCommand.ViewModel!.Index - 1 : targetCommand.ViewModel.Index
                    : sourceCommand.Section == targetCommand.ViewModel!.Section && targetCommand.ViewModel!.Index >= sourceCommand.Index ?
                        targetCommand.ViewModel!.Index : targetCommand.ViewModel!.Index + 1;
            }
            else if (target is ScriptSectionTreeItem targetSection)
            {
                if (position == TreeDataGridRowDropPosition.Before && ScriptSections.IndexOf(targetSection.ViewModel!) > 0)
                {
                    targetSectionIndex = ScriptSections.IndexOf(targetSection.ViewModel!) - 1;
                    targetCommandIndex = ScriptSections[targetSectionIndex].Commands.Count;
                }
                else
                {
                    targetSectionIndex = ScriptSections.IndexOf(targetSection.ViewModel!);
                    targetCommandIndex = 0;
                }
            }
            else
            {
                return;
            }
            ScriptSections[_script.Event.ScriptSections.IndexOf(sourceCommand.Section)].DeleteCommand(sourceCommand.Index, Commands);
            ScriptSections[targetSectionIndex].InsertCommand(targetCommandIndex, sourceCommand, Commands);
            sourceCommand.Section = ScriptSections[targetSectionIndex].Section;
            sourceCommand.Index = targetCommandIndex;

            _script.Refresh(_project, _log);
            _script.UnsavedChanges = true;
        }
    }

    public bool CanDrag(ITreeItem source)
    {
        return !(source is ScriptSectionTreeItem);
    }

    private void RowSelection_SelectionChanged(object sender, TreeSelectionModelSelectionChangedEventArgs<ITreeItem> e)
    {
        if (e.SelectedIndexes.Count == 0 || e.SelectedIndexes[0].Count == 0)
        {
            SelectedCommand = null;
            SelectedSection = null;
            return;
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
                .ShowDialog<CommandVerb?>(Window.Window);
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
            .ShowDialog<string>(Window.Window);
        if (string.IsNullOrEmpty(sectionName))
        {
            return;
        }

        sectionName = $"NONE{sectionName}";
        if (ScriptSections.Any(s => s.Name.Equals(sectionName)))
        {
            await Window.Window.ShowMessageBoxAsync(Strings.Duplicate_Section_Name,
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
        _script.Event.LabelsSection.Objects.Insert(sectionIndex - 1,
            new()
            {
                Name = $"NONE/{sectionName[4..]}",
                Id = (short)(_script.Event.LabelsSection.Objects.Count == 0 ? 1001 :
                    sectionIndex == _script.Event.LabelsSection.Objects.Count ?
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
            ReactiveScriptSection selectedSection = ScriptSections[_script.Event.ScriptSections.IndexOf(SelectedCommand.Section)];
            SelectedCommand = index switch
            {
                0 => Commands[SelectedCommand.Section].Count == 1 ? null : Commands[SelectedCommand.Section][1],
                _ => Commands[SelectedCommand.Section][index - 1],
            };
            if (SelectedCommand is null)
            {
                SelectedSection = selectedSection;
            }
            Source.RowSelection?.Select(new(ScriptSections.IndexOf(selectedSection), index));
            selectedSection.DeleteCommand(index, Commands);
        }
        else
        {
            int sectionIndex = ScriptSections.IndexOf(SelectedSection);
            if (sectionIndex == 0)
            {
                await Window.Window.ShowMessageBoxAsync(Strings.Cannot_Delete_Root_Section_,
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
        if (await Window.Window.ShowMessageBoxAsync(Strings.Clear_Script_,
                Strings.Are_you_sure_you_want_to_clear_the_script__nThis_action_is_irreversible_,
                ButtonEnum.YesNo, Icon.Question, _log) != ButtonResult.Yes)
        {
            return;
        };

        Source.RowSelection?.Select(new());
        SelectedSection = null;
        SelectedCommand = null;
        _script.Event.NumSections -= _script.Event.ScriptSections.Count - 1;
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
        _script.Event.LabelsSection.Objects.Add(new() { Id = 1, Name = "" });
        _commands.Clear();
        _commands.Add(_script.Event.ScriptSections[0], []);
        Commands = _commands;

        _script.Event.DialogueLines.Clear();
        _script.Event.DialogueSection.Objects.Clear();
        _script.Event.ConditionalsSection.Objects.RemoveRange(1, _script.Event.ConditionalsSection.Objects.Count - 1);
        _script.Event.ChoicesSection.Objects.RemoveRange(1, _script.Event.ChoicesSection.Objects.Count - 1);
        _script.Event.StartingChibisSection.Objects.RemoveRange(1, _script.Event.StartingChibisSection.Objects.Count - 1);
        _script.Event.MapCharactersSection.Objects.RemoveRange(1, _script.Event.MapCharactersSection.Objects.Count - 1);

        _script.Refresh(_project, _log);
        _script.UnsavedChanges = true;
    }

    private void Cut()
    {
        if (SelectedCommand is null)
        {
            return;
        }
        ClipboardCommand = SelectedCommand;
        _clipboardMode = ClipboardMode.Cut;
    }

    private void Copy()
    {
        if (SelectedCommand is null)
        {
            return;
        }
        ClipboardCommand = SelectedCommand;
        _clipboardMode = ClipboardMode.Copy;
    }

    private void Paste()
    {
        if (ClipboardCommand is null)
        {
            return;
        }
        int pasteSectionIndex, pasteCommandIndex;
        if (SelectedCommand is not null)
        {
            pasteSectionIndex = _script.Event.ScriptSections.IndexOf(SelectedCommand.Section);
            pasteCommandIndex = SelectedCommand.Section == ClipboardCommand.Section && SelectedCommand.Index >= ClipboardCommand.Index && _clipboardMode != ClipboardMode.Copy ?
                SelectedCommand.Index :
                SelectedCommand.Index + 1;
        }
        else if (SelectedSection is not null)
        {
            pasteSectionIndex = ScriptSections.IndexOf(SelectedSection);
            pasteCommandIndex = 0;
        }
        else
        {
            return;
        }
        switch (_clipboardMode)
        {
            case ClipboardMode.Cut:
                ScriptSections[_script.Event.ScriptSections.IndexOf(ClipboardCommand.Section)].DeleteCommand(ClipboardCommand.Index, Commands);
                ScriptSections[pasteSectionIndex].InsertCommand(pasteCommandIndex, ClipboardCommand, Commands);
                ClipboardCommand.Section = ScriptSections[pasteSectionIndex].Section;
                ClipboardCommand.Index = pasteCommandIndex;

                _clipboardMode = ClipboardMode.None;
                ClipboardCommand = null;
                break;
            case ClipboardMode.Copy:
                ScriptItemCommand clonedCommand = ClipboardCommand.Clone();
                ScriptSections[pasteSectionIndex].InsertCommand(pasteCommandIndex, clonedCommand, Commands);
                clonedCommand.Section = ScriptSections[pasteSectionIndex].Section;
                clonedCommand.Index = pasteCommandIndex;

                // We propagate the cloned command to the clipboard to allow further pasting
                // while still making sure each instance of the command is unique
                ClipboardCommand = clonedCommand;
                break;
            case ClipboardMode.None:
            default:
                return;
        }

        _script.Refresh(_project, _log);
        _script.UnsavedChanges = true;
    }

    private async Task ApplyTemplate()
    {
        ScriptTemplate template = await new ScriptTemplateSelectorDialog { DataContext = new ScriptTemplateSelectorDialogViewModel(_project) }
                .ShowDialog<ScriptTemplate>(Window.Window);
        if (template is null)
        {
            return;
        }

        SelectedSection = null;
        SelectedCommand = null;
        Source.RowSelection?.Select(new());
        template.Apply(_script, _project, _log);
        ScriptSections.Clear();
        ScriptSections.AddRange(_script.Event.ScriptSections.Select(s => new ReactiveScriptSection(s)));
        Commands = _script.GetScriptCommandTree(_project, _log);
        foreach (ReactiveScriptSection section in ScriptSections)
        {
            section.SetCommands(_commands[section.Section]);
        }
        Source.ExpandAll();
    }

    private async Task GenerateTemplate()
    {
        ScriptTemplate template = await new GenerateTemplateDialog() { DataContext = new GenerateTemplateDialogViewModel(Commands, _project, _log) }
                .ShowDialog<ScriptTemplate>(Window.Window);
        if (template is null)
        {
            return;
        }

        Lib.IO.WriteStringFile(Path.Combine(_project.Config.ScriptTemplatesDirectory, $"{template.Name}.slscr"),
            JsonSerializer.Serialize(template), _log);
        _project.Config.ScriptTemplates.Add(template);
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

    public ObservableCollection<ScriptCommandInvocation> Commands { get; } = new(section.Objects);

    public void InsertCommand(int index, ScriptCommandInvocation command)
    {
        Commands.Insert(index, command);
        Section.Objects.Insert(index, command);
    }
}
