using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Hacks;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public partial class AsmHackCreationDialogViewModel : ViewModelBase
{
    public ObservableCollection<HackFileContainer> HackFiles { get; set; }
    [Reactive]
    public HackFileContainer SelectedHackFile { get; set; }

    private ILogger _log;
    public ICommand SelectHackFilesCommand { get; set; }

    public AsmHackCreationDialogViewModel(ILogger log)
    {
        _log = log;
        SelectHackFilesCommand = ReactiveCommand.CreateFromTask<AsmHackCreationDialog>(SelectHackFiles);
    }

    private async Task SelectHackFiles(AsmHackCreationDialog dialog)
    {
        IStorageFile[] hackStorageFiles =
        [
            .. await dialog.ShowOpenMultiFilePickerAsync(Strings.Select_Hack_Files,
                [new("ARM assembly files") { Patterns = ["*.s"] }, new("C files") { Patterns = ["*.c"] }])
        ];
        if (hackStorageFiles is null || hackStorageFiles.Length == 0)
        {
            return;
        }
        string[] paths = [.. hackStorageFiles.Select(f => f.TryGetLocalPath())];
        if (paths.Any(string.IsNullOrEmpty))
        {
            _log.LogError("Failed to open hack file(s)!");
            return;
        }
        List<string> hackFiles = [];

        hackFiles.AddRange(paths.Select(File.ReadAllText));

        for (int i = 0; i < hackFiles.Count; i++)
        {
            List<string> parameters = [];
            var paramMatches = ParamRegex().Matches(hackFiles[i]);
            parameters.AddRange(paramMatches.Select(m => m.Groups["name"].Value));

            HackFiles.Add(new(paths[i], hackFiles[i], parameters));
        }
    }

    [GeneratedRegex(@"#{{(?<name>\w+)}}]")]
    private static partial Regex ParamRegex();
}

public class HackFileContainer(string hackFilePath, string hackFileContent, IEnumerable<string> parameters) : ReactiveObject
{
    public string HackFilePath { get; } = hackFilePath;
    public string HackFileName => Path.GetFileName(HackFilePath);
    public string HackFileContent { get; } = hackFileContent;
    public ObservableCollection<HackParameterContainer> Parameters { get; } = new(parameters.Select(p => new HackParameterContainer(p)));
}

public class HackParameterContainer(string name) : ReactiveObject
{
    public string Name { get; } = name;
    public ObservableCollection<HackParameterValue> Values { get; } = [];
}
