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
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Models;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors.ScriptCommandEditors;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.ViewModels.Editors;

public class ScriptEditorViewModel : EditorViewModel
{
    private ScriptItem _script;
    private ScriptItemCommand _selectedCommand;
    private Dictionary<ScriptSection, List<ScriptItemCommand>> _commands = [];

    public ICommand SelectedCommandChangedCommand { get; }
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

    [Reactive] public ReactiveScriptSection SelectedSection { get; set; }

    [Reactive] public SKBitmap PreviewBitmap { get; set; }
    [Reactive] public ScriptCommandEditorViewModel CurrentCommandViewModel { get; set; }

    public ObservableCollection<ReactiveScriptSection> ScriptSections { get; }

    public Dictionary<ScriptSection, List<ScriptItemCommand>> Commands
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
                            options: new TemplateColumnOptions<ITreeItem>() { IsTextSearchEnabled = true }),
                        i => i.Children
                    )
                }
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
                CommandVerb.GOTO => new GotoScriptCommandEditorViewModel(_selectedCommand, this, _log),
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
        ScriptItemCommand newCommand;
        ScriptSection scriptSection;
        if (SelectedCommand is null)
        {
            scriptSection = SelectedSection.Section;
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
            scriptSection = SelectedCommand.Section;
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

        for (int i = newCommand.Index + 1; i < ScriptSections.Count; i++)
        {
            Commands[scriptSection][i].Index++;
        }
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

    public void InsertCommand(int index, ScriptItemCommand command, Dictionary<ScriptSection, List<ScriptItemCommand>> commands)
    {
        Commands.Insert(index, new ScriptCommandTreeItem(command));
        Section.Objects.Insert(index, command.Invocation);
        commands[Section].Insert(index, command);
    }

    public void SetCommands(IEnumerable<ScriptItemCommand> commands)
    {
        Commands.Clear();
        Commands.AddRange(commands.Select(c => new ScriptCommandTreeItem(c)));
    }
}
