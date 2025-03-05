﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Input;
using DynamicData;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using ReactiveHistory;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class ChessPuzzleEditorViewModel : EditorViewModel
{
    private readonly ChessPuzzleItem _chessPuzzle;

    public SKBitmap EmptyChessboard { get; }

    [Reactive]
    public ObservableCollection<ChessPieceOnBoard> Pieces { get; private set; }

    private int _numMoves;
    public int NumMoves
    {
        get => _numMoves;
        set
        {
            this.RaiseAndSetIfChanged(ref _numMoves, value);
            _chessPuzzle.ChessPuzzle.NumMoves = _numMoves;
            _chessPuzzle.UnsavedChanges = true;
        }
    }

    private int _timeLimit;
    public int TimeLimit
    {
        get => _timeLimit;
        set
        {
            this.RaiseAndSetIfChanged(ref _timeLimit, value);
            _chessPuzzle.ChessPuzzle.TimeLimit = _timeLimit;
            _chessPuzzle.UnsavedChanges = true;
        }
    }

    private int _unknown08;
    public int Unknown08
    {
        get => _unknown08;
        set
        {
            this.RaiseAndSetIfChanged(ref _unknown08, value);
            _chessPuzzle.ChessPuzzle.Unknown08 = _unknown08;
            _chessPuzzle.UnsavedChanges = true;
        }
    }

    private readonly StackHistory _history;

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public KeyGesture UndoGesture { get; }
    public KeyGesture RedoGesture { get; }

    public ChessPuzzleEditorViewModel(ChessPuzzleItem chessPuzzle, MainWindowViewModel window, ILogger log) : base(chessPuzzle, window, log)
    {
        _history = new();

        _chessPuzzle = chessPuzzle;
        EmptyChessboard = ChessPuzzleItem.GetEmptyChessboard(Window.OpenProject.Grp);

        Pieces = new(_chessPuzzle.ChessPuzzle.Chessboard.Select((p, i) => new ChessPieceOnBoard(p, i, window.OpenProject, this)));

        _numMoves = _chessPuzzle.ChessPuzzle.NumMoves;
        _timeLimit = _chessPuzzle.ChessPuzzle.TimeLimit;
        _unknown08 = _chessPuzzle.ChessPuzzle.Unknown08;

        UndoCommand = ReactiveCommand.Create(() => _history.Undo());
        RedoCommand = ReactiveCommand.Create(() => _history.Redo());
        UndoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Z);
        RedoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Y);
    }

    public void MovePiece(int currentIndex, int newIndex, bool withHistory = true)
    {
        if (newIndex < 0 || newIndex >= _chessPuzzle.ChessPuzzle.Chessboard.Length)
        {
            Pieces.RemoveAt(currentIndex);
            Pieces.Insert(currentIndex, new(ChessFile.ChessPiece.Empty, newIndex, _project, this));
        }
        Pieces.Swap(currentIndex, newIndex);
        Pieces[currentIndex].Move(currentIndex);
        Pieces[newIndex].Move(newIndex);
        _chessPuzzle.ChessPuzzle.Chessboard = [.. Pieces.Select(p => p.Piece)];
        _chessPuzzle.UnsavedChanges = true;

        if (withHistory)
        {
            _history.Undos.Push(new(() => MovePiece(newIndex, currentIndex, withHistory: false),
                () => MovePiece(currentIndex, newIndex, withHistory: false), "Undo", "Redo"));
        }
    }
}

public class ChessPieceOnBoard : ReactiveObject
{
    public ChessFile.ChessPiece Piece { get; }
    public SKBitmap Image { get; }


    [Reactive]
    public float X { get; set; }

    [Reactive]
    public float Y { get; set; }

    private readonly ChessPuzzleEditorViewModel _editor;

    public ChessPieceOnBoard(ChessFile.ChessPiece piece, int position, Project project, ChessPuzzleEditorViewModel editor)
    {
        Piece = piece;
        Image = project.ChessPieceImages[piece];
        X = ChessPuzzleItem.GetChessPiecePosition(position).X;
        Y = ChessPuzzleItem.GetChessPiecePosition(position).Y;
        _editor = editor;
    }

    public void Move(int position)
    {
        X = ChessPuzzleItem.GetChessPiecePosition(position).X;
        Y = ChessPuzzleItem.GetChessPiecePosition(position).Y;
    }
}
