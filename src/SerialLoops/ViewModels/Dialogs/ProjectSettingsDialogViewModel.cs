using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs;

public class ProjectSettingsDialogViewModel : ViewModelBase
{
    public ICommand ReplaceCommand { get; private set; }
    public ICommand ApplyCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    [Reactive]
    public string GameTitle { get; set; }

    private SKBitmap _icon;
    public SKBitmap Icon
    {
        get
        {
            SKBitmap preview = new(64, 64);
            _icon.ScalePixels(preview, SKSamplingOptions.Default);
            return preview;
        }
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }

    private ProjectSettings _settings;
    public ILogger Log { get; set; }

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
        IStorageFile image = await _settingsDialog.ShowOpenFilePickerAsync(Strings.ProjectSettingsReplaceGameIcon, [new(Strings.FiletypeSupportedImages) { Patterns = Shared.SupportedImageFiletypes }]);
        if (image is null)
        {
            return;
        }
        SKBitmap fileImage = SKBitmap.Decode(image.TryGetLocalPath());
        if (fileImage is not null)
        {
            SKBitmap newIcon = new(32, 32);
            fileImage.ScalePixels(newIcon, Extensions.HighQualitySamplingOptions);
            Icon = newIcon;
            return;
        }
        Log.LogError(Strings.ErrorInvalidImageSelected);
    }

    private void ApplyCommand_Executed()
    {
        if (GameTitle.Length is < 1 or > 127)
        {
            Log.LogError(Strings.ProjectSettingsGameNameInvalidMessage);
            return;
        }

        if (GameTitle.Split('\n').Length > 3)
        {
            Log.LogError(Strings.ProjectSettingsGameBannerNameLimitMessages);
            return;
        }
        _settings.Name = GameTitle;

        if (Icon is not null)
        {
            _settings.Icon = _icon;
        }

        _settingsDialog.Close();
    }
}
