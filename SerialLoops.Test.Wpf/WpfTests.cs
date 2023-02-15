using Eto.Forms;
using Eto.UnitTest.NUnit;
using NUnit.Framework;
using SerialLoops.Editors;

namespace SerialLoops.Test.Wpf
{
    [TestFixture]
    public class WpfTests
    {
        public static BackgroundEditor editor;

        [Test, InvokeOnUI]
        public void Test()
        {
            Assert.Pass();
        }
    }
}