using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs;

public class GraphicSelectionDialogViewModel : ViewModelBase
{
    private readonly Project _project;
    private readonly ILogger _log;
    private readonly IEnumerable<IPreviewableGraphic> _items;
    private Func<ItemDescription, bool> _specialPredicate;
    private IPreviewableGraphic _currentSelection;
    private string _filter;

    [Reactive]
    public ObservableCollection<IPreviewableGraphic> Items { get; set; }
    public IPreviewableGraphic CurrentSelection
    {
        get => _currentSelection;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentSelection, value);
            PreviewLabel = _currentSelection is null ? Strings.GraphicSelectionNoPreviewAvailable : ((ItemDescription)_currentSelection).DisplayName;
            if (_currentSelection is null)
            {
                Preview = null;
            }
            else
            {
                Preview = _currentSelection.GetPreview(_project, 250, 350);
            }
        }
    }
    public string Filter
    {
        get => _filter;
        set
        {
            this.RaiseAndSetIfChanged(ref _filter, value);
            Items = new(_items.Where(i => i.Text.Contains(_filter, StringComparison.OrdinalIgnoreCase)));
        }
    }
    [Reactive]
    public string PreviewLabel { get; set; }
    [Reactive]
    public SKBitmap Preview { get; set; }

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    public GraphicSelectionDialogViewModel(IEnumerable<IPreviewableGraphic> items, IPreviewableGraphic currentSelection, Project project, ILogger log, Func<ItemDescription, bool> specialPredicate = null) : base()
    {
        _project = project;
        _log = log;
        _specialPredicate = specialPredicate ?? (i => true);
        _items = specialPredicate is not null ? items.Where(i => specialPredicate((ItemDescription)i)) : items;
        Items = new(_items);
        CurrentSelection = currentSelection;
        ConfirmCommand = ReactiveCommand.Create<GraphicSelectionDialog>((dialog) => dialog.Close(CurrentSelection));
        CancelCommand = ReactiveCommand.Create<GraphicSelectionDialog>((dialog) => dialog.Close());
    }
}
