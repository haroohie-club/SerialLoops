using HaruhiChokuretsuLib.Util;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class ChessPuzzleEditorViewModel : EditorViewModel
{
    private ChessPuzzleItem _chessPuzzle;

    [Reactive]
    public SKBitmap Chessboard { get; private set; }

    public ChessPuzzleEditorViewModel(ChessPuzzleItem chessPuzzle, MainWindowViewModel window, ILogger log) : base(chessPuzzle, window, log)
    {
        _chessPuzzle = chessPuzzle;
        Chessboard = chessPuzzle.GetChessboard(_window.OpenProject.Grp);
    }
}
