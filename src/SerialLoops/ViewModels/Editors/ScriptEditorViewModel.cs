using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using DynamicData;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors.ScriptCommandEditors;
using SerialLoops.Views;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using SoftCircuits.Collections;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.ViewModels.Editors;

public class ScriptEditorViewModel : EditorViewModel
{
    private readonly ScriptItem _script;
    private ScriptItemCommand _selectedCommand;
    private OrderedDictionary<ScriptSection, List<ScriptItemCommand>> _commands = [];

    public ICommand AddScriptCommandCommand { get; }
    public ICommand AddScriptSectionCommand { get; }
    public ICommand DeleteScriptCommandOrSectionCommand { get; }
    public ICommand ClearScriptCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand CutCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand ApplyTemplateCommand { get; }
    public ICommand GenerateTemplateCommand { get; }

    public ICommand AddStartingChibisCommand { get; }
    public ICommand RemoveStartingChibisCommand { get; }
    public ICommand AddChoiceCommand { get; }
    public ICommand AddInteractableObjectCommand { get; }
    public ICommand RemoveInteractableObjectCommand { get; }

    public KeyGesture AddCommandHotKey { get; set; }
    public KeyGesture CutHotKey { get; set; }
    public KeyGesture CopyHotKey { get; set; }
    public KeyGesture PasteHotKey { get; set; }
    public KeyGesture DeleteHotKey { get; set; }

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

    public ScriptPreviewCanvasViewModel ScriptPreviewCanvas { get; set; }

    [Reactive]
    public ScriptCommandEditorViewModel CurrentCommandViewModel { get; set; }

    public ObservableCollection<ReactiveScriptSection> ScriptSections { get; }

    public OrderedDictionary<ScriptSection, List<ScriptItemCommand>> Commands
    {
        get => _commands;
        set
        {
            this.RaiseAndSetIfChanged(ref _commands, value);
            Source = new(_commands?.Keys.Select(s => new ScriptSectionTreeItem(ScriptSections[_script.Event.ScriptSections.IndexOf(s)], _commands[s])) ?? [])
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

    [Reactive]
    public ChessPuzzleItem CurrentChessBoard { get; set; }
    public ObservableCollection<short> CurrentGuidePieces { get; } = [];
    public ObservableCollection<short> CurrentHighlightedSpaces { get; } = [];
    public ObservableCollection<short> CurrentCrossedSpaces { get; } = [];

    public ObservableCollection<StartingChibiWithImage> UnusedChibis { get; } = [];
    public ObservableCollection<StartingChibiWithImage> StartingChibis { get; } = [];

    [Reactive]
    public bool HasStartingChibis { get; set; }
    public MapCharactersSubEditorViewModel MapCharactersSubEditorVm { get; set; }

    public ObservableCollection<ReactiveInteractableObject> InteractableObjects { get; } = [];
    [Reactive]
    public ReactiveInteractableObject SelectedInteractableObject { get; set; }
    public ObservableCollection<ReactiveInteractableObject> UnusedInteractableObjects { get; } = [];

    public ObservableCollection<ReactiveChoice> Choices { get; } = [];

    [Reactive]
    public bool Script00TipVisible { get; set; }

    private Timer Script00TipTimer { get; set; }

    public ScriptEditorViewModel(ScriptItem script, MainWindowViewModel window, ILogger log) : base(script, window, log)
    {
        _script = script;
        ScriptSections = new(script.Event.ScriptSections.Select(s => new ReactiveScriptSection(s, log, Window.Window)));
        _project = window.OpenProject;
        ScriptPreviewCanvas = new(_project);
        Commands = _script.GetScriptCommandTree(_project, _log);
        _script.CalculateGraphEdges(_commands, _log);
        foreach (ReactiveScriptSection section in ScriptSections)
        {
            section.SetCommands(_commands[section.Section]);
        }
        Source.ExpandAll();
        _script.CalculateGraphEdges(_commands, _log);

        if (_script.Event.StartingChibisSection is not null)
        {
            HasStartingChibis = true;
            StartingChibis.AddRange(_script.Event.StartingChibisSection.Objects.Where(c => c.ChibiIndex > 0).Select(c => new StartingChibiWithImage(c,
                ((ChibiItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi
                                                      && ((ChibiItem)i).ChibiIndex == c.ChibiIndex)).ChibiAnimations.First().Value[0].Frame,
                StartingChibis, UnusedChibis, _script, this)));
            short[] usedIndices = StartingChibis.Select(c => c.StartingChibi.ChibiIndex).ToArray();
            for (short i = 1; i <= 5; i++)
            {
                if (usedIndices.Contains(i))
                {
                    continue;
                }
                UnusedChibis.Add(new(new() { ChibiIndex = i }, ((ChibiItem)_project.Items.First(c => c.Type == ItemDescription.ItemType.Chibi
                                                                                                     && ((ChibiItem)c).ChibiIndex == i)).ChibiAnimations.First().Value[0].Frame, StartingChibis, UnusedChibis, _script, this));
            }
        }
        else
        {
            HasStartingChibis = false;
        }

        Choices.AddRange(_script.Event.ChoicesSection.Objects.Where(c => c.Id > 0).Select(c => new ReactiveChoice(c, _script, this)));

        MapCharactersSubEditorVm = new(_script, this);

        InteractableObjects.AddRange(_script.Event.InteractableObjectsSection?.Objects.Where(o => o.ObjectId > 0).Select(o => new ReactiveInteractableObject(o,
            MapCharactersSubEditorVm.Maps.ToArray(), _script, this)) ?? []);
        UnusedInteractableObjects.AddRange(MapCharactersSubEditorVm.Maps.SelectMany(m => m.Map.InteractableObjects)
            .Where(o => o.ObjectId > 0 && InteractableObjects.All(i => i.InteractableObject.ObjectId != o.ObjectId))
            .Select(o => new ReactiveInteractableObject(new() { ObjectId = (short)o.ObjectId },  MapCharactersSubEditorVm.Maps.ToArray(), _script, this)));

        Script00TipTimer = new(TimeSpan.FromSeconds(5)) { AutoReset = false };
        Script00TipTimer.Elapsed += (_, _) =>
        {
            Script00TipVisible = false;
            Script00TipTimer.Stop();
        };

        AddScriptCommandCommand = ReactiveCommand.CreateFromTask(AddCommand);
        AddScriptSectionCommand = ReactiveCommand.CreateFromTask(AddSection);
        DeleteScriptCommandOrSectionCommand = ReactiveCommand.CreateFromTask(Delete);
        ClearScriptCommand = ReactiveCommand.CreateFromTask(Clear);
        MoveUpCommand = ReactiveCommand.Create(MoveUp);
        MoveDownCommand = ReactiveCommand.Create(MoveDown);
        CutCommand = ReactiveCommand.Create(Cut);
        CopyCommand = ReactiveCommand.Create(Copy);
        PasteCommand = ReactiveCommand.Create(Paste);
        ApplyTemplateCommand = ReactiveCommand.CreateFromTask(ApplyTemplate);
        GenerateTemplateCommand = ReactiveCommand.CreateFromTask(GenerateTemplate);
        AddStartingChibisCommand = ReactiveCommand.Create(AddStartingChibis);
        RemoveStartingChibisCommand = ReactiveCommand.Create(RemoveStartingChibis);
        AddChoiceCommand = ReactiveCommand.Create(AddChoice);
        AddInteractableObjectCommand = ReactiveCommand.CreateFromTask(AddInteractableObject);
        RemoveInteractableObjectCommand = ReactiveCommand.Create(RemoveInteractableObject);

        AddCommandHotKey = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.N, KeyModifiers.Shift);
        CutHotKey = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.X);
        CopyHotKey = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.C);
        PasteHotKey = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.V);
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
                CommandVerb.BG_FADE => new BgFadeScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.TRANS_OUT => new TransInOutScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.TRANS_IN => new TransInOutScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SET_PLACE => new SetPlaceScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.ITEM_DISPIMG => new ItemDispimgScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.BACK => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.NOOP2 => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.INVEST_START => new InvestStartScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.LOAD_ISOMAP => new LoadIsomapScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.INVEST_END => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHIBI_EMOTE => new ChibiEmoteScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.NEXT_SCENE => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SKIP_SCENE => new SkipSceneScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.MODIFY_FRIENDSHIP => new ModifyFriendshipScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHIBI_ENTEREXIT => new ChibiEnterExitScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.AVOID_DISP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.GLOBAL2D => new Global2DScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHESS_LOAD => new ChessLoadScriptCommandEditorViewModel(_selectedCommand, this, Window, _log),
                CommandVerb.CHESS_VGOTO => new ChessVgotoScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHESS_MOVE => new ChessMoveScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHESS_TOGGLE_GUIDE => new ChessToggleGuideScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHESS_TOGGLE_HIGHLIGHT => new ChessToggleHighlightScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHESS_TOGGLE_CROSS => new ChessToggleCrossScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHESS_CLEAR_ANNOTATIONS => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CHESS_RESET => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.SCENE_GOTO_CHESS => new SceneGotoScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                CommandVerb.EPHEADER => new EpheaderScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.NOOP3 => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.CONFETTI => new ConfettiScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.BG_DISPCG => new BgDispCgScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.BG_SCROLL => new BgScrollScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.OP_MODE => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.WAIT_CANCEL => new WaitCancelScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.BG_REVERT => new EmptyScriptCommandEditorViewModel(_selectedCommand, this, _log),
                CommandVerb.BG_DISP2 => new BgDispScriptCommandEditorViewModel(_selectedCommand, this, _log, Window),
                _ => new(_selectedCommand, this, _log),
            };
        }
    }

    public void UpdatePreview()
    {
        try
        {
            if (_selectedCommand is null)
            {
                ScriptPreviewCanvas.Preview = null;
            }
            else
            {
                ScriptPreview preview = _script.GetScriptPreview(_commands, _selectedCommand, _project, _log);
                _selectedCommand.CurrentPreview = preview;

                CurrentChessBoard = preview.ChessPuzzle;
                CurrentGuidePieces.Clear();
                CurrentGuidePieces.AddRange(preview.ChessGuidePieces);
                CurrentHighlightedSpaces.Clear();
                CurrentHighlightedSpaces.AddRange(preview.ChessHighlightedSpaces);
                CurrentCrossedSpaces.Clear();
                CurrentCrossedSpaces.AddRange(preview.ChessCrossedSpaces);

                ScriptPreviewCanvas.Preview = preview;
            }
        }
        catch (Exception ex)
        {
            _log.LogException(Strings.ErrorFailedUpdatingPreview, ex);
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
        else if (source is ScriptSectionTreeItem sourceSectionItem)
        {
            ReactiveScriptSection sourceSection = sourceSectionItem.ViewModel;
            int targetSectionIndex;
            if (target is ScriptCommandTreeItem targetCommand)
            {
                targetSectionIndex = _script.Event.ScriptSections.IndexOf(targetCommand.ViewModel!.Section);
            }
            else if (target is ScriptSectionTreeItem targetSection)
            {
                if (position == TreeDataGridRowDropPosition.Before && ScriptSections.IndexOf(targetSection.ViewModel!) > 0)
                {
                    targetSectionIndex = ScriptSections.IndexOf(targetSection.ViewModel!) - 1;
                }
                else
                {
                    targetSectionIndex = ScriptSections.IndexOf(targetSection.ViewModel!);
                }
            }
            else
            {
                return;
            }

            if (targetSectionIndex == 0)
            {
                Script00TipVisible = true;
                Script00TipTimer.Start();
                return;
            }

            int sourceSectionIndex = ScriptSections.IndexOf(sourceSection);
            if (!_commands.Move(sourceSectionIndex, targetSectionIndex))
            {
                return;
            }
            Commands = _commands;
            ScriptSections.Move(sourceSectionIndex, targetSectionIndex);
            _script.Event.ScriptSections.Move(sourceSectionIndex, targetSectionIndex);

            _script.Refresh(_project, _log);
            Description.UnsavedChanges = true;
        }
    }

    public bool CanDrag(ITreeItem source)
    {
        if (source is ScriptSectionTreeItem sectionTreeItem && sectionTreeItem.ViewModel!.Name.Equals("SCRIPT00"))
        {
            Script00TipTimer.Start();
            Script00TipVisible = true;
            return false;
        }
        return true;
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
            await new AddScriptCommandDialog { DataContext = new AddScriptCommandDialogViewModel() }
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
        string sectionName = await new AddScriptSectionDialog { DataContext = new AddScriptSectionDialogViewModel() }
            .ShowDialog<string>(Window.Window);
        if (string.IsNullOrEmpty(sectionName))
        {
            return;
        }

        sectionName = $"NONE{sectionName}";
        if (ScriptSections.Any(s => s.Name.Equals(sectionName)))
        {
            await Window.Window.ShowMessageBoxAsync(Strings.ScriptEditorDupeSectionNameErrorTitle,
                Strings.ScriptEditorSectionNameExistsWarning,
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
        };
        ReactiveScriptSection reactiveSection = new(section, _log, Window.Window);

        _script.Event.ScriptSections.Insert(sectionIndex, section);
        _script.Event.NumSections++;
        _script.Event.LabelsSection.Objects.Insert(sectionIndex,
            new()
            {
                Name = $"NONE/{sectionName[4..]}",
                Id = (short)(_script.Event.LabelsSection.Objects.Count == 1 ? 1001 :
                    _script.Event.LabelsSection.Objects.Max(l => l.Id) + 1),
            }
        );

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
                await Window.Window.ShowMessageBoxAsync(Strings.ScriptEditorCannotDeleteRootSection,
                    Strings.ScriptEditorCannotDeleteRootWarning, ButtonEnum.Ok,
                    Icon.Warning, _log);
                return;
            }

            foreach ((_, List<ScriptItemCommand> commands) in _commands)
            {
                foreach (ScriptItemCommand command in commands)
                {
                    if (command.Verb is CommandVerb.VGOTO or CommandVerb.GOTO or CommandVerb.INVEST_START)
                    {
                        int paramIdx = command.Verb switch
                        {
                            CommandVerb.VGOTO => 1,
                            CommandVerb.GOTO => 0,
                            _ => 4,
                        };
                        if (((ScriptSectionScriptParameter)command.Parameters[paramIdx]).Section.Name == SelectedSection.Name)
                        {
                            command.Parameters[paramIdx] = new ScriptSectionScriptParameter("Script Section",
                                ScriptSections.FirstOrDefault(s => s.Name.StartsWith("NONE"))?.Section);
                            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(command.Section)]
                                .Objects[command.Index].Parameters[paramIdx] = _script.Event.LabelsSection.Objects.FirstOrDefault(l => l.Name.Replace("/", "") == command.Parameters[paramIdx].Name)?.Id ?? 0;
                            command.UpdateDisplay();
                        }
                    }
                }
            }

            foreach (ReactiveChoice choice in Choices)
            {
                if (choice.AssociatedSection == SelectedSection)
                {
                    choice.AssociatedSection = null;
                    _script.Event.ChoicesSection.Objects[_script.Event.ChoicesSection.Objects.IndexOf(choice.Choice)].Id = 0;
                }
            }

            SelectedSection = null;
            SelectedCommand = Commands[_script.Event.ScriptSections[sectionIndex - 1]][^1];
            Source.RowSelection?.Select(new(_script.Event.ScriptSections.IndexOf(SelectedCommand.Section), SelectedCommand.Index));

            _script.Event.ScriptSections.RemoveAt(sectionIndex);
            _script.Event.NumSections--;
            _script.Event.LabelsSection.Objects.RemoveAt(sectionIndex);

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
        if (await Window.Window.ShowMessageBoxAsync(Strings.ScriptEditorClearScriptPromptTitle,
                Strings.ScriptEditorClearScriptPrompt,
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
        });
        ScriptSections.Clear();
        ScriptSections.Add(new(_script.Event.ScriptSections[0], _log, Window.Window));
        if (_script.Event.LabelsSection?.Objects?.Count > 2)
        {
            _script.Event.LabelsSection.Objects.RemoveRange(1, _script.Event.LabelsSection.Objects.Count - 2);
        }
        _commands.Clear();
        _commands.Add(_script.Event.ScriptSections[0], []);
        Commands = _commands;

        _script.Event.DialogueLines.Clear();
        _script.Event.DialogueSection.Objects.Clear();
        if (_script.Event.ConditionalsSection?.Objects?.Count > 1)
        {
            _script.Event.ConditionalsSection.Objects.RemoveRange(0, _script.Event.ConditionalsSection.Objects.Count - 2);
        }

        if (_script.Event.ChoicesSection?.Objects?.Count > 2)
        {
            _script.Event.ChoicesSection?.Objects?.RemoveRange(1, _script.Event.ChoicesSection.Objects.Count - 2);
        }

        if (HasStartingChibis)
        {
            RemoveStartingChibis();
        }
        if (MapCharactersSubEditorVm.HasMapCharacters)
        {
            await MapCharactersSubEditorVm.RemoveMapCharactersSection(false);
        }
        if (_script.Event.InteractableObjectsSection?.Objects?.Count > 1)
        {
            _script.Event.InteractableObjectsSection?.Objects?.RemoveRange(0, _script.Event.InteractableObjectsSection.Objects.Count - 2);
            UnusedInteractableObjects.AddRange(InteractableObjects);
            InteractableObjects.Clear();
        }

        _script.Refresh(_project, _log);
        _script.UnsavedChanges = true;
    }

    private void MoveUp()
    {
        if (SelectedCommand is null && SelectedSection is null)
        {
            return;
        }

        if (SelectedCommand is not null)
        {
            int sectionIndex = ScriptSections.IndexOf(ScriptSections.First(s => s.Name == SelectedCommand.Section.Name));
            int commandIndex = SelectedCommand.Index;
            if (SelectedCommand.Index == 0)
            {
                if (_script.Event.ScriptSections.IndexOf(SelectedCommand.Section) == 0)
                {
                    return;
                }

                ScriptItemCommand selectedCommandRef = SelectedCommand;
                ScriptSections[sectionIndex].DeleteCommand(0, Commands);
                ScriptSections[sectionIndex - 1].InsertCommand(ScriptSections[sectionIndex - 1].Commands.Count, selectedCommandRef, Commands);
                selectedCommandRef.Index = ScriptSections[sectionIndex - 1].Commands.Count - 1;
                selectedCommandRef.Section = ScriptSections[sectionIndex - 1].Section;
                Source.RowSelection?.Select(new(sectionIndex - 1, ScriptSections[sectionIndex - 1].Commands.Count - 1));
            }
            else
            {
                ScriptSections[sectionIndex].SwapCommands(commandIndex, commandIndex - 1, Commands);
                Source.RowSelection?.Select(new(sectionIndex, commandIndex - 1));
            }
        }
        else
        {
            int sectionIndex = ScriptSections.IndexOf(SelectedSection);
            if (sectionIndex < 2)
            {
                return;
            }

            if (!_commands.Swap(sectionIndex, sectionIndex - 1))
            {
                return;
            }
            Commands = _commands;
            ScriptSections.Swap(sectionIndex, sectionIndex - 1);
            _script.Event.ScriptSections.Swap(sectionIndex, sectionIndex - 1);
            Source.RowSelection?.Select(new(sectionIndex - 1));

            _script.Refresh(_project, _log);
            Description.UnsavedChanges = true;
        }
    }

    private void MoveDown()
    {
        if (SelectedCommand is null && SelectedSection is null)
        {
            return;
        }

        if (SelectedCommand is not null)
        {
            int sectionIndex = ScriptSections.IndexOf(ScriptSections.First(s => s.Name == SelectedCommand.Section.Name));
            int commandIndex = SelectedCommand.Index;
            if (SelectedCommand.Index == ScriptSections[sectionIndex].Commands.Count - 1)
            {
                if (_script.Event.ScriptSections.IndexOf(SelectedCommand.Section) == _script.Event.ScriptSections.Count - 1)
                {
                    return;
                }

                ScriptItemCommand selectedCommandRef = SelectedCommand;
                ScriptSections[sectionIndex].DeleteCommand(ScriptSections[sectionIndex].Commands.Count - 1, Commands);
                ScriptSections[sectionIndex + 1].InsertCommand(0, selectedCommandRef, Commands);
                selectedCommandRef.Index = 0;
                selectedCommandRef.Section = ScriptSections[sectionIndex + 1].Section;
                Source.RowSelection?.Select(new(sectionIndex + 1, 0));
            }
            else
            {
                ScriptSections[sectionIndex].SwapCommands(commandIndex, commandIndex + 1, Commands);
                Source.RowSelection?.Select(new(sectionIndex, commandIndex + 1));
            }
        }
        else
        {
            int sectionIndex = ScriptSections.IndexOf(SelectedSection);
            if (sectionIndex == 0 || sectionIndex == ScriptSections.Count - 1)
            {
                return;
            }

            if (!_commands.Swap(sectionIndex, sectionIndex + 1))
            {
                return;
            }
            Commands = _commands;
            ScriptSections.Swap(sectionIndex, sectionIndex + 1);
            _script.Event.ScriptSections.Swap(sectionIndex, sectionIndex + 1);
            Source.RowSelection?.Select(new(sectionIndex + 1));

            _script.Refresh(_project, _log);
            Description.UnsavedChanges = true;
        }
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
        ScriptSections.AddRange(_script.Event.ScriptSections.Select(s => new ReactiveScriptSection(s, _log, Window.Window)));
        Commands = _script.GetScriptCommandTree(_project, _log);
        foreach (ReactiveScriptSection section in ScriptSections)
        {
            section.SetCommands(_commands[section.Section]);
        }
        Source.ExpandAll();
        _script.Refresh(_project, _log);
        Description.UnsavedChanges = true;
    }

    private async Task GenerateTemplate()
    {
        ScriptTemplate template = await new GenerateTemplateDialog { DataContext = new GenerateTemplateDialogViewModel(Commands, _project, _log) }
                .ShowDialog<ScriptTemplate>(Window.Window);
        if (template is null)
        {
            return;
        }

        Lib.IO.WriteStringFile(Path.Combine(_project.ConfigUser.ScriptTemplatesDirectory, $"{template.Name.Replace("/", "-")}.slscr"),
            JsonSerializer.Serialize(template), _log);
        _project.ConfigUser.ScriptTemplates.Add(template);
    }

    private void AddStartingChibis()
    {
        _script.Event.StartingChibisSection = new() { Name = "STARTINGCHIBIS" };
        _script.Event.StartingChibisSection.Objects.Add(new()); // Blank chibi entry
        _script.Event.NumSections += 2;
        for (short i = 1; i <= 5; i++)
        {
            UnusedChibis.Add(new(new() { ChibiIndex = i }, ((ChibiItem)_project.Items.First(c => c.Type == ItemDescription.ItemType.Chibi
                && ((ChibiItem)c).ChibiIndex == i)).ChibiAnimations.First().Value[0].Frame, StartingChibis, UnusedChibis, _script, this));
        }
        HasStartingChibis = true;
        _script.UnsavedChanges = true;
        UpdatePreview();
    }

    private void RemoveStartingChibis()
    {
        _script.Event.StartingChibisSection = null;
        _script.Event.NumSections -= 2;
        StartingChibis.Clear();
        UnusedChibis.Clear();
        HasStartingChibis = false;
        _script.UnsavedChanges = true;
        UpdatePreview();
    }

    private void AddChoice()
    {
        ChoicesSectionEntry choice = new()
        {
            Id = _script.Event.LabelsSection.Objects.FirstOrDefault(i => i.Id > 0)?.Id ?? 0,
            Text = _project.Localize("Replace me"),
        };
        if (_script.Event.ChoicesSection.Objects.Count > 0)
        {
            _script.Event.ChoicesSection.Objects.Insert(_script.Event.ChoicesSection.Objects.Count - 1, choice);
        }
        else
        {
            _script.Event.ChoicesSection.Objects.Add(choice);
        }
        Choices.Add(new(choice, _script, this));
        Description.UnsavedChanges = true;
    }

    private async Task AddInteractableObject()
    {
        AddInteractableObjectDialogViewModel addInteractableObjectDialogViewModel = new(UnusedInteractableObjects,
            _project.ConfigUser.Hacks.FirstOrDefault(h => h.Name.Equals("Sensible Interactable Object Selection"))?.IsApplied ?? false);
        ReactiveInteractableObject obj =
            await new AddInteractableObjectDialog { DataContext = addInteractableObjectDialogViewModel }
                .ShowDialog<ReactiveInteractableObject>(Window.Window);
        if (obj is not null)
        {
            _script.Event.InteractableObjectsSection.Objects.Insert(_script.Event.InteractableObjectsSection.Objects.Count - 1, obj.InteractableObject);
            InteractableObjects.Add(obj);
            UnusedInteractableObjects.Remove(obj);
        }
    }

    private void RemoveInteractableObject()
    {
        if (SelectedInteractableObject is null)
        {
            return;
        }
        SelectedInteractableObject.InteractSection = null;
        UnusedInteractableObjects.Add(SelectedInteractableObject);
        _script.Event.InteractableObjectsSection.Objects.Remove(SelectedInteractableObject.InteractableObject);
        InteractableObjects.Remove(SelectedInteractableObject);
        _script.UnsavedChanges = true;
        _script.Refresh(_project, _log);
        UpdatePreview();
    }
}

public class ReactiveScriptSection(ScriptSection section, ILogger log, MainWindow window) : ReactiveObject
{
    public ScriptSection Section { get; } = section;

    private string _name = section.Name;

    public string DisplayName
    {
        get => _name.StartsWith("NONE") ? _name[4..] : _name;
        set
        {
            this.RaiseAndSetIfChanged(ref _name, value is "SCRIPT00" or "SCRIPT01" ? value : $"NONE{value}");
            Section.Name = _name;
        }
    }

    public string Name
    {
        get => _name;
        set => _name = value;
    }

    public ObservableCollection<ITreeItem> Commands { get; private set; } = [];

    public void InsertCommand(int index, ScriptItemCommand command, OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commands)
    {
        Commands.Insert(index, new ScriptCommandTreeItem(command, log, window));
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

    public void SwapCommands(int index1, int index2, OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commands)
    {
        if (index1 == index2)
        {
            return;
        }

        if (index1 > index2)
        {
            (index1, index2) = (index2, index1);
        }

        ScriptItemCommand command1 = commands[Section][index1];
        ScriptItemCommand command2 = commands[Section][index2];
        DeleteCommand(index2, commands);
        DeleteCommand(index1, commands);
        InsertCommand(index1, command2, commands);
        InsertCommand(index2, command1, commands);
        (command1.Index, command2.Index) = (command2.Index, command1.Index);
    }

    internal void SetCommands(IEnumerable<ScriptItemCommand> commands)
    {
        Commands.Clear();
        Commands.AddRange(commands.Select(c => new ScriptCommandTreeItem(c, log, window)));
    }

    public override string ToString() => DisplayName;
}

public class StartingChibiWithImage : ReactiveObject
{
    public StartingChibiEntry StartingChibi { get; }

    [Reactive]
    public SKBitmap ChibiBitmap { get; set; }

    public ICommand AddStartingChibiCommand { get; }

    public ICommand RemoveStartingChibiCommand { get; }

    public StartingChibiWithImage(StartingChibiEntry startingChibi, SKBitmap chibiBitmap,
        ObservableCollection<StartingChibiWithImage> usedChibis, ObservableCollection<StartingChibiWithImage> unusedChibis,
        ScriptItem script, ScriptEditorViewModel scriptEditor)
    {
        StartingChibi = startingChibi;
        ChibiBitmap = chibiBitmap;

        AddStartingChibiCommand = ReactiveCommand.Create(() =>
        {
            unusedChibis.Remove(this);
            for (short i = 0; i <= usedChibis.Count; i++)
            {
                if (i == usedChibis.Count)
                {
                    usedChibis.Add(this);
                    break;
                }

                if (usedChibis[i].StartingChibi.ChibiIndex > startingChibi.ChibiIndex)
                {
                    usedChibis.Insert(i, this);
                    break;
                }
            }
            script.Event.StartingChibisSection.Objects.Insert(script.Event.StartingChibisSection.Objects.Count - 1, StartingChibi);
            script.UnsavedChanges = true;
            scriptEditor.UpdatePreview();
        });
        RemoveStartingChibiCommand = ReactiveCommand.Create(() =>
        {
            usedChibis.Remove(this);
            for (short i = 0; i <= unusedChibis.Count; i++)
            {
                if (i == unusedChibis.Count)
                {
                    unusedChibis.Add(this);
                    break;
                }

                if (unusedChibis[i].StartingChibi.ChibiIndex > startingChibi.ChibiIndex)
                {
                    unusedChibis.Insert(i, this);
                    break;
                }
            }
            script.Event.StartingChibisSection.Objects.Remove(
                script.Event.StartingChibisSection.Objects.First(c => c.ChibiIndex == StartingChibi.ChibiIndex));
            script.UnsavedChanges = true;
            scriptEditor.UpdatePreview();
        });
    }
}

public class ReactiveInteractableObject(
    InteractableObjectEntry interactableObject,
    MapItem[] maps,
    ScriptItem script,
    ScriptEditorViewModel scriptEditor)
    : ReactiveObject
{
    public InteractableObjectEntry InteractableObject { get; } = interactableObject;


    public string Description => maps.SelectMany(m => m.Map.InteractableObjects)
                                     .FirstOrDefault(i => i.ObjectId == InteractableObject.ObjectId)?.ObjectName?.GetSubstitutedString(scriptEditor.Window.OpenProject)
                                 ?? $"{InteractableObject.ObjectId}";

    public ObservableCollection<ReactiveScriptSection> ScriptSections => scriptEditor.ScriptSections;
    public ReactiveScriptSection InteractSection
    {
        get => scriptEditor.ScriptSections.FirstOrDefault(s => s.Name.Equals(script.Event.LabelsSection.Objects
            .FirstOrDefault(l => l.Id == InteractableObject.ScriptBlock)?.Name.Replace("/", "")));
        set
        {
            InteractableObject.ScriptBlock = script.Event.LabelsSection.Objects
                .FirstOrDefault(l => l.Name.Replace("/", "").Equals(value?.Name))?.Id ?? 0;
            this.RaisePropertyChanged();
            script.Refresh(scriptEditor.Window.OpenProject, scriptEditor.Window.Log);
            scriptEditor.UpdatePreview();
            script.UnsavedChanges = true;
        }
    }
}

public class ReactiveChoice : ReactiveObject
{
    public ChoicesSectionEntry Choice { get; }

    private ScriptItem _script;
    private ScriptEditorViewModel _scriptEditor;

    public ReactiveScriptSection AssociatedSection
    {
        get => _scriptEditor.ScriptSections.FirstOrDefault(sec => sec.Name.Equals(
            _script.Event.LabelsSection.Objects.FirstOrDefault(s => s.Id == Choice.Id)?.Name.Replace("/", "")));
        set
        {
            Choice.Id = _script.Event.LabelsSection.Objects.FirstOrDefault(c => c.Name.Replace("/", "").Equals(value.Name))?.Id ?? 0;
            this.RaisePropertyChanged();
            _scriptEditor.UpdatePreview();
            _script.Refresh(_scriptEditor.Window.OpenProject, _scriptEditor.Window.Log);
            _scriptEditor.Description.UnsavedChanges = true;
        }
    }

    public string ChoiceText
    {
        get => Choice.Text.GetSubstitutedString(_scriptEditor.Window.OpenProject);
        set
        {
            Choice.Text = value.GetOriginalString(_scriptEditor.Window.OpenProject);
            this.RaisePropertyChanged();
            _scriptEditor.UpdatePreview();
            _scriptEditor.Description.UnsavedChanges = true;
        }
    }

    public ICommand DeleteCommand { get; }

    public ReactiveChoice(ChoicesSectionEntry choice, ScriptItem script, ScriptEditorViewModel scriptEditor)
    {
        Choice = choice;
        _script = script;
        _scriptEditor = scriptEditor;

        DeleteCommand = ReactiveCommand.Create(() =>
        {
            _script.Event.ChoicesSection.Objects.Remove(Choice);
            _scriptEditor.UpdatePreview();
            _scriptEditor.Description.UnsavedChanges = true;
            _scriptEditor.Choices.Remove(this);
        });
    }
}
