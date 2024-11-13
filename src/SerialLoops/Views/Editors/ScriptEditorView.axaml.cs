using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ReactiveUI;
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
        NativeMenu menu = NativeMenu.GetMenu(window.Window)!;
        ScriptEditorViewModel vm = (ScriptEditorViewModel)DataContext!;

        int insertionPoint = menu.Items.Count;
        if (((NativeMenuItem)menu.Items.Last()).Header!.Equals(Strings._Help))
        {
            insertionPoint--;
        }

        window.WindowMenu.Add(MenuHeader.EDIT, new(Strings._Edit));
        window.WindowMenu[MenuHeader.EDIT].Menu =
        [
            new NativeMenuItem()
            {
                Header = Strings.Generate_Template,
            },
            new NativeMenuItem()
            {
                Header = Strings.Apply_Template,
                Icon = ControlGenerator.GetIcon("Template", window.Log),
            },
            new NativeMenuItem()
            {
                Header = Strings.Cut,
                Command = vm.CutCommand,
                Icon = ControlGenerator.GetIcon("Cut", window.Log),
                Gesture = vm.CutHotKey,
            },
            new NativeMenuItem()
            {
                Header = Strings.Copy,
                Command = vm.CopyCommand,
                Icon = ControlGenerator.GetIcon("Copy", window.Log),
                Gesture = vm.CopyHotKey,
            },
            new NativeMenuItem()
            {
                Header = Strings.Paste,
                Command = vm.PasteCommand,
                Icon = ControlGenerator.GetIcon("Paste", window.Log),
                Gesture = vm.PasteHotKey,
            },
        ];
        // window.WindowMenu[MenuHeader.EDIT].Menu!.Items.Last().Bind(NativeMenuItem.IsEnabledProperty,
        //     vm.ObservableForProperty(s => s.ClipboardCommand));
        menu.Items.Insert(insertionPoint, window.WindowMenu[MenuHeader.EDIT]);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        MainWindowViewModel window = ((ScriptEditorViewModel)DataContext!).Window;
        NativeMenu menu = NativeMenu.GetMenu(((ScriptEditorViewModel)DataContext!).Window.Window)!;

        window.WindowMenu.Remove(MenuHeader.EDIT);
        menu.Items.Remove(menu.Items.First(i => ((NativeMenuItem)i).Header!.Equals(Strings._Edit)));
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
}
