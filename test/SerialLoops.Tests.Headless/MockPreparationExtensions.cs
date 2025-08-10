using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Audio.SDAT.SoundArchiveComponents;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using Moq;
using SerialLoops.Lib;
using SerialLoops.Tests.Shared;
using SerialLoops.ViewModels;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views;

namespace SerialLoops.Tests.Headless;

public static class MockPreparationExtensions
{
    public static void SetupCommonMocks(this Mock<MainWindowViewModel> windowVmMock)
    {
        windowVmMock.SetupAllProperties();
    }

    public static void SetupMockedProperties(this MainWindowViewModel mockedWindowVm, Project project, ILogger log)
    {
        string testId = Guid.NewGuid().ToString();
        ConfigUser configUser = new() { UserDirectory = Path.Combine(Path.GetTempPath(), testId) };
        ConfigFactoryMock configFactory = new($"{testId}.json", configUser);

        MainWindow window = new() { ConfigurationFactory = configFactory };
        mockedWindowVm.Window = window;
        mockedWindowVm.OpenProject = project;
        EditorTabsPanelViewModel editorTabs = new(mockedWindowVm, project, log);
        ItemExplorerPanelViewModel itemExplorer = new(null, mockedWindowVm);
        mockedWindowVm.EditorTabs = editorTabs;
        mockedWindowVm.ItemExplorer = itemExplorer;
        mockedWindowVm.Window.DataContext = mockedWindowVm;
    }


    public static void SetupCommonMocks(this Mock<Project> projectMock)
    {
        projectMock.SetupAllProperties();
    }
    public static void SetupMockedProperties(this Project mockedProject, UiVals uiVals)
    {
        FontReplacementDictionary fontReplacement = [];
        fontReplacement.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(File.ReadAllText(Path.Combine(UiVals.BaseDirectory, "Sources", "charset.json"))));
        mockedProject.FontReplacement = fontReplacement;
        mockedProject.LangCode = "en";
        mockedProject.Name = "Test";

        mockedProject.Localize = s => s;
    }

    public static void SetupFileAccessMocks<T>(this Mock<ArchiveFile<T>> archiveMock, T file, int index, string name) where T : FileInArchive, new()
    {
        archiveMock.Setup(a => a.GetFileByName(name)).Returns(file);
        archiveMock.Setup(a => a.GetFileByIndex(index)).Returns(file);
    }

    public static void SetupMockedFile<T>(this Mock<ArchiveFile<T>> archiveFile, string file, string dir, int index, ILogger log, ref int offset) where T : FileInArchive, new()
    {
        string fileName = file.Replace(".", "");
        T fileInArchive = new() { Index = index, Name = fileName };
        fileInArchive.Initialize(File.ReadAllBytes(Path.Combine(dir, file)), offset++, log);
        archiveFile.SetupFileAccessMocks(fileInArchive, index, fileName);
    }

    public static void SetupGrpMock(this Mock<ArchiveFile<GraphicsFile>> grpMock, string assetDirectory, ILogger log)
    {
        int offset = 0;
        grpMock.SetupMockedFile("BG_OKUD0_128.DNX", assetDirectory, 0x075, log, ref offset);
        grpMock.SetupMockedFile("BG_OKUD0_64.DNX", assetDirectory, 0x076, log, ref offset);
        grpMock.SetupMockedFile("BG_ROUD2_128.DNX", assetDirectory, 0x0B3, log, ref offset);
        grpMock.SetupMockedFile("BG_ROUD2_64.DNX", assetDirectory, 0x0B4, log, ref offset);
        grpMock.SetupMockedFile("EV352_128.DNX", assetDirectory, 0x387, log, ref offset);
        grpMock.SetupMockedFile("EV352_64.DNX", assetDirectory, 0x388, log, ref offset);
        grpMock.SetupMockedFile("KBG00.BNS", assetDirectory, 0x50D, log, ref offset);
        grpMock.SetupMockedFile("KBG00_128.DNX", assetDirectory, 0x50E, log, ref offset);
        grpMock.SetupMockedFile("EV150_D_256.DNX", assetDirectory, 0x317, log, ref offset);
        grpMock.SetupMockedFile("EV150_U_256.DNX", assetDirectory, 0x318, log, ref offset);
        grpMock.SetupMockedFile("EV285_128.DNX", assetDirectory, 0x35D, log, ref offset);
        grpMock.SetupMockedFile("EV285_64.DNX", assetDirectory, 0x35E, log, ref offset);
        grpMock.SetupMockedFile("EV381_256.DNX", assetDirectory, 0x397, log, ref offset);
    }

    public static void SetupSndMock(this SoundArchive sndMock, SfxEntry[] entries, ILogger log)
    {
        sndMock.SequenceArchives = [];
        sndMock.Groups = [];

        foreach (SfxEntry entry in entries)
        {
            while (sndMock.SequenceArchives.Count <= entry.SequenceArchive)
            {
                sndMock.SequenceArchives.Add(new() { File = new() });
            }
            while (sndMock.SequenceArchives[entry.SequenceArchive].File.Sequences.Count <= entry.Index)
            {
                sndMock.SequenceArchives[entry.SequenceArchive].File.Sequences.Add(new());
            }
            sndMock.SequenceArchives[entry.SequenceArchive].File.Sequences[entry.Index].Bank = new() { Name = "BANK_SE_fONMEM" };
        }
        sndMock.Groups.Add(new() { Name = "GROUP_ONMEM", Entries = [ new() { LoadBank = true, Entry = new BankInfo { Name = "BANK_ONMEM" }} ] });
    }
}
