using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class SelectScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public Project OpenProject { get; }
    public ObservableCollection<ChoicesSectionEntry> AvailableChoices { get; }

    private ChoicesSectionEntry _option1;
    public ChoicesSectionEntry Option1
    {
        get => _option1;
        set
        {
            this.RaiseAndSetIfChanged(ref _option1, value);
            EditOptionParameter(0, _option1);
        }
    }
    private ChoicesSectionEntry _option2;
    public ChoicesSectionEntry Option2
    {
        get => _option2;
        set
        {
            this.RaiseAndSetIfChanged(ref _option2, value);
            EditOptionParameter(1, _option2);
        }
    }
    private ChoicesSectionEntry _option3;
    public ChoicesSectionEntry Option3
    {
        get => _option3;
        set
        {
            this.RaiseAndSetIfChanged(ref _option3, value);
            EditOptionParameter(2, _option3);
        }
    }
    private ChoicesSectionEntry _option4;
    public ChoicesSectionEntry Option4
    {
        get => _option4;
        set
        {
            this.RaiseAndSetIfChanged(ref _option4, value);
            EditOptionParameter(3, _option4);
        }
    }

    private string _condtional1;
    public string Conditional1
    {
        get => _condtional1;
        set
        {
            this.RaiseAndSetIfChanged(ref _condtional1, value);
            EditConditional(0, _condtional1);
        }
    }
    private string _condtional2;
    public string Conditional2
    {
        get => _condtional2;
        set
        {
            this.RaiseAndSetIfChanged(ref _condtional2, value);
            EditConditional(1, _condtional2);
        }
    }
    private string _condtional3;
    public string Conditional3
    {
        get => _condtional3;
        set
        {
            this.RaiseAndSetIfChanged(ref _condtional3, value);
            EditConditional(2, _condtional3);
        }
    }
    private string _condtional4;
    public string Conditional4
    {
        get => _condtional4;
        set
        {
            this.RaiseAndSetIfChanged(ref _condtional4, value);
            EditConditional(3, _condtional4);
        }
    }

    public SelectScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, Project project) :
        base(command, scriptEditor, log)
    {
        OpenProject = project;
        AvailableChoices =
            new(new List<ChoicesSectionEntry> { new() { Text = "NONE", Id = 0 } }.Concat(Script.Event.ChoicesSection
                .Objects.Skip(1).SkipLast(1)));
        _option1 = ((OptionScriptParameter)Command.Parameters[0]).Option.Id == 0 ? AvailableChoices[0] : ((OptionScriptParameter)Command.Parameters[0]).Option;
        _option2 = ((OptionScriptParameter)Command.Parameters[1]).Option.Id == 0 ? AvailableChoices[0] : ((OptionScriptParameter)Command.Parameters[1]).Option;
        _option3 = ((OptionScriptParameter)Command.Parameters[2]).Option.Id == 0 ? AvailableChoices[0] : ((OptionScriptParameter)Command.Parameters[2]).Option;
        _option4 = ((OptionScriptParameter)Command.Parameters[3]).Option.Id == 0 ? AvailableChoices[0] : ((OptionScriptParameter)Command.Parameters[3]).Option;
        _condtional1 = ((ConditionalScriptParameter)Command.Parameters[4]).Conditional;
        _condtional2 = ((ConditionalScriptParameter)Command.Parameters[5]).Conditional;
        _condtional3 = ((ConditionalScriptParameter)Command.Parameters[6]).Conditional;
        _condtional4 = ((ConditionalScriptParameter)Command.Parameters[7]).Conditional;
    }

    private void EditOptionParameter(int index, ChoicesSectionEntry option)
    {
        ((OptionScriptParameter)Command.Parameters[index]).Option = option;
        Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
            .Objects[Command.Index].Parameters[index] = Script.Event.ChoicesSection.Objects.IndexOf(option) == -1
            ? (short)0
            : (short)Script.Event.ChoicesSection.Objects.IndexOf(option);
        Script.UnsavedChanges = true;
        ScriptEditor.UpdatePreview();
        Script.Refresh(OpenProject, Log);
    }

    private void EditConditional(int index, string conditional)
    {
        ((ConditionalScriptParameter)Command.Parameters[index + 4]).Conditional = conditional;
        if (string.IsNullOrEmpty(conditional))
        {
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[index + 4] = -1;
            Script.UnsavedChanges = true;
            return;
        }
        Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[index + 4] = (short)Script.Event.ConditionalsSection.Objects.Count;
        Script.Event.ConditionalsSection.Objects.Add(conditional);
        Script.UnsavedChanges = true;
    }
}
