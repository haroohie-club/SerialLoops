using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Script;
using SerialLoops.Utility;
using SerialLoops.Views.Dialogs;
using SoftCircuits.Collections;

namespace SerialLoops.ViewModels.Dialogs;

public class GenerateTemplateDialogViewModel
{
    public string TemplateName { get; set; }
    public string TemplateDescription { get; set; }

    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    private OrderedDictionary<ScriptSection, List<ScriptItemCommand>> _commands;
    private Project _project;
    private ILogger _log;

    public GenerateTemplateDialogViewModel(OrderedDictionary<ScriptSection, List<ScriptItemCommand>> commands,
        Project project, ILogger log)
    {
        _commands = commands;
        _project = project;
        _log = log;
        CreateCommand = ReactiveCommand.CreateFromTask<GenerateTemplateDialog>(GenerateTemplate);
        CancelCommand = ReactiveCommand.Create<GenerateTemplateDialog>((dialog) => dialog.Close());
    }

    private async Task GenerateTemplate(GenerateTemplateDialog dialog)
    {
        if (_project.Config.ScriptTemplates.Any(t => t.Name.Equals(TemplateName, StringComparison.OrdinalIgnoreCase)))
        {
            await dialog.ShowMessageBoxAsync(Strings.Template_Already_Exists, Strings.There_is_already_a_template_with_that_name__Please_choose_a_different_name_for_your_template_,
                ButtonEnum.Ok, Icon.Warning, _log);
            return;
        }
        dialog.Close(new ScriptTemplate(TemplateName, TemplateDescription, _commands, _project));
    }
}
