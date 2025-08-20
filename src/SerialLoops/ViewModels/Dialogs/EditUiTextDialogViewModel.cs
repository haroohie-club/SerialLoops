using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using SerialLoops.Views.Dialogs;
using IO = SerialLoops.Lib.IO;

namespace SerialLoops.ViewModels.Dialogs;

public class EditUiTextDialogViewModel : ViewModelBase
{
    public ObservableCollection<UiTextWithDescription> UiTextStrings { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditUiTextDialogViewModel(Project project, ILogger log)
    {
        UiTextStrings = [];
        for (int i = 0; i < project.UiText.Messages.Count; i++)
        {
            if (!UiDescriptions.TryGetValue(i, out string text))
            {
                continue;
            }

            UiTextStrings.Add(new(project, text, i));
        }

        SaveCommand = ReactiveCommand.Create<EditUiTextDialog>(dialog =>
        {
            foreach (UiTextWithDescription uiText in UiTextStrings)
            {
                project.UiText.Messages[uiText.Index] = uiText.UiText.GetOriginalString(project);
            }
            IO.WriteStringFile(Path.Combine("assets", "data", $"{project.UiText.Index:X3}.s"),
                project.UiText.GetSource([]), project, log);
            dialog.Close();
        });
        CancelCommand = ReactiveCommand.Create<EditUiTextDialog>(dialog => dialog.Close());
    }

    private static readonly Dictionary<int, string> UiDescriptions = new()
    {
        { 2, Strings.UiMessage02Label },
        { 3, Strings.UiMessage03Label },
        { 4, Strings.UiMessage04Label },
        { 5, Strings.UiMessage05Label },
        { 6, Strings.UiMessage06Label },
        { 7, Strings.UiMessage07Label },
        { 8, Strings.UiMessage08Label },
        { 9, Strings.UiMessage09Label },
        { 10, Strings.UiMessage10Label },
        { 11, Strings.UiMessage11Label },
        { 12, Strings.UiMessage12Label },
        { 13, Strings.UiMessage13Label },
        { 14, Strings.UiMessage14Label },
        { 15, Strings.UiMessage15Label },
        { 16, Strings.UiMessage16Label },
        { 17, Strings.UiMessage17Label },
        { 18, Strings.UiMessage18Label },
        { 19, Strings.UiMessage19Label },
        { 20, Strings.UiMessage20Label },
        { 21, Strings.UiMessage21Label },
        { 22, Strings.UiMessage22Label },
        { 23, Strings.UiMessage23Label },
        { 24, Strings.UiMessage24Label },
        { 25, Strings.UiMessage25Label },
        { 26, Strings.UiMessage26Label },
        { 27, Strings.UiMessage27Label },
        { 28, Strings.UiMessage28Label },
        { 29, Strings.UiMessage29Label },
        { 30, Strings.UiMessage30Label },
        { 31, Strings.Yes },
        { 32, Strings.No },
        { 33, Strings.UiMessage33Label },
        { 34, Strings.UiMessage34Label },
        { 35, Strings.UiMessage35Label },
        { 36, Strings.UiMessage36Label },
        { 37, Strings.UiMessage37Label },
        { 38, Strings.UiMessage38Label },
        { 39, Strings.UiMessage39Label },
        { 40, Strings.UiMessage40Label },
        { 41, Strings.UiMessage41Label },
        { 42, Strings.UiMessage42Label },
        { 43, Strings.UiMessage43Label },
        { 44, Strings.UiMessage44Label },
        { 45, Strings.UiMessage34Label },
        { 46, Strings.UiMessage46Label },
        { 47, Strings.UiMessage47Label },
        { 48, Strings.UiMessage48Label },
        { 49, Strings.UiMessage49Label },
        { 50, Strings.UiMessage50Label },
        { 51, Strings.UiMessage51Label },
        { 52, Strings.UiMessage52Label },
        { 53, Strings.UiMessage53Label },
        { 54, Strings.UiMessage54Label },
        { 55, Strings.UiMessage55Label },
        { 56, Strings.UiMessage56Label },
        { 57, Strings.UiMessage57Label },
        { 58, Strings.UiMessage58Label },
        { 59, Strings.UiMessage59Label },
        { 60, Strings.UiMessage60Label },
        { 61, Strings.UiMessage61Label },
        { 62, Strings.UiMessage62Label },
        { 63, Strings.UiMessage63Label },
        { 64, Strings.UiMessage64Label },
        { 65, Strings.UiMessage65Label },
        { 66, Strings.UiMessage66Label },
        { 67, Strings.UiMessage67Label },
        { 68, Strings.UiMessage68Label },
        { 69, Strings.UiMessage69Label },
        { 70, Strings.UiMessage70Label },
        { 71, Strings.UiMessage71Label },
        { 72, Strings.UiMessage72Label },
        { 73, Strings.UiMessage73Label },
        { 74, Strings.UiMessage74Label },
        { 75, Strings.UiMessage75Label },
        { 76, Strings.UiMessage76Label },
        { 77, Strings.UiMessage77Label },
        { 78, Strings.UiMessage78Label },
        { 79, Strings.UiMessage79Label },
        { 80, Strings.UiMessage80Label },
        { 81, Strings.UiMessage81Label },
        { 82, Strings.UiMessage82Label },
        { 83, Strings.UiMessage83Label },
        { 84, Strings.UiMessage84Label },
        { 85, Strings.UiMessage85Label },
        { 86, Strings.UiMessage86Label },
        { 87, Strings.UiMessage87Label },
        { 88, Strings.UiMessage88Label },
        { 89, Strings.UiMessage89Label },
        { 90, Strings.UiMessage90Label },
        { 91, Strings.UiMessage91Label },
        { 92, Strings.UiMessage92Label },
        { 93, Strings.UiMessage93Label },
        { 94, Strings.UiMessage94Label },
        { 95, Strings.UiMessage95Label },
        { 96, Strings.UiMessage96Label },
        { 97, Strings.UiMessage97Label },
        { 98, Strings.UiMessage98Label },
        { 99, Strings.UiMessage99Label },
        { 100, Strings.UiMessage100Label },
        { 101, Strings.UiMessage101Label },
        { 102, Strings.UiMessage102Label },
        { 103, Strings.UiMessage103Label },
        { 104, Strings.UiMessage104Label },
        { 105, Strings.UiMessage105Label },
        { 106, Strings.UiMessage106Label },
        { 107, Strings.UiMessage107Label },
        { 108, Strings.UiMessage108Label },
        { 109, Strings.UiMessage109Label },
        { 111, Strings.UiMessage111Label },
        { 112, Strings.UiMessage112Label },
        { 115, Strings.UiMessage115Label },
        { 116, Strings.UiMessage116Label },
        { 117, Strings.UiMessage117Label },
        { 118, Strings.UiMessage118Label },
        { 119, Strings.UiMessage119Label },
        { 120, Strings.UiMessage120Label },
        { 121, Strings.UiMessage121Label },
        { 122, Strings.UiMessage122Label },
        { 123, Strings.UiMessage123Label },
        { 124, Strings.UiMessage124Label },
        { 125, Strings.UiMessage125Label },
        { 126, Strings.UiMessage126Label },
        { 127, Strings.UiMessage127Label  },
        { 128, Strings.UiMessage128Label },
        { 129, Strings.UiMessage129Label },
    };
}

public class UiTextWithDescription(Project project, string description, int index) : ReactiveObject
{
    public string Description { get; } = description;

    [Reactive]
    public string UiText { get; set; } = project.UiText.Messages[index].GetSubstitutedString(project);

    public int Index { get; } = index;
}
