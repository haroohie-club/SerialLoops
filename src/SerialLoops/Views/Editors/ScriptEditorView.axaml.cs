using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MiniToolbar.Avalonia;
using SerialLoops.Assets;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Editors;

namespace SerialLoops.Views.Editors;

public partial class ScriptEditorView : UserControl
{
    private ITreeItem _dragSource = null;

    public ScriptEditorView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        MainWindowViewModel window = ((ScriptEditorViewModel)DataContext!).Window;
        if (window.WindowMenu.ContainsKey(MenuHeader.EDIT))
        {
            UnloadEditMenu();
        }
        NativeMenu menu = NativeMenu.GetMenu(window.Window)!;
        ScriptEditorViewModel vm = (ScriptEditorViewModel)DataContext!;

        int insertionPoint = menu.Items.Count;
        if (((NativeMenuItem)menu.Items.Last()).Header!.Equals(Strings.MenuHelp))
        {
            insertionPoint--;
        }

        window.WindowMenu.Add(MenuHeader.EDIT, new(Strings.MenuEdit));
        window.WindowMenu[MenuHeader.EDIT].Menu =
        [
            new NativeMenuItem
            {
                Header = Strings.ScriptTemplateGenerateLabel,
                Command = vm.GenerateTemplateCommand,
            },
            new NativeMenuItem
            {
                Header = Strings.ScriptEditorApplyTemplateLabel,
                Command = vm.ApplyTemplateCommand,
                Icon = ControlGenerator.GetIcon("Template", window.Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Cut,
                Command = vm.CutCommand,
                Icon = ControlGenerator.GetIcon("Cut", window.Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Copy,
                Command = vm.CopyCommand,
                Icon = ControlGenerator.GetIcon("Copy", window.Log),
            },
            new NativeMenuItem
            {
                Header = Strings.Paste,
                Command = vm.PasteCommand,
                Icon = ControlGenerator.GetIcon("Paste", window.Log),
            },
        ];
        // window.WindowMenu[MenuHeader.EDIT].Menu!.Items.Last().Bind(NativeMenuItem.IsEnabledProperty,
        //     vm.ObservableForProperty(s => s.ClipboardCommand));
        menu.Items.Insert(insertionPoint, window.WindowMenu[MenuHeader.EDIT]);

        ToolbarButton applyTemplateButton = new()
        {
            DataContext = Strings.ScriptEditorApplyTemplateLabel,
            Text = Strings.ScriptTemplateToolbarText,
            Command = vm.ApplyTemplateCommand,
            Icon = ControlGenerator.GetVectorIcon("Template", window.Log),
        };
        window.ToolBar.Items.Insert(0, applyTemplateButton);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (CommandTree.Scroll is not null)
        {
            CommandTree.Scroll.Offset = ((ScriptEditorViewModel)DataContext!).ScrollPosition ?? new();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        UnloadEditMenu();
    }

    private void UnloadEditMenu()
    {
        MainWindowViewModel window = ((ScriptEditorViewModel)DataContext!).Window;
        NativeMenu menu = NativeMenu.GetMenu(((ScriptEditorViewModel)DataContext!).Window.Window)!;

        window.WindowMenu.Remove(MenuHeader.EDIT);
        menu.Items.Remove(menu.Items.First(i => ((NativeMenuItem)i).Header!.Equals(Strings.MenuEdit)));

        window.ToolBar.Items.RemoveAt(0);
        ((ScriptEditorViewModel)DataContext!).ScrollPosition = CommandTree.Scroll?.Offset;
    }

    private void TreeDataGrid_OnRowDragStarted(object sender, TreeDataGridRowDragStartedEventArgs e)
    {
        if (!((ScriptEditorViewModel)DataContext!).CanDrag((ITreeItem)e.Models.ElementAt(0)))
        {
            e.AllowedEffects = DragDropEffects.None;
        }
        else
        {
            e.AllowedEffects = DragDropEffects.Move;
            _dragSource = (ITreeItem)e.Models.ElementAt(0);
        }
    }

    private void TreeDataGrid_OnRowDrop(object sender, TreeDataGridRowDragEventArgs e)
    {
        ((ScriptEditorViewModel)DataContext!).HandleDragDrop(_dragSource, (ITreeItem)e.TargetRow.Model, e.Position);
        _dragSource = null;
        e.Inner.DragEffects = DragDropEffects.None;
        e.Handled = true;
    }

    private void CommandTree_OnKeyDown(object sender, KeyEventArgs e)
    {
        ScriptEditorViewModel vm = (ScriptEditorViewModel)DataContext!;
        if (vm.CutHotKey.Matches(e))
        {
            vm.CutCommand.Execute(null);
        }
        else if (vm.CopyHotKey.Matches(e))
        {
            vm.CopyCommand.Execute(null);
        }
        else if (vm.PasteHotKey.Matches(e))
        {
            vm.PasteCommand.Execute(null);
        }
        else if (vm.DeleteHotKey.Matches(e))
        {
            vm.DeleteScriptCommandOrSectionCommand.Execute(null);
        }
        else if (vm.AddCommandHotKey.Matches(e))
        {
            vm.AddScriptCommandCommand.Execute(null);
        }
    }
}
