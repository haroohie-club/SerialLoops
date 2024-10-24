using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Models;

namespace SerialLoops.ViewModels.Editors
{
    public class ScriptEditorViewModel : EditorViewModel
    {
        private ScriptItem _script;
        private Dictionary<ScriptSection, List<ScriptItemCommand>> _commands = [];

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
    }
}
