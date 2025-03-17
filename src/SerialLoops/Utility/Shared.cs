﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace SerialLoops.Utility;

public static class Shared
{
    public static CancellationTokenSource AudioReplacementCancellation { get; set; } = new();

    public static string[] SupportedImageFiletypes => ["*.bmp", "*.gif", "*.heif", "*.jpg", "*.jpeg", "*.png", "*.webp",];
    public static string[] SupportedAudioFiletypes => ["*.wav", "*.flac", "*.mp3", "*.ogg",];

    public static void SaveGif(this IEnumerable<SKBitmap> frames, string fileName, IProgressTracker tracker)
    {
        using Image<Rgba32> gif = new(frames.Max(f => f.Width), frames.Max(f => f.Height));
        gif.Metadata.GetGifMetadata().RepeatCount = 0;

        tracker.Focus(Strings.Converting_frames___, 1);
        IEnumerable<Image<Rgba32>> gifFrames = frames.Select(f => Image.LoadPixelData<Rgba32>(f.Pixels.Select(c => new Rgba32(c.Red, c.Green, c.Blue, c.Alpha)).ToArray(), f.Width, f.Height));
        tracker.Finished++;
        tracker.Focus(Strings.Adding_frames_to_GIF___, gifFrames.Count());
        foreach (Image<Rgba32> gifFrame in gifFrames)
        {
            GifFrameMetadata metadata = gifFrame.Frames.RootFrame.Metadata.GetGifMetadata();
            metadata.FrameDelay = 2;
            metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
            gif.Frames.AddFrame(gifFrame.Frames.RootFrame);
            tracker.Finished++;
        }
        gif.Frames.RemoveFrame(0);

        tracker.Focus(Strings.Saving_GIF___, 1);
        gif.SaveAsGif(fileName);
        tracker.Finished++;
    }

    public static SKBitmap GetCharacterVoicePortrait(Project project, ILogger log, CharacterItem character)
    {
        ItemDescription id = project.Items.Find(i => i.Name.Equals("SYSTEX_SYS_CMN_B46"));
        if (id is not SystemTextureItem tex)
        {
            log.LogError(string.Format(Strings.Failed_to_load_character_progress_voice_for__0__, character.DisplayName));
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
}
