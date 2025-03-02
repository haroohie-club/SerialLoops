using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;

namespace SerialLoops.ViewModels.Controls;

public class ChibiDirectionSelectorViewModel : ViewModelBase
{
    public Action<ChibiItem.Direction> DirectionChangedAction { get; set; }

    public ObservableCollection<DirectionWithButton> Directions { get; }

    private DirectionWithButton _selectedDirection;
    public DirectionWithButton SelectedDirection
    {
        get => _selectedDirection;
        set
        {
            if (value is null)
            {
                return;
            }
            this.RaiseAndSetIfChanged(ref _selectedDirection, value);
            DirectionChangedAction?.Invoke(_selectedDirection.Direction);
        }
    }

    public ChibiDirectionSelectorViewModel(ChibiItem.Direction selectedDirection, Action<ChibiItem.Direction> directionChangedAction)
    {
        Directions = new(new DirectionWithButton[]
        {
            new(ChibiItem.Direction.UP_LEFT), new(ChibiItem.Direction.UP_RIGHT),
            new(ChibiItem.Direction.DOWN_LEFT), new(ChibiItem.Direction.DOWN_RIGHT),
        });
        _selectedDirection = Directions.First(d => d.Direction == selectedDirection);
        DirectionChangedAction = directionChangedAction;
    }

    public void SetAvailableDirections(List<(string Name, ChibiItem.ChibiGraphics Chibi)> chibiEntries)
    {
        List<bool> availableDirections;
        if (chibiEntries is null)
        {
            availableDirections = [false, false, false, false];
        }
        else if (chibiEntries.Count < 4)
        {
            availableDirections =
            [
                chibiEntries.Any(c => c.Name.Contains("_UL")),
                chibiEntries.Any(c => c.Name.Contains("_UR")),
                chibiEntries.Any(c => c.Name.Contains("_BL")),
                chibiEntries.Any(c => c.Name.Contains("_BR")),
            ];
        }
        else
        {
            availableDirections = [true, true, true, true];
        }

        if (availableDirections.Count != Directions.Count)
        {
            return;
        }

        for (int i = 0; i < availableDirections.Count; i++)
        {
            Directions[i].Enabled = availableDirections[i];
        }
    }
}

public class DirectionWithButton(ChibiItem.Direction direction) : ReactiveObject
{
    public ChibiItem.Direction Direction { get; } = direction;
    public string IconPath => $"avares://SerialLoops/Assets/Icons/Chibi_{Direction}.svg";
    [Reactive]
    public bool Enabled { get; set; } = true;
}
