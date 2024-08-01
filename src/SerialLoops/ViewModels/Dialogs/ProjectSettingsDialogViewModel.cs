using System.Linq;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs
{
    public class ProjectSettingsDialogViewModel : ReactiveObject
    {
        public int MinWidth => 550;
        public int MinHeight => 600;
        public int Width { get; set; } = 550;
        public int Height { get; set; } = 600;

        public ICommand ReplaceCommand { get; private set; }
        public ICommand ApplyCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private string _gameTitle;
        public string GameTitle
        {
            get => _gameTitle;
            set => this.RaiseAndSetIfChanged(ref _gameTitle, value);
        }

        private SKBitmap _icon;
        public SKBitmap Icon
        {
            get
            {
                SKBitmap preview = new(64, 64);
                _icon.ScalePixels(preview, SKFilterQuality.None);
                return preview;
            }
            set => this.RaiseAndSetIfChanged(ref _icon, value);
        }

        private ProjectSettings _settings;
        public ILogger Log { get; set; }
        public bool Applied { get; set; }
        private ProjectSettingsDialog _settingsDialog;

        public void Initialize(ProjectSettingsDialog settingsDialog, ProjectSettings settings, ILogger log)
        {
            _settingsDialog = settingsDialog;
            _settings = settings;

            Icon = settings.Icon;
            GameTitle = settings.Name;
            Log = log;

            ReplaceCommand = ReactiveCommand.Create(ReplaceCommand_Executed);
            ApplyCommand = ReactiveCommand.Create(ApplyCommand_Executed);
            CancelCommand = ReactiveCommand.Create(_settingsDialog.Close);
        }

        private async void ReplaceCommand_Executed()
        {
            FilePickerOpenOptions options = new()
            {
                Title = Strings.Open_ROM,
                FileTypeFilter = [new FilePickerFileType(Strings.Chokuretsu_ROM) { Patterns = [ "*.png", ".jpg", "*.jpeg", "*.bmp", "*.gif" ] }]
            };
            IStorageFile image = (await _settingsDialog.StorageProvider.OpenFilePickerAsync(options)).FirstOrDefault();
            if (image is null)
            {
                return;
            }
            SKBitmap fileImage = SKBitmap.Decode(image.TryGetLocalPath());
            if (fileImage is not null)
            {
                SKBitmap newIcon = new(32, 32);
                fileImage.ScalePixels(newIcon, SKFilterQuality.High);
                Icon = newIcon;
                return;
            }
            Log.LogError(Strings.Invalid_image_file_selected);
        }

        private async void ApplyCommand_Executed()
        {
            if (_gameTitle.Length is < 1 or > 127)
            {
                Log.LogError(Strings.Please_enter_a_game_name_for_the_banner__between_1_and_128_characters_);
                return;
            }

            if (_gameTitle.Split('\n').Length > 3)
            {
                Log.LogError(Strings.Game_banner_can_only_contain_up_to_three_lines_);
                return;
            }
            _settings.Name = _gameTitle;

            if (Icon is not null)
            {
                _settings.Icon = _icon;
            }

            Applied = true;
            _settingsDialog.Close();
        }
    }
}
