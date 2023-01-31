using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            EventFile evt = new();
            evt.Name = $"{Path.GetFileNameWithoutExtension(evtFile)}S";
            evt.Initialize(File.ReadAllBytes(evtFile), 0, _log);
            EventItem eventItem = new(evt);

            Assert.That(evt.Name[0..^1], Is.EqualTo(eventItem.Name));
        }
    }
}
