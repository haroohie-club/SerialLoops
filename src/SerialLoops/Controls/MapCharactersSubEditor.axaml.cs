using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SkiaSharp;

namespace SerialLoops.Controls;

public partial class MapCharactersSubEditor : UserControl
{
    public MapCharactersSubEditor()
    {
        InitializeComponent();
    }

    private async void DoDrag(object sender, PointerPressedEventArgs e)
    {
        DataObject dragData = new();
        dragData.Set(nameof(ReactiveMapCharacter), ((Panel)sender).DataContext!);

        await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
    }


    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
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
            Point point = de.GetPosition((Canvas)mapCharactersSubEditor.Content!);
            SKPoint gridPoint = ((MapCharactersSubEditorViewModel)DataContext)!.AllGridPositions
                .MinBy(p => p.Key.Distance(point)).Value;
            ((MapCharactersSubEditorViewModel)DataContext).UpdateMapCharacter(character, (short)gridPoint.X, (short)gridPoint.Y);
        }

        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void MapCharacter_OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        ((MapCharactersSubEditorViewModel)DataContext)!.SelectedMapCharacter = ((Panel)sender).DataContext as ReactiveMapCharacter;
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

