using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class ImportProjectDialogViewModel
{
    public string RomHashString { get; set; }
    public string SlzipPath { get; set; }
    public string RomPath { get; set; }

    public ICommand SelectExportedProjectCommand { get; }
    public ICommand OpenRomCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand CancelCommand { get; }

    public ImportProjectDialogViewModel()
    {
        RomHashString = string.Format(Strings.Expected_ROM_SHA_1_Hash___0_,
            Strings.Select_an_exported_project_to_see_expected_ROM_hash);

        SelectExportedProjectCommand = ReactiveCommand.CreateFromTask<ImportProjectDialog>(SelectExportedProject);
        OpenRomCommand = ReactiveCommand.CreateFromTask<ImportProjectDialog>(OpenRom);
        ImportCommand = ReactiveCommand.Create<ImportProjectDialog>(dialog => dialog.Close((SlzipPath, RomPath)));
        CancelCommand = ReactiveCommand.Create<ImportProjectDialog>(dialog => dialog.Close());
    }

    private async Task SelectExportedProject(ImportProjectDialog dialog)
    {

    }

    private async Task OpenRom(ImportProjectDialog dialog)
    {

    }
}
