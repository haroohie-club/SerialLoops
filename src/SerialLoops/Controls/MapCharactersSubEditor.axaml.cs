using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SkiaSharp;

namespace SerialLoops.Controls;

public partial class MapCharactersSubEditor : UserControl
{
    private CancellationTokenSource _cts = new();

    public MapCharactersSubEditor()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
        return;

        void DragOver(object s, DragEventArgs de)
        {
            if (de.Source is Control && s is MapCharactersSubEditor)
            {
                de.DragEffects = DragDropEffects.Move;
            }
            else
            {
                de.DragEffects = DragDropEffects.None;
            }
        }

        void Drop(object s, DragEventArgs de)
        {
            if (de.Source is not Control || s is not MapCharactersSubEditor mapCharactersSubEditor)
            {
                return;
            }

            de.DragEffects = DragDropEffects.Move;

            ReactiveMapCharacter character = de.Data.Get(nameof(ReactiveMapCharacter)) as ReactiveMapCharacter;
            Point point = de.GetPosition(MapCanvas);
            SKPoint gridPoint = ((MapCharactersSubEditorViewModel)DataContext)!.AllGridPositions
                .MinBy(p => p.Key.Distance(point)).Value;
            ((MapCharactersSubEditorViewModel)DataContext).UpdateMapCharacter(character, (short)gridPoint.X, (short)gridPoint.Y);
        }
    }

    private async void DoDrag(object sender, PointerPressedEventArgs e)
    {
        DataObject dragData = new();
        ReactiveMapCharacter character = ((Panel)sender).DataContext as ReactiveMapCharacter;;
        dragData.Set(nameof(ReactiveMapCharacter), character!);
        ((MapCharactersSubEditorViewModel)DataContext)!.SelectedMapCharacter = character;

        // This makes it so that drag/drop doesn't start immediately after clicking
        _cts = new();
        await Task.Delay(100);
        if (_cts.IsCancellationRequested)
            return;

        await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
    }

    private void MapCharacter_OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        _cts.Cancel();
    }

    private void MapCharacter_OnPointerEntered(object sender, PointerEventArgs e)
    {
        Panel characterBg = sender as Panel;
        characterBg!.Background = Brushes.Gold;
    }

    private void MapCharacter_OnPointerExited(object sender, PointerEventArgs e)
    {

        Panel characterBg = sender as Panel;
        characterBg!.Background = Brushes.Transparent;
    }
}

