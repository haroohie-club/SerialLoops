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
        public class TemplateOption
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public Func<Project, EventFile, ScriptTemplate> Template { get; set; }

            public TemplateOption(string name, string description, Func<Project, EventFile, ScriptTemplate> template)
            {
                Name = name;
                Description = description;
                Template = template;
            }
        }

        public static readonly ObservableCollection<TemplateOption> AvailableTemplates = new()
        {
            new("Standard Story Script Intro", "A standard intro for a story script (i.e. EVX_XXX)", (Project p, EventFile evt) => StandardStoryScriptIntro(p)),
            new("Standard Chess Script Skeleton", "An outline for a standard chess file script", (Project p, EventFile evt) => StandardChessFile(p, evt))
        };

        private static ScriptTemplate StandardStoryScriptIntro(Project project) => new(
            new TemplateSection("SCRIPT00", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.KBG_DISP, new BgScriptParameter(
                    "Kinetic Background",
                    (BackgroundItem)project.Items.First(b => b.Type == ItemType.Background && b.Name == "BG_KBG01"),
                    true)),
                new ScriptItemCommand(CommandVerb.BG_DISP, new BgScriptParameter(
                    "Background",
                    (BackgroundItem)project.Items.First(b => b.Type == ItemType.Background && ((BackgroundItem)b).BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.TEX_BG),
                    true
                    )),
                new ScriptItemCommand(CommandVerb.BGM_PLAY,
                    new BgmScriptParameter("BGM", (BackgroundMusicItem)project.Items.First(b => b.Type == ItemType.BGM)),
                    new BgmModeScriptParameter("BGM Mode", (short)BgmModeScriptParameter.BgmMode.START),
                    new ShortScriptParameter("Volume", 100),
                    new ShortScriptParameter("Fade In Time", 30),
                    new ShortScriptParameter("Fade Out Time", 0)),
                new ScriptItemCommand(CommandVerb.SET_PLACE,
                    new BoolScriptParameter("Display", true),
                    new PlaceScriptParameter("Place", (PlaceItem)project.Items.First(b => b.Type == ItemType.Place))),
                new ScriptItemCommand(CommandVerb.SCREEN_FADEIN,
                    new ShortScriptParameter("Fade In Time", 30),
                    new ShortScriptParameter("Fade In Percentage", 0),
                    new ScreenScriptParameter("Screen", (short)ScreenScriptParameter.DsScreen.BOTH),
                    new ColorMonochromeScriptParameter("Color", (short)ColorMonochromeScriptParameter.ColorMonochrome.BLACK)),
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
                    new BgmScriptParameter("BGM", (BackgroundMusicItem)project.Items.First(b => b.Name == "BGM003")),
                    new BgmModeScriptParameter("BGM Mode", (short)BgmModeScriptParameter.BgmMode.START),
                    new ShortScriptParameter("Volume", 100),
                    new ShortScriptParameter("Fade In Time", 30),
                    new ShortScriptParameter("Fade Out Time", 0)),
                new ScriptItemCommand(CommandVerb.BG_DISP, new BgScriptParameter(
                    "Background",
                    (BackgroundItem)project.Items.First(b => b.Type == ItemType.Background && b.Name == "BG_BG_BUND0"),
                    true
                    )),
                new ScriptItemCommand(CommandVerb.SCREEN_FADEIN,
                    new ShortScriptParameter("Fade In Time", 30),
                    new ShortScriptParameter("Fade In Percentage", 0),
                    new ScreenScriptParameter("Screen", (short)ScreenScriptParameter.DsScreen.BOTH),
                    new ColorMonochromeScriptParameter("Color", (short)ColorMonochromeScriptParameter.ColorMonochrome.WHITE)),
                new ScriptItemCommand(CommandVerb.DIALOGUE,
                    new DialogueScriptParameter("Dialogue", openingLine),
                    new SpriteScriptParameter("Sprite", null),
                    new SpriteEntranceScriptParameter("Entrance", (short)SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER),
                    new SpriteExitScriptParameter("Exit", (short)SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT),
                    new SpriteShakeScriptParameter("Shake", (short)SpriteShakeScriptParameter.SpriteShakeEffect.NONE),
                    new VoicedLineScriptParameter("Voice", null),
                    new DialoguePropertyScriptParameter("Text Voice Font", (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new DialoguePropertyScriptParameter("Text Speed", (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new TextEntranceEffectScriptParameter("Text Speed", (short)TextEntranceEffectScriptParameter.TextEntranceEffect.NORMAL),
                    new ShortScriptParameter("Sprite Layer", 0),
                    new BoolScriptParameter("Don't Clear Text", false),
                    new BoolScriptParameter("Disable Lip Flap", false)),
                new ScriptItemCommand(CommandVerb.CHESS_LOAD,
                    new ChessFileScriptParameter("Chess File", 0)),
                new ScriptItemCommand(CommandVerb.PIN_MNL,
                    new DialogueScriptParameter("Dialogue", pinnedLine)),
                new ScriptItemCommand(CommandVerb.CHESS_VGOTO,
                    new ScriptSectionScriptParameter("Clear Block", clearSection),
                    new ScriptSectionScriptParameter("Miss Block", missSection),
                    new ScriptSectionScriptParameter("Miss2 Block", null))
                ),
            new TemplateSection("NONEClear", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.DIALOGUE,
                    new DialogueScriptParameter("Dialogue", clearLine),
                    new SpriteScriptParameter("Sprite", null),
                    new SpriteEntranceScriptParameter("Entrance", (short)SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER),
                    new SpriteExitScriptParameter("Exit", (short)SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT),
                    new SpriteShakeScriptParameter("Shake", (short)SpriteShakeScriptParameter.SpriteShakeEffect.NONE),
                    new VoicedLineScriptParameter("Voice", null),
                    new DialoguePropertyScriptParameter("Text Voice Font", (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new DialoguePropertyScriptParameter("Text Speed", (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new TextEntranceEffectScriptParameter("Text Speed", (short)TextEntranceEffectScriptParameter.TextEntranceEffect.NORMAL),
                    new ShortScriptParameter("Sprite Layer", 0),
                    new BoolScriptParameter("Don't Clear Text", false),
                    new BoolScriptParameter("Disable Lip Flap", false)),
                new ScriptItemCommand(CommandVerb.TOGGLE_DIALOGUE,
                    new BoolScriptParameter("Display", false)),
                new ScriptItemCommand(CommandVerb.BACK)
                ),
            new TemplateSection("NONEMiss", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.DIALOGUE,
                    new DialogueScriptParameter("Dialogue", missLine),
                    new SpriteScriptParameter("Sprite", null),
                    new SpriteEntranceScriptParameter("Entrance", (short)SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER),
                    new SpriteExitScriptParameter("Exit", (short)SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT),
                    new SpriteShakeScriptParameter("Shake", (short)SpriteShakeScriptParameter.SpriteShakeEffect.NONE),
                    new VoicedLineScriptParameter("Voice", null),
                    new DialoguePropertyScriptParameter("Text Voice Font", (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new DialoguePropertyScriptParameter("Text Speed", (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new TextEntranceEffectScriptParameter("Text Speed", (short)TextEntranceEffectScriptParameter.TextEntranceEffect.NORMAL),
                    new ShortScriptParameter("Sprite Layer", 0),
                    new BoolScriptParameter("Don't Clear Text", false),
                    new BoolScriptParameter("Disable Lip Flap", false)),
                new ScriptItemCommand(CommandVerb.TOGGLE_DIALOGUE,
                    new BoolScriptParameter("Display", false)),
                new ScriptItemCommand(CommandVerb.BACK)
                ),
            new TemplateSection("NONEMiss2", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.DIALOGUE,
                    new DialogueScriptParameter("Dialogue", miss2Line),
                    new SpriteScriptParameter("Sprite", null),
                    new SpriteEntranceScriptParameter("Entrance", (short)SpriteEntranceScriptParameter.SpriteEntranceTransition.FADE_TO_CENTER),
                    new SpriteExitScriptParameter("Exit", (short)SpriteExitScriptParameter.SpriteExitTransition.NO_EXIT),
                    new SpriteShakeScriptParameter("Shake", (short)SpriteShakeScriptParameter.SpriteShakeEffect.NONE),
                    new VoicedLineScriptParameter("Voice", null),
                    new DialoguePropertyScriptParameter("Text Voice Font", (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new DialoguePropertyScriptParameter("Text Speed", (CharacterItem)project.Items.First(c => c.Type == ItemType.Character && ((CharacterItem)c).MessageInfo.Character == Speaker.UNKNOWN)),
                    new TextEntranceEffectScriptParameter("Text Speed", (short)TextEntranceEffectScriptParameter.TextEntranceEffect.NORMAL),
                    new ShortScriptParameter("Sprite Layer", 0),
                    new BoolScriptParameter("Don't Clear Text", false),
                    new BoolScriptParameter("Disable Lip Flap", false)),
                new ScriptItemCommand(CommandVerb.TOGGLE_DIALOGUE,
                    new BoolScriptParameter("Display", false)),
                new ScriptItemCommand(CommandVerb.BACK)
                )
            );
        }
    }
}
