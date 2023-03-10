using System;
using System.Windows;
using System.Windows.Controls;
using Eto.Wpf.Forms.ToolBar;
using Eto.Wpf;
using Eto.Forms;
using Application = Eto.Forms.Application;

namespace SerialLoops.Wpf
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Platform();
            platform.Add<SoundPlayer.ISoundPlayer>(() => new SoundPlayerHandler());

            // Windows toolbar styling
            Eto.Style.Add<ToolBarHandler>("sl-toolbar", toolbar =>
            {
                toolbar.Control.Background = null;
                toolbar.Control.BorderBrush = null;

                toolbar.Control.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                toolbar.Control.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                toolbar.Control.Margin = new(5, 0, 5, 0);
                toolbar.Control.Padding = new(5, 0, 5, 0);
            });

            // Windows toolbar button styling
            Eto.Style.Add<ButtonToolItemHandler>("sl-toolbar-button", button =>
            {
                button.Control.Width = 50;
                button.Control.Height = 40;

                StackPanel stackPanel = new() { Orientation = System.Windows.Controls.Orientation.Vertical };
                TextBlock textBlock = new() { Text = button.Text, TextAlignment = System.Windows.TextAlignment.Center };
                Image image = button.Image.ToWpfImage();
                image.Height = 20;

                stackPanel.Children.Add(image);
                stackPanel.Children.Add(textBlock);
                button.Control.Content = stackPanel;
            });

            new Application(platform).Run(new MainForm());
        }
    }
}
