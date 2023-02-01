using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using SerialLoops.Lib.Items;
using SkiaSharp;
using System;
using System.IO;

namespace SerialLoops.Tests
{
    public class ItemTests
    {
        private ConsoleLogger _log;

        [OneTimeSetUp]
        public void SetUp()
        {
            _log = new();
        }

        private static string[] EventFiles()
        {
            return Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inputs", "events"));
        }

        [Test]
        [TestCaseSource(nameof(EventFiles))]
        [Parallelizable(ParallelScope.All)]
        public void EventItemCreationTest(string evtFile)
        {
            EventFile evt = new() { Name = $"{Path.GetFileNameWithoutExtension(evtFile)}S" };
            evt.Initialize(File.ReadAllBytes(evtFile), 0, _log);
            ScriptItem eventItem = new(evt);

            Assert.That(evt.Name[0..^1], Is.EqualTo(eventItem.Name));
        }

        [Test]
        public void BackgroundItemCreationTest()
        {
            GraphicsFile grp1 = new() { Name = "KBG00_128DNX" };
            GraphicsFile grp2 = new() { Name = "KBG00BNS" };
            grp1.Initialize(File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inputs", "graphics", "KBG00_128.bin")), 0, _log);
            grp2.Initialize(File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inputs", "graphics", "KBG00.bin")), 0, _log);
            BackgroundItem item = new($"BG_{grp2.Name}") { Graphic1 = grp1, Graphic2 = grp2, BackgroundType = BgType.KINETIC_SCREEN };
            SKBitmap bitmap = item.GetBackground();
            bitmap.Encode(SKEncodedImageFormat.Png, 1); // Make sure we can encode, something we have to do to display the image
        }
    }
}
