using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
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
            public Func<Project, ScriptTemplate> Template { get; set; }

            public TemplateOption(string name, string description, Func<Project, ScriptTemplate> template)
            {
                Name = name;
                Description = description;
                Template = template;
            }
        }

        public static readonly ObservableCollection<TemplateOption> AvailableTemplates = new()
        {
            new("Standard Story Script Intro", "A standard intro for a mainline script (i.e. EVX_XXX)", (Project p) => StandardStoryScriptIntro(p)),
        };

        private static ScriptTemplate StandardStoryScriptIntro(Project project) => new(
            new TemplateSection("SCRIPT00", CommandsPosition.TOP,
                new ScriptItemCommand(CommandVerb.KBG_DISP, new BgScriptParameter(
                    "Kinetic Background",
                    (BackgroundItem)project.Items.First(b => b.Type == ItemType.Background && ((BackgroundItem)b).BackgroundType == HaruhiChokuretsuLib.Archive.Data.BgType.KINETIC_SCREEN),
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

    }
}
