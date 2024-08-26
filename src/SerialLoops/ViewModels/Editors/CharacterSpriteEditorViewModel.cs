using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Controls;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors
{
    public class CharacterSpriteEditorViewModel : EditorViewModel
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

        public ICommand ReplaceCommand { get; set; }
        public ICommand ExportFramesCommand { get; set; }
        public ICommand ExportGIFCommand { get; set; }

        public CharacterSpriteEditorViewModel(CharacterSpriteItem item, MainWindowViewModel mainWindow, ILogger log) : base(item, mainWindow, log)
        {
            _sprite = item;
            AnimatedImage = new(_sprite.GetLipFlapAnimation(mainWindow.OpenProject));
            Characters = new(mainWindow.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Character).Cast<CharacterItem>());
            _character = (CharacterItem)mainWindow.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Character && ((CharacterItem)i).MessageInfo.Character == item.Sprite.Character);
            _isLarge = _sprite.Sprite.IsLarge;

            ReplaceCommand = ReactiveCommand.CreateFromTask(ReplaceSprite);
            ExportFramesCommand = ReactiveCommand.CreateFromTask(ExportFrames);
            ExportGIFCommand = ReactiveCommand.CreateFromTask(ExportGIF);
        }

        private async Task ReplaceSprite()
        {

        }

        private async Task ExportFrames()
        {
            IStorageFolder dir = await _window.Window.ShowOpenFolderPickerAsync(Strings.Select_character_sprite_export_folder);
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
                    _log.LogException(string.Format(Strings.Failed_to_export_layout_for_sprite__0__to_file, _sprite.DisplayName), ex);
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
                        _log.LogException(string.Format(Strings.Failed_to_export_eye_animation__0__for_sprite__1__to_file, i, _sprite.DisplayName), ex);
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
                        _log.LogException(string.Format(Strings.Failed_to_export_mouth_animation__0__for_sprite__1__to_file, i, _sprite.DisplayName), ex);
                    }
                }
                await _window.Window.ShowMessageBoxAsync(Strings.Success_, Strings.Character_sprite_frames_exported_, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success, _log);
            }
        }

        private async Task ExportGIF()
        {
            List<(SKBitmap bitmap, int timing)> animationFrames;
            if (await _window.Window.ShowMessageBoxAsync(Strings.Animation_Export_Option, Strings.Include_lip_flap_animation_, MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question, _log) == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                animationFrames = _sprite.GetLipFlapAnimation(_window.OpenProject);
            }
            else
            {
                animationFrames = _sprite.GetClosedMouthAnimation(_window.OpenProject);
            }

            IStorageFile saveFile = await _window.Window.ShowSaveFilePickerAsync(Strings.Save_character_sprite_GIF, [new FilePickerFileType(Strings.GIF_file) { Patterns = ["*.gif"] }]);
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

                LoopyProgressTracker tracker = new();
                await new ProgressDialog(() => frames.SaveGif(saveFile.Path.LocalPath, tracker),
                    async () => await _window.Window.ShowMessageBoxAsync(Strings.Success_, Strings.GIF_exported_, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success, _log),
                    tracker, Strings.Exporting_GIF___).ShowDialog(_window.Window);
            }
        }
    }
}
