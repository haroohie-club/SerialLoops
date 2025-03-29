using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Event;
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
    private readonly MainWindowViewModel _window;
    public EditorTabsPanelViewModel Tabs { get; set; }
    private Func<ItemDescription, bool> _specialPredicate;
    private List<ScriptItemCommand> _scriptCommands;

    private static SpritePostTransitionScriptParameter.SpritePostTransition[] s_spriteExits =
    [
        SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_FROM_CENTER_TO_RIGHT_FADE_OUT,
        SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_FROM_CENTER_TO_LEFT_FADE_OUT,
        SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_RIGHT_FADE_OUT,
        SpritePostTransitionScriptParameter.SpritePostTransition.SLIDE_LEFT_FADE_OUT,
        SpritePostTransitionScriptParameter.SpritePostTransition.FADE_OUT_CENTER,
        SpritePostTransitionScriptParameter.SpritePostTransition.FADE_OUT_LEFT,
    ];

    public DialogueScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) : base(command, scriptEditor, log)
    {
        _window = window;
        Tabs = _window.EditorTabs;
        Characters = new(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Character).Cast<CharacterItem>());
        _speaker = _window.OpenProject.GetCharacterBySpeaker(((DialogueScriptParameter)Command.Parameters[0]).Line.Speaker);
        _specialPredicate = i => i.Name == "NONE" || ((CharacterSpriteItem)i).Sprite.Character == _speaker.MessageInfo.Character;
        _dialogueLine = ((DialogueScriptParameter)command.Parameters[0]).Line.Text;
        _characterSprite = ((SpriteScriptParameter)command.Parameters[1]).Sprite;
        SelectCharacterSpriteCommand = ReactiveCommand.CreateFromTask(SelectCharacterSpriteCommand_Executed);
        _spriteEntranceTransition = new(((SpritePreTransitionScriptParameter)command.Parameters[2]).PreTransition);
        _spriteExitTransition = new(((SpritePostTransitionScriptParameter)command.Parameters[3]).PostTransition);
        _spriteShakeEffect = new(((SpriteShakeScriptParameter)command.Parameters[4]).ShakeEffect);
        VoicedLines = new(new List<VoicedLineItem> { null }.Concat(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Voice).Cast<VoicedLineItem>()));
        _voicedLine = ((VoicedLineScriptParameter)command.Parameters[5]).VoiceLine;
        _textVoiceFont = ((DialoguePropertyScriptParameter)command.Parameters[6]).Character;
        _textSpeed = ((DialoguePropertyScriptParameter)command.Parameters[7]).Character;
        _textEntranceEffect = new(((TextEntranceEffectScriptParameter)command.Parameters[8]).EntranceEffect);
        _spriteLayer = ((ShortScriptParameter)command.Parameters[9]).Value;
        _dontClearText = ((BoolScriptParameter)command.Parameters[10]).Value;
        _disableLipFlap = ((BoolScriptParameter)command.Parameters[11]).Value;
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

            if (_speaker.MessageInfo.Character == TextVoiceFont.MessageInfo.Character)
            {
                TextVoiceFont = value;
            }
            if (_speaker.MessageInfo.Character == TextSpeed.MessageInfo.Character)
            {
                TextSpeed = value;
            }
            this.RaiseAndSetIfChanged(ref _speaker, value);
            ((DialogueScriptParameter)Command.Parameters[0]).Line.Speaker = _speaker.MessageInfo.Character;
            Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Speaker = _speaker.MessageInfo.Character;
            _specialPredicate = i => i.Name != "NONE" && ((CharacterSpriteItem)i).Sprite.Character == _speaker.MessageInfo.Character;
            if (_characterSprite is null)
            {
                _scriptCommands ??= Command.WalkCommandGraph(ScriptEditor.Commands, Script.Graph);
                ScriptItemCommand lastDialogueCommand = _scriptCommands[..^1].LastOrDefault(c => c.Verb == EventFile.CommandVerb.DIALOGUE
                    && ((DialogueScriptParameter)c.Parameters[0]).Line?.Speaker == _speaker.MessageInfo.Character);
                if (((SpriteScriptParameter)lastDialogueCommand?.Parameters[1])?.Sprite is not null &&
                    !s_spriteExits.Contains(((SpritePostTransitionScriptParameter)lastDialogueCommand.Parameters[3]).PostTransition))
                {
                    CharacterSprite = ((SpriteScriptParameter)lastDialogueCommand.Parameters[1]).Sprite;
                    return; // return here to avoid updating the script preview twice
                }
            }
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

            ScriptEditor.UpdatePreview();
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
            ((SpriteScriptParameter)Command.Parameters[1]).Sprite = _characterSprite;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short?)_characterSprite?.Index ?? 0;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }
    public ICommand SelectCharacterSpriteCommand { get; }

    private async Task SelectCharacterSpriteCommand_Executed()
    {
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(new List<IPreviewableGraphic> { NonePreviewableGraphic.CHARACTER_SPRITE }.Concat(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Character_Sprite).Cast<IPreviewableGraphic>()),
            CharacterSprite, _window.OpenProject, _window.Log, _specialPredicate);
        IPreviewableGraphic sprite = await new GraphicSelectionDialog { DataContext = graphicSelectionDialog }.ShowDialog<IPreviewableGraphic>(_window.Window);
        if (sprite?.Text == "NONE")
        {
            CharacterSprite = null;
        }
        else if (sprite is not null)
        {
            CharacterSprite = (CharacterSpriteItem)sprite;
        }
    }

    public ObservableCollection<SpriteEntranceTransitionLocalized> SpriteEntranceTransitions { get; } =
        new(Enum.GetValues<SpritePreTransitionScriptParameter.SpritePreTransition>().Select(e => new SpriteEntranceTransitionLocalized(e)));
    private SpriteEntranceTransitionLocalized _spriteEntranceTransition;
    public SpriteEntranceTransitionLocalized SpriteEntranceTransition
    {
        get => _spriteEntranceTransition;
        set
        {
            this.RaiseAndSetIfChanged(ref _spriteEntranceTransition, value);
            ((SpritePreTransitionScriptParameter)Command.Parameters[2]).PreTransition = _spriteEntranceTransition.PreTransition;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = (short)_spriteEntranceTransition.PreTransition;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<SpriteExitTransitionLocalized> SpriteExitTransitions { get; } =
        new(Enum.GetValues<SpritePostTransitionScriptParameter.SpritePostTransition>().Select(e => new SpriteExitTransitionLocalized(e)));
    private SpriteExitTransitionLocalized _spriteExitTransition;
    public SpriteExitTransitionLocalized SpriteExitTransition
    {
        get => _spriteExitTransition;
        set
        {
            this.RaiseAndSetIfChanged(ref _spriteExitTransition, value);
            ((SpritePostTransitionScriptParameter)Command.Parameters[3]).PostTransition = _spriteExitTransition.Post;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[3] = (short)_spriteExitTransition.Post;
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
                .Objects[Command.Index].Parameters[5] = (short)(_voicedLine?.Index ?? 0);
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
                    .Objects[Command.Index].Parameters[6] = (short)ScriptEditor.Window.OpenProject.MessInfo.MessageInfos.IndexOf(_textVoiceFont.MessageInfo);
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
                .Objects[Command.Index].Parameters[7] = (short)ScriptEditor.Window.OpenProject.MessInfo.MessageInfos.IndexOf(_textSpeed.MessageInfo);
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

public readonly struct SpriteEntranceTransitionLocalized(SpritePreTransitionScriptParameter.SpritePreTransition preTransition)
{
    public SpritePreTransitionScriptParameter.SpritePreTransition PreTransition { get; } = preTransition;
    public override string ToString() => Strings.ResourceManager.GetString(PreTransition.ToString());
}
public readonly struct SpriteExitTransitionLocalized(SpritePostTransitionScriptParameter.SpritePostTransition post)
{
    public SpritePostTransitionScriptParameter.SpritePostTransition Post { get; } = post;
    public override string ToString() => Strings.ResourceManager.GetString(Post.ToString());
}
public readonly struct SpriteShakeLocalized(SpriteShakeScriptParameter.SpriteShakeEffect shake)
{
    public SpriteShakeScriptParameter.SpriteShakeEffect Shake { get; } = shake;
    public override string ToString() => Strings.ResourceManager.GetString(Shake.ToString());
}
public readonly struct TextEntranceEffectLocalized(TextEntranceEffectScriptParameter.TextEntranceEffect effect)
{
    public TextEntranceEffectScriptParameter.TextEntranceEffect Effect { get; } = effect;
    public override string ToString() => Strings.ResourceManager.GetString(Effect.ToString());
}

