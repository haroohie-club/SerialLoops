using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using OpenTK.Graphics.OpenGL4;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Hacks;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public partial class AsmHackCreationDialogViewModel : ViewModelBase
{
    public ObservableCollection<HackFileContainer> HackFiles { get; set; } = [];
    [Reactive]
    public HackFileContainer SelectedHackFile { get; set; }
    [Reactive]
    public string Name { get; set; }
    [Reactive]
    public string Description { get; set; }
    public List<InjectionSite> InjectionSites { get; } = [];

    private ILogger _log;
    public ICommand SelectHackFilesCommand { get; set; }
    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    public AsmHackCreationDialogViewModel(ILogger log)
    {
        _log = log;
        SelectHackFilesCommand = ReactiveCommand.CreateFromTask<AsmHackCreationDialog>(SelectHackFiles);
        SaveCommand = ReactiveCommand.Create<AsmHackCreationDialog>(dialog => dialog.Close(((Name, Description, HackFiles.ToArray(), InjectionSites.ToArray()))));
        CancelCommand = ReactiveCommand.Create<AsmHackCreationDialog>(dialog => dialog.Close());
    }

    private async Task SelectHackFiles(AsmHackCreationDialog dialog)
    {
        IStorageFile[] hackStorageFiles =
        [
            .. await dialog.ShowOpenMultiFilePickerAsync(Strings.Select_Hack_Files,
                [new("Valid Source Files") { Patterns = ["*.s", "*.c"] }]),
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
            if (paths[i].EndsWith(".s", StringComparison.OrdinalIgnoreCase))
            {
                var injectionSiteMatches = InjectionSitesRegex().Matches(hackFiles[i]);
                foreach (Match match in injectionSiteMatches)
                {
                    string injectionSite = match.Groups["injectionSite"].Value;
                    if (!uint.TryParse(injectionSite, NumberStyles.HexNumber,
                            CultureInfo.CurrentCulture.NumberFormat, out uint injectionSiteAddress))
                    {
                        _log.LogWarning($"Failed to parse injection site {injectionSite}");
                        continue;
                    }
                    // If it's overlay code, temporarily set it to overlay 1 until we can ask the user which overlay it belongs to
                    InjectionSites.Add(new() { Code = (injectionSiteAddress < 0x020C7660 ? "ARM9" : "01"),  Location = injectionSite });
                }
            }
        }

        List<string> definedSymbols = [];
        for (int i = 0; i < hackFiles.Count; i++)
        {
            if (paths[i].EndsWith(".s", StringComparison.OrdinalIgnoreCase))
            {
                var asmDefinedSymbolMatches = AsmDefinedSymbolRegex().Matches(hackFiles[i]);
                definedSymbols.AddRange(asmDefinedSymbolMatches.Select(m => m.Groups["symbol"].Value));
            }
            else
            {
                var cAllSymbolMatches = CSymbolRegex().Matches(hackFiles[i]);
                var cUndefinedSymbolMatches = CUndefinedSymbolRegex().Matches(hackFiles[i]);
                definedSymbols.AddRange(cAllSymbolMatches.Select(m => m.Groups["symbol"].Value)
                    .Where(s => !cUndefinedSymbolMatches.Select(m => m.Groups["symbol"].Value).Contains(s)));
            }
        }

        for (int i = 0; i < hackFiles.Count; i++)
        {
            string[] symbols;
            if (paths[i].EndsWith(".s", StringComparison.OrdinalIgnoreCase))
            {
                var allAsmSymbolMatches = AsmSymbolRegex().Matches(hackFiles[i]);
                symbols = [.. allAsmSymbolMatches.Select(m => m.Groups["symbol"].Value)
                        .Where(s => !definedSymbols.Contains(s)).Distinct()];
            }
            else
            {
                var cSymbolMatches = CUndefinedSymbolRegex().Matches(hackFiles[i]);
                symbols = [.. cSymbolMatches.Select(m => m.Groups["symbol"].Value)
                    .Where(s => !definedSymbols.Contains(s)).Distinct()];
            }

            var paramMatches = ParamRegex().Matches(hackFiles[i]);
            string[] parameters = [.. paramMatches.Select(m => m.Groups["name"].Value)];

            HackFiles.Add(new(paths[i], hackFiles[i], symbols, parameters, InjectionSites));
        }
    }

    [GeneratedRegex(@"#{{(?<name>\w+)}}")]
    private static partial Regex ParamRegex();
    [GeneratedRegex(@"(?<symbol>\w+):")]
    private static partial Regex AsmDefinedSymbolRegex();
    [GeneratedRegex(@"(?:bl |=)(?!0x)(?<symbol>[A-z]\w*)")]
    private static partial Regex AsmSymbolRegex();
    [GeneratedRegex(@"extern (?:\w[\w\[\]\*]*) \*?(?<symbol>\w+)")]
    private static partial Regex CUndefinedSymbolRegex();
    [GeneratedRegex(@"(?:\w[\w\[\]\*]*) \*?(?<symbol>\w+)\(")]
    private static partial Regex CSymbolRegex();
    [GeneratedRegex(@"ahook_(?<injectionSite>\w{7,8}):")]
    private static partial Regex InjectionSitesRegex();
}

public class HackFileContainer(string hackFilePath, string hackFileContent, IEnumerable<string> symbols, IEnumerable<string> parameters, IEnumerable<InjectionSite> injectionSites) : ReactiveObject
{
    private InjectionSite[] _injectionSites = [.. injectionSites];
    public string HackFilePath { get; } = hackFilePath;
    public string HackFileName => Path.GetFileName(HackFilePath);
    public string HackFileContent { get; } = hackFileContent;

    public ObservableCollection<string> Locations { get; } = new(Enum.GetNames<HackLocation>());
    private HackLocation _location = HackLocation.ARM9;
    public string Location
    {
        get => _location.ToString();
        set
        {
            if (value is null)
            {
                return;
            }
            this.RaiseAndSetIfChanged(ref _location, Enum.Parse<HackLocation>(value));
            foreach (InjectionSite injectionSite in _injectionSites)
            {
                injectionSite.Code = _location == HackLocation.ARM9 ? "ARM9" : $"{(int)_location:D2}";
            }
        }
    }
    public string Destination => _location switch
    {
        HackLocation.ARM9 => $"source/{HackFileName}",
        _ => $"overlays/main{(int)_location:X4}/source/{HackFileName}",
    };

    public ObservableCollection<HackSymbolContainer> Symbols { get; } = new(symbols.Select(s => new HackSymbolContainer(s)));
    public ObservableCollection<HackParameterContainer> Parameters { get; } = new(parameters.Select(p => new HackParameterContainer(p)));
}

public class HackSymbolContainer(string symbol) : ReactiveObject
{
    private uint _location;
    public uint Location => _location;

    public string LocationString
    {
        get => $"{_location:X7}";
        set
        {
            if (uint.TryParse(value, NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier,
                    CultureInfo.CurrentCulture.NumberFormat, out uint location))
            {
                this.RaiseAndSetIfChanged(ref _location, location);
            }
        }
    }

    [Reactive]
    public string Symbol { get; set; } = symbol;
}

public class HackParameterContainer : ReactiveObject
{
    public ICommand AddValueCommand { get; }

    public string Name { get; }
    [Reactive]
    public string DescriptiveName { get; set; }
    public ObservableCollection<HackParameterValueContainer> Values { get; } = [];

    public HackParameterContainer(string name)
    {
        Name = name;
        DescriptiveName = string.Empty;
        AddValueCommand = ReactiveCommand.Create(() => Values.Add(new("", "", this)));
    }
}

public class HackParameterValueContainer : ReactiveObject
{
    public ICommand RemoveValueCommand { get; }

    [Reactive]
    public string Name { get; set; }
    [Reactive]
    public string Value { get; set; }

    public HackParameterValueContainer(string name, string value, HackParameterContainer parent)
    {
        Name = name;
        Value = value;
        RemoveValueCommand = ReactiveCommand.Create(() => parent.Values.Remove(this));
    }
}
