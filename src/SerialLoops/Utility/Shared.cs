using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emik;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.Utility;

public static class Shared
{
    public static string GetVersion()
    {
        string infoVersionWithCommit = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        return infoVersionWithCommit[..infoVersionWithCommit.IndexOf('+')];
    }

    public static CancellationTokenSource AudioReplacementCancellation { get; set; } = new();

    public static string[] SupportedImageFiletypes => ["*.bmp", "*.gif", "*.heif", "*.jpg", "*.jpeg", "*.png", "*.webp",];
    public static string[] SupportedAudioFiletypes => ["*.wav", "*.flac", "*.mp3", "*.ogg",];

    public static void SaveGif(this IEnumerable<SKBitmap> frames, string fileName, IProgressTracker tracker)
    {
        using Image<Rgba32> gif = new(frames.Max(f => f.Width), frames.Max(f => f.Height));
        gif.Metadata.GetGifMetadata().RepeatCount = 0;

        tracker.Focus(Strings.EditorConvertingFramesMessage, 1);
        IEnumerable<Image<Rgba32>> gifFrames = frames.Select(f => Image.LoadPixelData<Rgba32>(f.Pixels.Select(c => new Rgba32(c.Red, c.Green, c.Blue, c.Alpha)).ToArray(), f.Width, f.Height));
        tracker.Finished++;
        tracker.Focus(Strings.GifExportAddingFramesStatusMessage, gifFrames.Count());
        foreach (Image<Rgba32> gifFrame in gifFrames)
        {
            GifFrameMetadata metadata = gifFrame.Frames.RootFrame.Metadata.GetGifMetadata();
            metadata.FrameDelay = 2;
            metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
            gif.Frames.AddFrame(gifFrame.Frames.RootFrame);
            tracker.Finished++;
        }
        gif.Frames.RemoveFrame(0);

        tracker.Focus(Strings.AnimationGifSavingProgressMessage, 1);
        gif.SaveAsGif(fileName);
        tracker.Finished++;
    }

    public static SKBitmap GetCharacterVoicePortrait(Project project, ILogger log, CharacterItem character)
    {
        ItemDescription id = project.Items.Find(i => i.Name.Equals("SYSTEX_SYS_CMN_B46"));
        if (id is not SystemTextureItem tex)
        {
            log.LogError(string.Format(Strings.ErrorFailedLoadingCharacterProgressVoice, character.DisplayName));
            return null;
        }
        SKBitmap bitmap = tex.GetTexture();

        // Crop a 16x16 bitmap portrait
        SKBitmap portrait = new(16, 16);
        int portraitIndex = (int)character.MessageInfo.Character;
        int charNum = Math.Min(portraitIndex, 10) - 1;
        int x = (charNum % 4) * 32;
        int z = (charNum / 4) * 32;

        SKRectI cropRect = new(x + 8, z + 4, x + 24, z + 20);
        bitmap.ExtractSubset(portrait, cropRect);
        return portrait;
    }

    public static async Task<bool> DeleteProjectAsync(string path, MainWindowViewModel mainWindow)
    {
        if (await mainWindow.Window.ShowMessageBoxAsync(Strings.ProjectDeleteConfirmTitle,
                Strings.ProjectDeleteConfirmText,
                ButtonEnum.YesNoCancel, Icon.Warning, mainWindow.Log) == ButtonResult.Yes)
        {
            if (!await Rubbish.MoveAsync(Path.GetDirectoryName(path)))
            {
                if (await mainWindow.Window.ShowMessageBoxAsync(Strings.ProjectDeleteFailedTitle,
                        Strings.ProjectDeleteFailedText,ButtonEnum.YesNoCancel, Icon.Error, mainWindow.Log) == ButtonResult.Yes)
                {
                    try
                    {
                        Directory.Delete(Path.GetDirectoryName(path)!, true);
                    }
                    catch (Exception ex)
                    {
                        mainWindow.Log.LogException(Strings.ProjectDeleteHardFailedErrorMsg, ex);
                    }
                }
                else
                {
                    return false; // don't remove this project if we didn't delete it
                }
            }
            mainWindow.ProjectsCache.RemoveProject(path);
            mainWindow.ProjectsCache.Save(mainWindow.Log);
            mainWindow.UpdateRecentProjects();
            return true;
        }

        return false;
    }

    public static string GetScenarioVerbHelp(ScenarioCommand.ScenarioVerb verb)
    {
        return verb switch
        {
            ScenarioCommand.ScenarioVerb.NEW_GAME => Strings.ScenarioVerbHelpNEW_GAME,
            ScenarioCommand.ScenarioVerb.SAVE => Strings.ScenarioVerbHelpSAVE,
            ScenarioCommand.ScenarioVerb.LOAD_SCENE => Strings.ScenarioVerbHelpLOAD_SCENE,
            ScenarioCommand.ScenarioVerb.PUZZLE_PHASE => Strings.ScenarioVerbHelpPUZZLE_PHASE,
            ScenarioCommand.ScenarioVerb.ROUTE_SELECT => Strings.ScenarioVerbHelpROUTE_SELECT,
            ScenarioCommand.ScenarioVerb.STOP => Strings.ScenarioVerbHelpSTOP,
            ScenarioCommand.ScenarioVerb.SAVE2 => Strings.ScenarioVerbHelpSAVE2,
            ScenarioCommand.ScenarioVerb.TOPICS => Strings.ScenarioVerbHelpTOPICS,
            ScenarioCommand.ScenarioVerb.COMPANION_SELECT => Strings.ScenarioVerbHelpCOMPANION_SELECT,
            ScenarioCommand.ScenarioVerb.PLAY_VIDEO => Strings.ScenarioVerbHelpPLAY_VIDEO,
            ScenarioCommand.ScenarioVerb.NOP => Strings.ScenarioVerbHelpNOP,
            ScenarioCommand.ScenarioVerb.UNLOCK_ENDINGS => Strings.ScenarioVerbHelpUNLOCK_ENDINGS,
            ScenarioCommand.ScenarioVerb.UNLOCK => Strings.ScenarioVerbHelpUNLOCK,
            ScenarioCommand.ScenarioVerb.END => Strings.ScenarioVerbHelpEND,
            _ => string.Empty,
        };
    }

    public static string GetScriptVerbHelp(string verb)
    {
        return Enum.TryParse<CommandVerb>(verb, out var parsedVerb) ? GetScriptVerbHelp(parsedVerb) : string.Empty;
    }
    public static string GetScriptVerbHelp(CommandVerb verb)
    {
        return verb switch
        {
            CommandVerb.AVOID_DISP => Strings.ScriptCommandVerbHelpAVOID_DISP,
            CommandVerb.BACK => Strings.ScriptCommandVerbHelpBACK,
            CommandVerb.BG_DISP => Strings.ScriptCommandVerbHelpBG_DISP,
            CommandVerb.BG_DISP2 => Strings.ScriptCommandVerbHelpBG_DISP2,
            CommandVerb.BG_DISPCG => Strings.ScriptCommandVerbHelpBG_DISPCG,
            CommandVerb.BG_FADE => Strings.ScriptCommandVerbHelpBG_FADE,
            CommandVerb.BG_REVERT => Strings.ScriptCommandVerbHelpBG_REVERT,
            CommandVerb.BG_SCROLL => Strings.ScriptCommandVerbHelpBG_SCROLL,
            CommandVerb.BGM_PLAY => Strings.ScriptCommandVerbHelpBGM_PLAY,
            CommandVerb.CHESS_CLEAR_ANNOTATIONS => Strings.ScriptCommandVerbHelpCHESS_CLEAR_ANNOTATIONS,
            CommandVerb.CHESS_LOAD => Strings.ScriptCommandVerbHelpCHESS_LOAD,
            CommandVerb.CHESS_MOVE => Strings.ScriptCommandVerbHelpCHESS_MOVE,
            CommandVerb.CHESS_RESET => Strings.ScriptCommandVerbHelpCHESS_RESET,
            CommandVerb.CHESS_TOGGLE_CROSS => Strings.ScriptCommandVerbHelpCHESS_TOGGLE_CROSS,
            CommandVerb.CHESS_TOGGLE_GUIDE => Strings.ScriptCommandVerbHelpCHESS_TOGGLE_GUIDE,
            CommandVerb.CHESS_TOGGLE_HIGHLIGHT => Strings.ScriptCommandVerbHelpCHESS_TOGGLE_HIGHLIGHT,
            CommandVerb.CHESS_VGOTO => Strings.ScriptCommandVerbHelpCHESS_VGOTO,
            CommandVerb.CHIBI_EMOTE => Strings.ScriptCommandVerbHelpCHIBI_EMOTE,
            CommandVerb.CHIBI_ENTEREXIT => Strings.ScriptCommandVerbHelpCHIBI_ENTEREXIT,
            CommandVerb.CONFETTI => Strings.ScriptCommandVerbHelpCONFETTI,
            CommandVerb.DIALOGUE => Strings.ScriptCommandVerbHelpDIALOGUE,
            CommandVerb.EPHEADER => Strings.ScriptCommandVerbHelpEPHEADER,
            CommandVerb.FLAG => Strings.ScriptCommandVerbHelpFLAG,
            CommandVerb.GLOBAL2D => Strings.ScriptCommandVerbHelpGLOBAL2D,
            CommandVerb.GOTO => Strings.ScriptCommandVerbHelpGOTO,
            CommandVerb.HARUHI_METER => Strings.ScriptCommandVerbHelpHARUHI_METER,
            CommandVerb.HARUHI_METER_NOSHOW => Strings.ScriptCommandVerbHelpHARUHI_METER_NOSHOW,
            CommandVerb.HOLD => Strings.ScriptCommandVerbHelpHOLD,
            CommandVerb.INIT_READ_FLAG => Strings.ScriptCommandVerbHelpINIT_READ_FLAG,
            CommandVerb.INVEST_END => Strings.ScriptCommandVerbHelpINVEST_END,
            CommandVerb.INVEST_START => Strings.ScriptCommandVerbHelpINVEST_START,
            CommandVerb.ITEM_DISPIMG  => Strings.ScriptCommandVerbHelpITEM_DISPIMG,
            CommandVerb.KBG_DISP  => Strings.ScriptCommandVerbHelpKBG_DISP,
            CommandVerb.LOAD_ISOMAP => Strings.ScriptCommandVerbHelpLOAD_ISOMAP,
            CommandVerb.MODIFY_FRIENDSHIP => Strings.ScriptCommandVerbHelpMODIFY_FRIENDSHIP,
            CommandVerb.NEXT_SCENE => Strings.ScriptCommandVerbHelpNEXT_SCENE,
            CommandVerb.NOOP1 => Strings.ScriptCommandVerbHelpNOOP,
            CommandVerb.NOOP2 => Strings.ScriptCommandVerbHelpNOOP,
            CommandVerb.NOOP3 => Strings.ScriptCommandVerbHelpNOOP,
            CommandVerb.OP_MODE => Strings.ScriptCommandVerbHelpOP_MODE,
            CommandVerb.PALEFFECT => Strings.ScriptCommandVerbHelpPALEFFECT,
            CommandVerb.PIN_MNL => Strings.ScriptCommandVerbHelpPIN_MNL,
            CommandVerb.REMOVED => Strings.ScriptCommandVerbHelpREMOVED,
            CommandVerb.SCENE_GOTO => Strings.ScriptCommandVerbHelpSCENE_GOTO,
            CommandVerb.SCENE_GOTO_CHESS => Strings.ScriptCommandVerbHelpSCENE_GOTO_CHESS,
            CommandVerb.SCREEN_FADEIN => Strings.ScriptCommandVerbHelpSCREEN_FADEIN,
            CommandVerb.SCREEN_FADEOUT => Strings.ScriptCommandVerbHelpSCREEN_FADEOUT,
            CommandVerb.SCREEN_FLASH => Strings.ScriptCommandVerbHelpSCREEN_FLASH,
            CommandVerb.SCREEN_SHAKE => Strings.ScriptCommandVerbHelpSCREEN_SHAKE,
            CommandVerb.SCREEN_SHAKE_STOP => Strings.ScriptCommandVerbHelpSCREEN_SHAKE_STOP,
            CommandVerb.SELECT => Strings.ScriptCommandVerbHelpSELECT,
            CommandVerb.SET_PLACE => Strings.ScriptCommandVerbHelpSET_PLACE,
            CommandVerb.SKIP_SCENE => Strings.ScriptCommandVerbHelpSKIP_SCENE,
            CommandVerb.SND_PLAY => Strings.ScriptCommandVerbHelpSND_PLAY,
            CommandVerb.SND_STOP => Strings.ScriptCommandVerbHelpSND_STOP,
            CommandVerb.STOP => Strings.ScriptCommandVerbHelpSTOP,
            CommandVerb.TOGGLE_DIALOGUE => Strings.ScriptCommandVerbHelpTOGGLE_DIALOGUE,
            CommandVerb.TOPIC_GET => Strings.ScriptCommandVerbHelpTOPIC_GET,
            CommandVerb.TRANS_IN => Strings.ScriptCommandVerbHelpTRANS_IN,
            CommandVerb.TRANS_OUT => Strings.ScriptCommandVerbHelpTRANS_OUT,
            CommandVerb.VCE_PLAY => Strings.ScriptCommandVerbHelpVCE_PLAY,
            CommandVerb.VGOTO => Strings.ScriptCommandVerbHelpVGOTO,
            CommandVerb.WAIT => Strings.ScriptCommandVerbHelpWAIT,
            CommandVerb.WAIT_CANCEL => Strings.ScriptCommandVerbHelpWAIT_CANCEL,
            _ => string.Empty,
        };
    }
}
