using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Models;
using SerialLoops.ViewModels.Editors.ScriptCommandEditors;
using SkiaSharp;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.ViewModels.Editors
{
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
        [Reactive]
        public ScriptSection SelectedSection { get; set; }

        [Reactive]
        public SKBitmap PreviewBitmap { get; set; }
        [Reactive]
        public ScriptCommandEditorViewModel CurrentCommandViewModel { get; set; }

        public Dictionary<ScriptSection, List<ScriptItemCommand>> Commands
        {
            get => _commands;
            set
            {
                this.RaiseAndSetIfChanged(ref _commands, value);
                Source = new(_commands.Keys.Select(s => new ScriptSectionTreeItem(s, _commands[s])))
                {
                    Columns =
                    {
                        new HierarchicalExpanderColumn<ITreeItem>(
                            new TemplateColumn<ITreeItem>(null, new FuncDataTemplate<ITreeItem>((val, namescope) =>
                            {
                                return GetItemPanel(val);
                            }), options: new TemplateColumnOptions<ITreeItem>() { IsTextSearchEnabled = true }),
                            i => i.Children
                        )
                    }
                };
                Source.RowSelection.SingleSelect = true;
                Source.RowSelection.SelectionChanged += RowSelection_SelectionChanged;
                Source.ExpandAll();
            }
        }

        [Reactive]
        public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }

        public ScriptEditorViewModel(ScriptItem script, MainWindowViewModel window, ILogger log) : base(script, window, log)
        {
            _script = script;
            _project = window.OpenProject;
            PopulateScriptCommands();
            _script.CalculateGraphEdges(_commands, _log);
        }

        public void PopulateScriptCommands(bool refresh = false)
        {
            if (refresh)
            {
                _script.Refresh(_project, _log);
            }
            Commands = _script.GetScriptCommandTree(_project, _log);
        }

        private StackPanel GetItemPanel(ITreeItem val)
        {
            if (val is null)
            {
                return null;
            }
            StackPanel panel = new()
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 3,
            };
            if (val.Icon is not null)
            {
                if (val.Icon.Parent is not null)
                {
                    ((StackPanel)val.Icon.Parent).Children.Clear();
                }
                panel.Children.Add(val.Icon);
            }
            panel.Children.Add(new TextBlock { Text = val.Text });
            return panel;
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
                    CommandVerb.INIT_READ_FLAG => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.DIALOGUE => new DialogueScriptCommandEditorViewModel(_selectedCommand, this, _window),
                    CommandVerb.REMOVED => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.SND_STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.SCREEN_SHAKE_STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.WAIT => new WaitScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.HOLD => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.NOOP1 => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.BACK => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.STOP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.NOOP2 => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.INVEST_END => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.NEXT_SCENE => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    CommandVerb.AVOID_DISP => new EmptyScriptCommandEditorViewModel(_selectedCommand, this),
                    _ => new ScriptCommandEditorViewModel(_selectedCommand, this)
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
                    (SKBitmap previewBitmap, string errorImage) = _script.GeneratePreviewImage(_commands, SelectedCommand, _project, _log);
                    if (previewBitmap is null)
                    {
                        previewBitmap = new(256, 384);
                        SKCanvas canvas = new(previewBitmap);
                        canvas.DrawColor(SKColors.Black);
                        using Stream noPreviewStream = Assembly.GetCallingAssembly().GetManifestResourceStream(errorImage);
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
                SelectedSection = Commands.Keys.ElementAt(e.SelectedIndexes[0][0]);
            }
        }
    }
}
