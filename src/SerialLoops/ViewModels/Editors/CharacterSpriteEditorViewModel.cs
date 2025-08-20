using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using ReactiveHistory;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public partial class CharacterSpriteEditorViewModel : EditorViewModel
{
    private CharacterSpriteItem _sprite;
    private CharacterItem _character;
    private bool _isLarge;

    public AnimatedImageViewModel AnimatedImage { get; set; }

    public ObservableCollection<CharacterItem> Characters { get; set; }
    public CharacterItem Character
    {
        get => _character;
        set
        {
            this.RaiseAndSetIfChanged(ref _character, value);
            _sprite.Sprite.Character = _character.MessageInfo.Character;
            _sprite.UnsavedChanges = true;
        }
    }
    public bool IsLarge
    {
        get => _isLarge;
        set
        {
            this.RaiseAndSetIfChanged(ref _isLarge, value);
            _sprite.Sprite.IsLarge = _isLarge;
            if (_sprite.Sprite.IsLarge)
            {
                _sprite.Graphics.BodyTextures[2].RenderHeight *= 2;
                _sprite.Graphics.BodyTextures[2].RenderWidth *= 2;
                _sprite.Graphics.EyeTexture.RenderWidth *= 2;
                _sprite.Graphics.EyeTexture.RenderHeight *= 2;
                _sprite.Graphics.MouthTexture.RenderWidth *= 2;
                _sprite.Graphics.MouthTexture.RenderHeight *= 2;
            }
            else
            {
                _sprite.Graphics.BodyTextures[2].RenderWidth /= 2;
                _sprite.Graphics.BodyTextures[2].RenderHeight /= 2;
                _sprite.Graphics.EyeTexture.RenderWidth /= 2;
                _sprite.Graphics.EyeTexture.RenderHeight /= 2;
                _sprite.Graphics.MouthTexture.RenderWidth /= 2;
                _sprite.Graphics.MouthTexture.RenderHeight /= 2;
            }
            _sprite.UnsavedChanges = true;
        }
    }

    private StackHistory _history;

    public ICommand ReplaceCommand { get; set; }
    public ICommand ExportFramesCommand { get; set; }
    public ICommand ExportGIFCommand { get; set; }

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public KeyGesture UndoGesture { get; }
    public KeyGesture RedoGesture { get; }

    public CharacterSpriteEditorViewModel(CharacterSpriteItem item, MainWindowViewModel mainWindow, ILogger log) : base(item, mainWindow, log)
    {
        _history = new();

        _sprite = item;
        AnimatedImage = new(_sprite.GetLipFlapAnimation(mainWindow.OpenProject));
        Characters = new(mainWindow.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Character).Cast<CharacterItem>());
        _character = (CharacterItem)mainWindow.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Character && ((CharacterItem)i).MessageInfo.Character == item.Sprite.Character);
        _isLarge = _sprite.Sprite.IsLarge;

        this.WhenAnyValue(c => c.Character).ObserveWithHistory(c => Character = c, Character, _history);
        this.WhenAnyValue(c => c.IsLarge).ObserveWithHistory(l => IsLarge = l, IsLarge, _history);

        ReplaceCommand = ReactiveCommand.CreateFromTask(ReplaceSprite);
        ExportFramesCommand = ReactiveCommand.CreateFromTask(ExportFrames);
        ExportGIFCommand = ReactiveCommand.CreateFromTask(ExportGIF);

        UndoCommand = ReactiveCommand.Create(() => _history.Undo());
        RedoCommand = ReactiveCommand.Create(() => _history.Redo());
        UndoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Z);
        RedoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Y);
    }

    private async Task ReplaceSprite()
    {
        string baseFile = (await Window.Window.ShowOpenFilePickerAsync(Strings.CharacterSpriteEditorSelectBase,
            [new(Strings.FiletypeImages) { Patterns = Shared.SupportedImageFiletypes }]))?.TryGetLocalPath();
        if (string.IsNullOrEmpty(baseFile))
        {
            return;
        }
        string[] eyeFiles = (await Window.Window.ShowOpenMultiFilePickerAsync(Strings.CharacterSpriteEditorSelectEyeFrames,
                [new(Strings.FiletypeImages) { Patterns = Shared.SupportedImageFiletypes }]))?
            .Select(f => f.TryGetLocalPath()).ToArray();
        if (eyeFiles is null || eyeFiles.Length == 0)
        {
            return;
        }
        string[] mouthFiles = (await Window.Window.ShowOpenMultiFilePickerAsync(Strings.CharacterSpriteEditorSelectMouthFrames,
                [new(Strings.FiletypeImages) { Patterns = Shared.SupportedImageFiletypes }]))?
            .Select(f => f.TryGetLocalPath()).ToArray();
        if (mouthFiles is null || mouthFiles.Length == 0)
        {
            return;
        }

        SKBitmap baseLayout = SKBitmap.Decode(baseFile);
        Match eyePosMatch = EyePosRegex().Match(Path.GetFileNameWithoutExtension(baseFile));
        Match mouthPosMatch = MouthPosRegex().Match(Path.GetFileNameWithoutExtension(baseFile));
        short eyePosX = eyePosMatch.Success ? short.Parse(eyePosMatch.Groups["x"].Value) : (short)0;
        short eyePosY = eyePosMatch.Success ? short.Parse(eyePosMatch.Groups["y"].Value) : (short)0;
        short mouthPosX = mouthPosMatch.Success ? short.Parse(mouthPosMatch.Groups["x"].Value) : (short)0;
        short mouthPosY = mouthPosMatch.Success ? short.Parse(mouthPosMatch.Groups["y"].Value) : (short)0;

        List<SKBitmap> eyeFrames = eyeFiles.OrderBy(f => f).Select(f => SKBitmap.Decode(f)).ToList();
        short[] eyeTimings = new short[eyeFrames.Count];
        Array.Fill<short>(eyeTimings, 32);
        for (int i = 0; i < eyeTimings.Length; i++)
        {
            if (Path.GetFileNameWithoutExtension(eyeFiles[i])!.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            {
                eyeTimings[i] = short.Parse(Path.GetFileNameWithoutExtension(eyeFiles[i]).Split('_').Last()[..^1]);
            }
        }
        List<(SKBitmap Frame, short Timing)> eyeFramesAndTimings = eyeFrames.Zip(eyeTimings).ToList();

        List<SKBitmap> mouthFrames = mouthFiles.OrderBy(f => f).Select(f => SKBitmap.Decode(f)).ToList();
        short[] mouthTimings = new short[mouthFrames.Count];
        Array.Fill<short>(mouthTimings, 32);
        for (int i = 0; i < mouthTimings.Length; i++)
        {
            if (Path.GetFileNameWithoutExtension(mouthFiles[i])!.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            {
                mouthTimings[i] = short.Parse(Path.GetFileNameWithoutExtension(mouthFiles[i]).Split('_').Last()[..^1]);
            }
        }
        List<(SKBitmap Frame, short Timing)> mouthFramesAndTimings = mouthFrames.Zip(mouthTimings).ToList();

        _sprite.SetSprite(baseLayout, eyeFramesAndTimings, mouthFramesAndTimings, eyePosX, eyePosY, mouthPosX, mouthPosY);
        Description.UnsavedChanges = true;
    }

    private async Task ExportFrames()
    {
        IStorageFolder dir = await Window.Window.ShowOpenFolderPickerAsync(Strings.CharacterSpriteEditorSelectExportFolder);
        if (dir is not null)
        {
            SKBitmap layout = _sprite.GetBaseLayout();
            List<(SKBitmap frame, short timing)> eyeFrames = _sprite.GetEyeFrames();
            List<(SKBitmap frame, short timing)> mouthFrames = _sprite.GetMouthFrames();

            try
            {
                using FileStream layoutStream = File.Create(Path.Combine(dir.Path.LocalPath, $"{_sprite.DisplayName}_BODY" +
                    $"_E{_sprite.Graphics.EyeAnimation.AnimationX},{_sprite.Graphics.EyeAnimation.AnimationY}" +
                    $"_M{_sprite.Graphics.MouthAnimation.AnimationX},{_sprite.Graphics.MouthAnimation.AnimationY}.png"));
                layout.Encode(layoutStream, SKEncodedImageFormat.Png, 1);
            }
            catch (Exception ex)
            {
                _log.LogException(string.Format(Strings.ErrorFailedExportingSpriteLayout, _sprite.DisplayName), ex);
            }

            int i = 0;
            foreach ((SKBitmap frame, short timing) in eyeFrames)
            {
                try
                {
                    using FileStream frameStream = File.Create(Path.Combine(dir.Path.LocalPath, $"{_sprite.DisplayName}_EYE_{i++:D3}_{timing}f.png"));
                    frame.Encode(frameStream, SKEncodedImageFormat.Png, 1);
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Strings.ErrorFailedExportingEyeAnimation, i, _sprite.DisplayName), ex);
                }
            }
            i = 0;
            foreach ((SKBitmap frame, short timing) in mouthFrames)
            {
                try
                {
                    using FileStream frameStream = File.Create(Path.Combine(dir.Path.LocalPath, $"{_sprite.DisplayName}_MOUTH_{i++:D3}_{timing}f.png"));
                    frame.Encode(frameStream, SKEncodedImageFormat.Png, 1);
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Strings.ErrorFailedExportingMouthAnimation, i, _sprite.DisplayName), ex);
                }
            }
            await Window.Window.ShowMessageBoxAsync(Strings.MessageBoxTitleSuccessGeneric, Strings.CharacterSpriteEditorFramesExportedMessage, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success, _log);
        }
    }

    private async Task ExportGIF()
    {
        List<(SKBitmap bitmap, int timing)> animationFrames;
        if (await Window.Window.ShowMessageBoxAsync(Strings.ChibiEditorAnimationExportOptionMessageBoxTitle, Strings.CharacterSpriteEditorIncludeLipFlapAnimationTitle, MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question, _log) == MsBox.Avalonia.Enums.ButtonResult.Yes)
        {
            animationFrames = _sprite.GetLipFlapAnimation(Window.OpenProject);
        }
        else
        {
            animationFrames = _sprite.GetClosedMouthAnimation(Window.OpenProject);
        }

        IStorageFile saveFile = await Window.Window.ShowSaveFilePickerAsync(Strings.CharacterSpriteEditorSaveGifFileDialogTitle, [new(Strings.FiletypeGif) { Patterns = ["*.gif"] }]);
        if (saveFile is not null)
        {
            List<SKBitmap> frames = [];
            foreach ((SKBitmap frame, int timing) in animationFrames)
            {
                for (int i = 0; i < timing; i++)
                {
                    frames.Add(frame);
                }
            }

            ProgressDialogViewModel tracker = new(Strings.AnimationExportingGifProgressMessage);
            tracker.InitializeTasks(() => frames.SaveGif(saveFile.Path.LocalPath, tracker),
                async void () => await Window.Window.ShowMessageBoxAsync(Strings.MessageBoxTitleSuccessGeneric, Strings.AnimationExportedGifSuccessMessage, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success, _log));
            await new ProgressDialog { DataContext = tracker }.ShowDialog(Window.Window);
        }
    }

    [GeneratedRegex(@"_(?<frameCount>\d{1,4})f(?:_|$)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex FrameCountRegex();
    [GeneratedRegex(@"_E(?<x>\d{1,3}),(?<y>\d{1,3})(?:_|$)")]
    private static partial Regex EyePosRegex();
    [GeneratedRegex(@"_M(?<x>\d{1,3}),(?<y>\d{1,3})(?:_|$)")]
    private static partial Regex MouthPosRegex();
}
