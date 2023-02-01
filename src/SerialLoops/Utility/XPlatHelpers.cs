using Eto.Forms;

namespace SerialLoops.Utility
{
    /// <summary>
    /// Helper methods to deal with cross-plat weirdness
    /// </summary>
    public static class XPlatHelpers
    {
        public static bool ShowAndReportIfFileSelected(this OpenFileDialog openFileDialog, Control parent)
        {
            DialogResult result = openFileDialog.ShowDialog(parent);
            return result == DialogResult.Ok || result == DialogResult.Ignore; // "Ignore" is returned on Linux
        }
    }
}
