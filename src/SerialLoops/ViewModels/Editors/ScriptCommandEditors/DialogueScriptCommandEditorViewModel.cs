﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public partial class DialogueScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private MainWindowViewModel _window;
    public EditorTabsPanelViewModel Tabs { get; set; }
    private Func<ItemDescription, bool> _specialPredicate;
    private Timer _dialogueUpdateTimer;

    public DialogueScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) : base(command, scriptEditor, log)
    {
        _window = window;
        Tabs = _window.EditorTabs;
        Characters = new(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Character).Cast<CharacterItem>());
        _speaker = _window.OpenProject.GetCharacterBySpeaker(((DialogueScriptParameter)Command.Parameters[0]).Line.Speaker);
        _specialPredicate = i => i.Name != "NONE" && ((CharacterSpriteItem)i).Sprite.Character == _speaker.MessageInfo.Character;
        _dialogueLine = ((DialogueScriptParameter)command.Parameters[0]).Line.Text;
        _characterSprite = ((SpriteScriptParameter)command.Parameters[1]).Sprite;
        SelectCharacterSpriteCommand = ReactiveCommand.CreateFromTask(SelectCharacterSpriteCommand_Executed);
        _spriteEntranceTransition = new(((SpriteEntranceScriptParameter)command.Parameters[2]).EntranceTransition);
        _spriteExitTransition = new(((SpriteExitScriptParameter)command.Parameters[3]).ExitTransition);
        _spriteShakeEffect = new(((SpriteShakeScriptParameter)command.Parameters[4]).ShakeEffect);
        VoicedLines = new(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Voice).Cast<VoicedLineItem>());
        _voicedLine = ((VoicedLineScriptParameter)command.Parameters[5]).VoiceLine;
        _textVoiceFont = ((DialoguePropertyScriptParameter)command.Parameters[6]).Character;
        _textSpeed = ((DialoguePropertyScriptParameter)command.Parameters[7]).Character;
        _textEntranceEffect = new(((TextEntranceEffectScriptParameter)command.Parameters[8]).EntranceEffect);
        _spriteLayer = ((ShortScriptParameter)command.Parameters[9]).Value;
        _dontClearText = ((BoolScriptParameter)command.Parameters[10]).Value;
        _disableLipFlap = ((BoolScriptParameter)command.Parameters[11]).Value;

        _dialogueUpdateTimer = new(TimeSpan.FromMilliseconds(250));
        _dialogueUpdateTimer.Elapsed += (sender, args) =>
        {
            ScriptEditor.UpdatePreview();
            _dialogueUpdateTimer.Stop();
        };
    }

    public ObservableCollection<CharacterItem> Characters { get; }
    private CharacterItem _speaker;
    public CharacterItem Speaker
    {
        get => _speaker;
        set
        {
            if (value is null)
                return;

            if (_speaker == TextVoiceFont)
            {
                TextVoiceFont = value;
            }
            if (_speaker == TextSpeed)
            {
                TextSpeed = value;
            }
            this.RaiseAndSetIfChanged(ref _speaker, value);
            ((DialogueScriptParameter)Command.Parameters[0]).Line.Speaker = _speaker.MessageInfo.Character;
            Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Speaker = _speaker.MessageInfo.Character;
            _specialPredicate = i => i.Name != "NONE" && ((CharacterSpriteItem)i).Sprite.Character == _speaker.MessageInfo.Character;
            Script.UnsavedChanges = true;
            ScriptEditor.UpdatePreview();
        }
    }

    private string _dialogueLine;
    public string DialogueLine
    {
        get => _dialogueLine.GetSubstitutedString(_window.OpenProject);
        set
        {
            if (value is null)
                return;
            string text = value;
            text = StartStringQuotes().Replace(text, "“");
            text = MidStringOpenQuotes().Replace(text, "$1“");
            text = text.Replace('"', '”');

            this.RaiseAndSetIfChanged(ref _dialogueLine, text.GetOriginalString(_window.OpenProject));

            if (string.IsNullOrEmpty(_dialogueLine))
            {
                ((DialogueScriptParameter)Command.Parameters[0]).Line.Text = "";
                Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Text = "";
                ((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer = 0;
                Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Pointer = 0;
            }
            else
            {
                if (((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer == 0)
                {
                    // It doesn't matter what we set this to as long as it's greater than zero
                    // The ASM creation routine only checks that the pointer is not zero
                    ((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer = 1;
                    Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Pointer = 1;
                }
                ((DialogueScriptParameter)Command.Parameters[0]).Line.Text = _dialogueLine;
                Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Text = _dialogueLine;
            }

            _dialogueUpdateTimer.Stop();
            _dialogueUpdateTimer.Start();
            Script.UnsavedChanges = true;
            Command.UpdateDisplay();
        }
    }

    private CharacterSpriteItem _characterSprite;
    public CharacterSpriteItem CharacterSprite
    {
        get => _characterSprite;
        set
        {
            this.RaiseAndSetIfChanged(ref _characterSprite, value);
            if (_characterSprite is null)
            {
                ((SpriteScriptParameter)Command.Parameters[1]).Sprite = null;
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[1] = 0;
            }
            else
            {
                ((SpriteScriptParameter)Command.Parameters[1]).Sprite = _characterSprite;
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[1] = (short)(_characterSprite.Index);
            }
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }
    public ICommand SelectCharacterSpriteCommand { get; }

    private async Task SelectCharacterSpriteCommand_Executed()
    {
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Character_Sprite).Cast<CharacterSpriteItem>(),
            CharacterSprite, _window.OpenProject, _window.Log, _specialPredicate);
        CharacterSpriteItem sprite = await new GraphicSelectionDialog() { DataContext = graphicSelectionDialog }.ShowDialog<CharacterSpriteItem>(_window.Window);
        if (sprite is not null)
        {
            CharacterSprite = sprite;
        }
    }

    public ObservableCollection<SpriteEntranceTransitionLocalized> SpriteEntranceTransitions { get; } =
        new(Enum.GetValues<SpriteEntranceScriptParameter.SpriteEntranceTransition>().Select(e => new SpriteEntranceTransitionLocalized(e)));
    private SpriteEntranceTransitionLocalized _spriteEntranceTransition;
    public SpriteEntranceTransitionLocalized SpriteEntranceTransition
    {
        get => _spriteEntranceTransition;
        set
        {
            this.RaiseAndSetIfChanged(ref _spriteEntranceTransition, value);
            ((SpriteEntranceScriptParameter)Command.Parameters[2]).EntranceTransition = _spriteEntranceTransition.Entrance;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = (short)_spriteEntranceTransition.Entrance;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<SpriteExitTransitionLocalized> SpriteExitTransitions { get; } =
        new(Enum.GetValues<SpriteExitScriptParameter.SpriteExitTransition>().Select(e => new SpriteExitTransitionLocalized(e)));
    private SpriteExitTransitionLocalized _spriteExitTransition;
    public SpriteExitTransitionLocalized SpriteExitTransition
    {
        get => _spriteExitTransition;
        set
        {
            this.RaiseAndSetIfChanged(ref _spriteExitTransition, value);
            ((SpriteExitScriptParameter)Command.Parameters[3]).ExitTransition = _spriteExitTransition.Exit;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[3] = (short)_spriteExitTransition.Exit;
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<SpriteShakeLocalized> SpriteShakeEffects { get; } =
        new(Enum.GetValues<SpriteShakeScriptParameter.SpriteShakeEffect>().Select(s => new SpriteShakeLocalized(s)));
    private SpriteShakeLocalized _spriteShakeEffect;
    public SpriteShakeLocalized SpriteShakeEffect
    {
        get => _spriteShakeEffect;
        set
        {
            this.RaiseAndSetIfChanged(ref _spriteShakeEffect, value);
            ((SpriteShakeScriptParameter)Command.Parameters[4]).ShakeEffect = _spriteShakeEffect.Shake;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[4] = (short)_spriteShakeEffect.Shake;
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<VoicedLineItem> VoicedLines { get; }
    private VoicedLineItem _voicedLine;
    public VoicedLineItem VoicedLine
    {
        get => _voicedLine;
        set
        {
            this.RaiseAndSetIfChanged(ref _voicedLine, value);
            ((VoicedLineScriptParameter)Command.Parameters[5]).VoiceLine = _voicedLine;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[5] = (short)_voicedLine.Index;
            Script.UnsavedChanges = true;
        }
    }

    private CharacterItem _textVoiceFont;
    public CharacterItem TextVoiceFont
    {
        get => _textVoiceFont;
        set
        {
            this.RaiseAndSetIfChanged(ref _textVoiceFont, value);
            ((DialoguePropertyScriptParameter)Command.Parameters[6]).Character = _textVoiceFont;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[6] = (short)_textVoiceFont.MessageInfo.Character;
            Script.UnsavedChanges = true;
        }
    }

    private CharacterItem _textSpeed;
    public CharacterItem TextSpeed
    {
        get => _textSpeed;
        set
        {
            this.RaiseAndSetIfChanged(ref _textSpeed, value);
            ((DialoguePropertyScriptParameter)Command.Parameters[7]).Character = _textSpeed;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[7] = (short)_textSpeed.MessageInfo.Character;
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<TextEntranceEffectLocalized> TextEntranceEffects { get; } =
        new(Enum.GetValues<TextEntranceEffectScriptParameter.TextEntranceEffect>().Select(e => new TextEntranceEffectLocalized(e)));
    private TextEntranceEffectLocalized _textEntranceEffect;
    public TextEntranceEffectLocalized TextEntranceEffect
    {
        get => _textEntranceEffect;
        set
        {
            this.RaiseAndSetIfChanged(ref _textEntranceEffect, value);
            ((TextEntranceEffectScriptParameter)Command.Parameters[8]).EntranceEffect = _textEntranceEffect.Effect;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[8] = (short)_textEntranceEffect.Effect;
            Script.UnsavedChanges = true;
        }
    }

    private short _spriteLayer;
    public short SpriteLayer
    {
        get => _spriteLayer;
        set
        {
            this.RaiseAndSetIfChanged(ref _spriteLayer, value);
            ((ShortScriptParameter)Command.Parameters[9]).Value = _spriteLayer;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[9] = _spriteLayer;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    private bool _dontClearText;
    public bool DontClearText
    {
        get => _dontClearText;
        set
        {
            this.RaiseAndSetIfChanged(ref _dontClearText, value);
            ((BoolScriptParameter)Command.Parameters[10]).Value = _dontClearText;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[10] = _dontClearText ? ((BoolScriptParameter)Command.Parameters[10]).TrueValue : ((BoolScriptParameter)Command.Parameters[10]).FalseValue;
            Script.UnsavedChanges = true;
        }
    }

    private bool _disableLipFlap;
    public bool DisableLipFlap
    {
        get => _disableLipFlap;
        set
        {
            this.RaiseAndSetIfChanged(ref _disableLipFlap, value);
            ((BoolScriptParameter)Command.Parameters[11]).Value = _disableLipFlap;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[11] = _disableLipFlap ? ((BoolScriptParameter)Command.Parameters[11]).TrueValue : ((BoolScriptParameter)Command.Parameters[11]).FalseValue;
            Script.UnsavedChanges = true;
        }
    }

    [GeneratedRegex(@"^""")]
    private static partial Regex StartStringQuotes();
    [GeneratedRegex(@"(\s)""")]
    private static partial Regex MidStringOpenQuotes();
}

public readonly struct SpriteEntranceTransitionLocalized(SpriteEntranceScriptParameter.SpriteEntranceTransition entrance)
{
    public SpriteEntranceScriptParameter.SpriteEntranceTransition Entrance { get; } = entrance;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(entrance.ToString());
}
public readonly struct SpriteExitTransitionLocalized(SpriteExitScriptParameter.SpriteExitTransition exit)
{
    public SpriteExitScriptParameter.SpriteExitTransition Exit { get; } = exit;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(exit.ToString());
}
public readonly struct SpriteShakeLocalized(SpriteShakeScriptParameter.SpriteShakeEffect shake)
{
    public SpriteShakeScriptParameter.SpriteShakeEffect Shake { get; } = shake;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(shake.ToString());
}
public readonly struct TextEntranceEffectLocalized(TextEntranceEffectScriptParameter.TextEntranceEffect effect)
{
    public TextEntranceEffectScriptParameter.TextEntranceEffect Effect { get; } = effect;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(effect.ToString());
}

