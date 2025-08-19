using System.IO;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class ProjectRenameDuplicateDialogViewModel : ViewModelBase
{
    public string Title { get; }
    public string SubmitText { get; }

    [Reactive]
    public string NewName { get; set; }

    public ICommand CancelCommand { get; }
    public ICommand SubmitCommand { get; }

    public ProjectRenameDuplicateDialogViewModel(bool rename, string project, ConfigUser configUser, ILogger log)
    {
        Title = rename ? Strings.ProjectRenameText : Strings.ProjectDuplicateText;
        SubmitText = rename ? Strings.Rename : Strings.ProjectDuplicateButtonLabel;

        CancelCommand = ReactiveCommand.Create<ProjectRenameDuplicateDialog>(dialog => dialog.Close());
        SubmitCommand = ReactiveCommand.CreateFromTask<ProjectRenameDuplicateDialog>(async dialog =>
        {
            if (string.IsNullOrEmpty(NewName))
            {
                await dialog.ShowMessageBoxAsync(Strings.ProjectRenameDuplicateMustEnterNameErrorTitle,
                    Strings.ProjectRenameDuplicateMustEnterNameErrorText,
                    ButtonEnum.Ok, Icon.Forbidden, log);
                return;
            }

            string newPath = Path.Combine(configUser.ProjectsDirectory, NewName);
            if (Directory.Exists(newPath))
            {
                await dialog.ShowMessageBoxAsync(Strings.ProjectAlreadyExistsErrorTitle, Strings.ProjectAlreadyExistsErrorText,
                    ButtonEnum.Ok, Icon.Forbidden, log);
                return;
            }

            string oldName = Path.GetFileNameWithoutExtension(project);
            if (rename)
            {
                Directory.Move(Path.GetDirectoryName(project)!, newPath);
            }
            else
            {
                Lib.IO.CopyDirectoryRecursively(Path.GetDirectoryName(project)!, newPath);
            }

            string newProj = Path.Combine(newPath, $"{NewName}.{Project.PROJECT_FORMAT}");
            File.Move(Path.Combine(newPath, $"{oldName}.{Project.PROJECT_FORMAT}"), newProj);
            if (File.Exists(Path.Combine(newPath, $"{oldName}.nds")))
            {
                File.Move(Path.Combine(newPath, $"{oldName}.nds"), Path.Combine(newPath, $"{NewName}.nds"));
            }
            if (File.Exists(Path.Combine(newPath, $"{oldName}.sav")))
            {
                File.Move(Path.Combine(newPath, $"{oldName}.sav"), Path.Combine(newPath, $"{NewName}.sav"));
            }
            if (File.Exists(Path.Combine(newPath, "base", "original", $"{oldName}.json")))
            {
                File.Move(Path.Combine(newPath, "base", "original", $"{oldName}.json"), Path.Combine(newPath, "base", "original", $"{NewName}.json"));
            }
            if (File.Exists(Path.Combine(newPath, "base", "rom", $"{oldName}.json")))
            {
                File.Move(Path.Combine(newPath, "base", "rom", $"{oldName}.json"), Path.Combine(newPath, "base", "rom", $"{NewName}.json"));
            }
            if (File.Exists(Path.Combine(newPath, "iterative", "original", $"{oldName}.json")))
            {
                File.Move(Path.Combine(newPath, "iterative", "original", $"{oldName}.json"), Path.Combine(newPath, "iterative", "original", $"{NewName}.json"));
            }
            if (File.Exists(Path.Combine(newPath, "iterative", "rom", $"{oldName}.json")))
            {
                File.Move(Path.Combine(newPath, "iterative", "rom", $"{oldName}.json"), Path.Combine(newPath, "iterative", "rom", $"{NewName}.json"));
            }

            Project deserializedProject = Project.Deserialize(newProj);
            deserializedProject.ConfigUser = configUser;
            deserializedProject.Name = NewName;
            deserializedProject.Save(log);

            dialog.Close(newProj);
        });
    }
}
