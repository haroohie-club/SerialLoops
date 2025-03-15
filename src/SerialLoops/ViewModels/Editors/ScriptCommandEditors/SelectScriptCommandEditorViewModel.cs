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

    private short _displayFlag1;
    public short DisplayFlag1
    {
        get => _displayFlag1;
        set
        {
            this.RaiseAndSetIfChanged(ref _displayFlag1, value);
            EditDisplayFlag(0, _displayFlag1);
        }
    }
    private short _displayFlag2;
    public short DisplayFlag2
    {
        get => _displayFlag2;
        set
        {
            this.RaiseAndSetIfChanged(ref _displayFlag2, value);
            EditDisplayFlag(1, _displayFlag2);
        }
    }
    private short _displayFlag3;
    public short DisplayFlag3
    {
        get => _displayFlag3;
        set
        {
            this.RaiseAndSetIfChanged(ref _displayFlag3, value);
            EditDisplayFlag(2, _displayFlag3);
        }
    }
    private short _displayFlag4;
    public short DisplayFlag4
    {
        get => _displayFlag4;
        set
        {
            this.RaiseAndSetIfChanged(ref _displayFlag4, value);
            EditDisplayFlag(3, _displayFlag4);
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
        _displayFlag1 = ((ShortScriptParameter)Command.Parameters[4]).Value;
        _displayFlag2 = ((ShortScriptParameter)Command.Parameters[5]).Value;
        _displayFlag3 = ((ShortScriptParameter)Command.Parameters[6]).Value;
        _displayFlag4 = ((ShortScriptParameter)Command.Parameters[7]).Value;
    }

    private void EditOptionParameter(int index, ChoicesSectionEntry option)
    {
        ((OptionScriptParameter)Command.Parameters[index]).Option = option;
        Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
            .Objects[Command.Index].Parameters[index] = Script.Event.ChoicesSection.Objects.IndexOf(option) == -1 ? (short)0 : (short)Script.Event.ChoicesSection.Objects.IndexOf(option) ;
        Script.UnsavedChanges = true;
        ScriptEditor.UpdatePreview();
        Script.Refresh(OpenProject, Log);
    }

    private void EditDisplayFlag(int index, short displayFlag)
    {
        ((ShortScriptParameter)Command.Parameters[index + 4]).Value = displayFlag;
        Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
            .Objects[Command.Index].Parameters[index + 4] = displayFlag;
        Script.UnsavedChanges = true;
    }
}
