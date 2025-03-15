using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Skia;
using DynamicData;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using IO = SerialLoops.Lib.IO;

namespace SerialLoops.ViewModels.Dialogs;

public class EditDialogueColorsDialogViewModel : ViewModelBase
{
    private Project _project;
    private ILogger _log;
    public DefaultDialogueColorPalette DialogueColorPalette { get; }
    public ObservableCollection<DialogueColorWithInfo> Colors { get; } = [];

    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    public EditDialogueColorsDialogViewModel(Project project, ILogger log)
    {
        _project = project;
        _log = log;
        DialogueColorPalette = new();
        Colors.AddRange(_project.DialogueColors.Select((c, i) => new DialogueColorWithInfo(c.ToAvalonia(), i)));
        Colors[0].Info = Strings.DialogueColor0Info;
        Colors[1].Info = Strings.DialogueColor1Info;
        Colors[4].Info = Strings.DialogueColor4Info;
        Colors[7].Info = Strings.DialogueColor7Info;

        SaveCommand = ReactiveCommand.Create<EditDialogueColorsDialog>(Save);
        CancelCommand = ReactiveCommand.Create<EditDialogueColorsDialog>(dialog => dialog.Close());
    }

    public void Save(EditDialogueColorsDialog dialog)
    {
        for (int i = 0; i < Colors.Count; i++)
        {
            _project.DialogueColors[i] = Colors[i].Color.ToSKColor();
            _project.DialogueColorFilters[i] = _project.DialogueColors[i].GetColorFilter();

            byte r = _project.DialogueColors[i].Red;
            byte g = _project.DialogueColors[i].Green;
            byte b = _project.DialogueColors[i].Blue;
            int rStep = Math.Max((255 - r) / 32, 1);
            int gStep = Math.Max((255 - g) / 32, 1);
            int bStep = Math.Max((255 - b) / 32, 1);
            int rCur = 0, gCur = 0, bCur = 0;
            for (int j = i * 16 + 15; j > i * 16; j--)
            {
                _project.DialoguePaletteFile.Palette[j] = new(r, g, b);
                rCur++;
                gCur++;
                bCur++;
                if (rCur == rStep)
                {
                    rCur = 0;
                    r -= 8;
                }

                if (gCur == gStep)
                {
                    gCur = 0;
                    g -= 8;
                }

                if (bCur == bStep)
                {
                    bCur = 0;
                    b -= 8;
                }
            }
        }

        using MemoryStream textureStream = new();
        _project.DialoguePaletteFile.GetImage().Encode(textureStream, SKEncodedImageFormat.Png, 1);
        IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{_project.DialoguePaletteFile.Index:X3}.png"),
            textureStream.ToArray(), _project, _log);
        IO.WriteStringFile(Path.Combine("assets", "graphics", $"{_project.DialoguePaletteFile.Index:X3}.gi"),
            _project.DialoguePaletteFile.GetGraphicInfoFile(), _project, _log);

        dialog.Close();
    }
}

public class DialogueColorWithInfo(Color color, int index) : ReactiveObject
{
    public string IndexHeader { get; } = string.Format(Strings.DialogueColorHeader, index);

    [Reactive]
    public Color Color { get; set; } = color;
    [Reactive]
    public string Info { get; set; }
}
