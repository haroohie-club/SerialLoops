using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SerialLoops.Views.Dialogs
{
    public partial class ProjectCreationDialog : Window
    {
        public ProjectCreationDialog()
        {
            InitializeComponent();
            NameBox.AddHandler(KeyDownEvent, NameBox_KeyDown, RoutingStrategies.Tunnel);
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.KeySymbol) && !AllowedCharactersRegex().IsMatch(e.KeySymbol) && e.Key != Key.Back)
            {
                e.Handled = true;
                return;
            }
        }

        [GeneratedRegex(@"[A-Za-z\d-_\.]")]
        private static partial Regex AllowedCharactersRegex();
    }
}
