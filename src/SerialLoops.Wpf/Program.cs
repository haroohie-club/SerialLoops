using Eto;
using Eto.Forms;
using Eto.Wpf;
using Eto.Wpf.Forms.ToolBar;
using SerialLoops.Lib.Items;
using System;
using System.Linq;
using System.Windows.Controls;

namespace SerialLoops.Wpf
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Eto.Wpf.Platform();
            platform.Add<SoundPlayer.ISoundPlayer>(() => new SoundPlayerHandler());

            // Windows toolbar styling
            Style.Add<ToolBarHandler>("sl-toolbar", toolbar =>
            {
                toolbar.Control.Background = null;
                toolbar.Control.BorderBrush = null;

                toolbar.Control.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                toolbar.Control.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                toolbar.Control.Margin = new System.Windows.Thickness(0, 0, 0, 0);
                toolbar.Control.Padding = new System.Windows.Thickness(5, 0, 5, 0);
            });

            // Windows toolbar button styling
            Style.Add<ButtonToolItemHandler>("sl-toolbar-button", button =>
            {
                button.Text = string.Empty;
                button.Control.Width = 25;
                button.Control.Height = 25;
                button.Image = button.Image.With(i => { i.ToWpfImage().RenderSize = new System.Windows.Size(25, 25); });
            });

            new Application(platform).Run(new MainForm());
        }
    }
}
