using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;
using static SerialLoops.Lib.Items.ItemDescription;

namespace SerialLoops.Lib.Script
{
    public static class ScriptTemplates
    {
        public class TemplateOption(string name, string description, Func<Project, EventFile, ScriptTemplate> template)
        {
            public string Name { get; set; } = name;
            public string Description { get; set; } = description;
            public Func<Project, EventFile, ScriptTemplate> Template { get; set; } = template;
        }

        public static readonly ObservableCollection<TemplateOption> AvailableTemplates =
        [
            new("Standard Story Script Intro", "A standard intro for a story script (i.e. EVX_XXX)", (Project p, EventFile evt) => StandardStoryScriptIntro(p)),
            new("Standard Chess Script Skeleton", "An outline for a standard chess file script", StandardChessFile)
        ];

        private static ScriptTemplate StandardStoryScriptIntro(Project project) => new(
            new TemplateSection("SCRIPT00", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.KBG_DISP, new BgScriptParameter(
                    project.Localize("Kinetic Background"),
                    (BackgroundItem)project.Items.First(b => b.Type == ItemType.Background && b.Name == "BG_KBG01"),
                    true)),
                new ScriptItemCommand(CommandVerb.BG_DISP, new BgScriptParameter(
                    project.Localize("Background"),
                    (BackgroundItem)project.Items.First(b => b.Type == ItemType.Background && ((BackgroundItem)b).BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_BG),
                    true
                    )),
                new ScriptItemCommand(CommandVerb.BGM_PLAY,
                    new BgmScriptParameter(project.Localize("BGM"), (BackgroundMusicItem)project.Items.First(b => b.Type == ItemType.BGM)),
                    new BgmModeScriptParameter(project.Localize("BGM Mode"), (short)BgmModeScriptParameter.BgmMode.START),
                    new ShortScriptParameter(project.Localize("Volume"), 100),
                    new ShortScriptParameter(project.Localize("Fade In Time"), 30),
                    new ShortScriptParameter(project.Localize("Fade Out Time"), 0)),
                new ScriptItemCommand(CommandVerb.SET_PLACE,
                    new BoolScriptParameter(project.Localize("Display"), true),
                    new PlaceScriptParameter(project.Localize("Place"), (PlaceItem)project.Items.First(b => b.Type == ItemType.Place))),
                new ScriptItemCommand(CommandVerb.SCREEN_FADEIN,
                    new ShortScriptParameter(project.Localize("Fade In Time"), 30),
                    new ShortScriptParameter(project.Localize("Fade In Percentage"), 0),
                    new ScreenScriptParameter(project.Localize("Screen"), (short)ScreenScriptParameter.DsScreen.BOTH),
                    new ColorMonochromeScriptParameter(project.Localize("Color"), (short)ColorMonochromeScriptParameter.ColorMonochrome.BLACK)),
                new ScriptItemCommand(CommandVerb.BACK)
            ));

        private static ScriptTemplate StandardChessFile(Project project, EventFile script)
        {
            DialogueLine openingLine = new("Let's play!".GetOriginalString(project), script) { Speaker = Speaker.UNKNOWN };
            DialogueLine pinnedLine = new("Do the chess".GetOriginalString(project), script) { Speaker = Speaker.INFO };
            DialogueLine clearLine = new("You won the chess!".GetOriginalString(project), script) { Speaker = Speaker.UNKNOWN };
            DialogueLine missLine = new("You lost the chess".GetOriginalString(project), script) { Speaker = Speaker.UNKNOWN };
            DialogueLine miss2Line = new("You lost the chess 2".GetOriginalString(project), script) { Speaker = Speaker.UNKNOWN };

            ScriptSection clearSection, missSection;

            if (script.ScriptSections.Any(s => s.Name == "NONEClear"))
            {
                clearSection = script.ScriptSections.Find(s => s.Name == "NONEClear");
            }
            else
            {
                clearSection = new() { Name = "NONEClear", CommandsAvailable = CommandsAvailable };
                script.ScriptSections.Add(clearSection);
                script.LabelsSection.Objects.Add(new() { Id = (short)(script.LabelsSection.Objects.Count > 0 ? script.LabelsSection.Objects.Max(l => l.Id) + 1 : 1001), Name = "NONE/Clear" });
            }
            if (script.ScriptSections.Any(s => s.Name == "NONEMiss"))
            {
                missSection = script.ScriptSections.Find(s => s.Name == "NONEMiss");
            }
            else
            {
                missSection = new() { Name = "NONEMiss", CommandsAvailable = CommandsAvailable };
                script.ScriptSections.Add(missSection);
                script.LabelsSection.Objects.Add(new() { Id = (short)(script.LabelsSection.Objects.Max(l => l.Id) + 1), Name = "NONE/Miss" });
            }

            script.DialogueSection.Objects.AddRange(new DialogueLine[] { openingLine, pinnedLine, clearLine, missLine, miss2Line });

            return new(
            new TemplateSection("SCRIPT00", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.BGM_PLAY,
                    new BgmScriptParameter(project.Localize("BGM"), (BackgroundMusicItem)project.Items.First(b => b.Name == "BGM003")),
                    new BgmModeScriptParameter(project.Localize("BGM Mode"), (short)BgmModeScriptParameter.BgmMode.START),
                    new ShortScriptParameter(project.Localize("Volume"), 100),
                    new ShortScriptParameter(project.Localize("Fade In Time"), 30),
                    new ShortScriptParameter(project.Localize("Fade Out Time"), 0)),
                new ScriptItemCommand(CommandVerb.BG_DISP, new BgScriptParameter(
                    "Background",
                    (BackgroundItem)project.Items.First(b => b.Type == ItemType.Background && b.Name == "BG_BG_BUND0"),
                    true
                    )),
                new ScriptItemCommand(CommandVerb.SCREEN_FADEIN,
                    new ShortScriptParameter(project.Localize("Fade In Time"), 30),
                    new ShortScriptParameter(project.Localize("Fade In Percentage"), 0),
                    new ScreenScriptParameter(project.Localize("Screen"), (short)ScreenScriptParameter.DsScreen.BOTH),
                    new ColorMonochromeScriptParameter(project.Localize("Color"), (short)ColorMonochromeScriptParameter.ColorMonochrome.WHITE)),
                new ScriptItemCommand(CommandVerb.DIALOGUE,
                    new DialogueScriptParameter(project.Localize("Dialogue"), openingLine),
                    new SpriteScriptParameter(project.Localize("Sprite"), null),
                    new SpriteEntranceScriptParameter(project.Localize("Entrance"), (short)SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER),
                    new SpriteExitScriptParameter(project.Localize("Exit"), (short)SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT),
                    new SpriteShakeScriptParameter(project.Localize("Shake"), (short)SpriteShakeScriptParameter.SpriteShakeEffect.NONE),
                    new VoicedLineScriptParameter(project.Localize("Voice"), null),
                    new DialoguePropertyScriptParameter(project.Localize("Text Voice Font"), (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new DialoguePropertyScriptParameter(project.Localize("Text Speed"), (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new TextEntranceEffectScriptParameter(project.Localize("Text Speed"), (short)TextEntranceEffectScriptParameter.TextEntranceEffect.NORMAL),
                    new ShortScriptParameter(project.Localize("Sprite Layer"), 0),
                    new BoolScriptParameter(project.Localize("Don't Clear Text"), false),
                    new BoolScriptParameter(project.Localize("Disable Lip Flap"), false)),
                new ScriptItemCommand(CommandVerb.CHESS_LOAD,
                    new ChessFileScriptParameter(project.Localize("Chess File"), 0)),
                new ScriptItemCommand(CommandVerb.PIN_MNL,
                    new DialogueScriptParameter(project.Localize("Dialogue"), pinnedLine)),
                new ScriptItemCommand(CommandVerb.CHESS_VGOTO,
                    new ScriptSectionScriptParameter(project.Localize("Clear Block"), clearSection),
                    new ScriptSectionScriptParameter(project.Localize("Miss Block"), missSection),
                    new ScriptSectionScriptParameter(project.Localize("Miss2 Block"), null))
                ),
            new TemplateSection("NONEClear", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.DIALOGUE,
                    new DialogueScriptParameter(project.Localize("Dialogue"), clearLine),
                    new SpriteScriptParameter(project.Localize("Sprite"), null),
                    new SpriteEntranceScriptParameter(project.Localize("Entrance"), (short)SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER),
                    new SpriteExitScriptParameter(project.Localize("Exit"), (short)SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT),
                    new SpriteShakeScriptParameter(project.Localize("Shake"), (short)SpriteShakeScriptParameter.SpriteShakeEffect.NONE),
                    new VoicedLineScriptParameter(project.Localize("Voice"), null),
                    new DialoguePropertyScriptParameter(project.Localize("Text Voice Font"), (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new DialoguePropertyScriptParameter(project.Localize("Text Speed"), (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new TextEntranceEffectScriptParameter(project.Localize("Text Speed"), (short)TextEntranceEffectScriptParameter.TextEntranceEffect.NORMAL),
                    new ShortScriptParameter(project.Localize("Sprite Layer"), 0),
                    new BoolScriptParameter(project.Localize("Don't Clear Text"), false),
                    new BoolScriptParameter(project.Localize("Disable Lip Flap"), false)),
                new ScriptItemCommand(CommandVerb.TOGGLE_DIALOGUE,
                    new BoolScriptParameter(project.Localize("Display"), false)),
                new ScriptItemCommand(CommandVerb.BACK)
                ),
            new TemplateSection("NONEMiss", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.DIALOGUE,
                    new DialogueScriptParameter(project.Localize("Dialogue"), missLine),
                    new SpriteScriptParameter(project.Localize("Sprite"), null),
                    new SpriteEntranceScriptParameter(project.Localize("Entrance"), (short)SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER),
                    new SpriteExitScriptParameter(project.Localize("Exit"), (short)SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT),
                    new SpriteShakeScriptParameter(project.Localize("Shake"), (short)SpriteShakeScriptParameter.SpriteShakeEffect.NONE),
                    new VoicedLineScriptParameter(project.Localize("Voice"), null),
                    new DialoguePropertyScriptParameter(project.Localize("Text Voice Font"), (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new DialoguePropertyScriptParameter(project.Localize("Text Speed"), (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new TextEntranceEffectScriptParameter(project.Localize("Text Speed"), (short)TextEntranceEffectScriptParameter.TextEntranceEffect.NORMAL),
                    new ShortScriptParameter(project.Localize("Sprite Layer"), 0),
                    new BoolScriptParameter(project.Localize("Don't Clear Text"), false),
                    new BoolScriptParameter(project.Localize("Disable Lip Flap"), false)),
                new ScriptItemCommand(CommandVerb.TOGGLE_DIALOGUE,
                    new BoolScriptParameter(project.Localize("Display"), false)),
                new ScriptItemCommand(CommandVerb.BACK)
                ),
            new TemplateSection("NONEMiss2", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.DIALOGUE,
                    new DialogueScriptParameter(project.Localize("Dialogue"), miss2Line),
                    new SpriteScriptParameter(project.Localize("Sprite"), null),
                    new SpriteEntranceScriptParameter(project.Localize("Entrance"), (short)SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER),
                    new SpriteExitScriptParameter(project.Localize("Exit"), (short)SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT),
                    new SpriteShakeScriptParameter(project.Localize("Shake"), (short)SpriteShakeScriptParameter.SpriteShakeEffect.NONE),
                    new VoicedLineScriptParameter(project.Localize("Voice"), null),
                    new DialoguePropertyScriptParameter(project.Localize("Text Voice Font"), (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new DialoguePropertyScriptParameter(project.Localize("Text Speed"), (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new TextEntranceEffectScriptParameter(project.Localize("Text Speed"), (short)TextEntranceEffectScriptParameter.TextEntranceEffect.NORMAL),
                    new ShortScriptParameter(project.Localize("Sprite Layer"), 0),
                    new BoolScriptParameter(project.Localize("Don't Clear Text"), false),
                    new BoolScriptParameter(project.Localize("Disable Lip Flap"), false)),
                new ScriptItemCommand(CommandVerb.TOGGLE_DIALOGUE,
                    new BoolScriptParameter(project.Localize("Display"), false)),
                new ScriptItemCommand(CommandVerb.BACK)
                )
            );
        }
    }
}
