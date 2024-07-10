using Eto.Forms.Controls.SkiaSharp.Shared;
using Eto.Forms.Controls.SkiaSharp.WinForms;
using Eto.Forms.Controls.SkiaSharp.Wpf;
using Eto.Wpf;
using Eto.Wpf.Forms.ToolBar;
using SerialLoops.Utility;
using System;
using System.Windows.Controls;
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
            platform.Add<SfxMixer.ISfxMixer>(() => new SfxMixerHandler());
            platform.Add<SKControl.ISKControl>(() => new SKControlHandler());
            platform.Add<SKGLControl.ISKGLControl>(() => new SKGLControlHandler());

            // Windows toolbar styling
            Eto.Style.Add<ToolBarHandler>("sl-toolbar", toolbar =>
            {
                toolbar.Control.Background = null;
                toolbar.Control.BorderBrush = null;

                toolbar.Control.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                toolbar.Control.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                toolbar.Control.Margin = new(3, 0, 3, 0);
                toolbar.Control.Padding = new(3, 0, 3, 0);
            });

            // Windows toolbar button & separator styling
            Eto.Style.Add<ButtonToolItemHandler>("sl-toolbar-button", button =>
            {
                button.Control.Width = 55;
                button.Control.Height = 45;

                StackPanel stackPanel = new() { Orientation = Orientation.Vertical };
                TextBlock textBlock = new() { Text = button.Text, TextAlignment = System.Windows.TextAlignment.Center };
                Image image = button.Image.ToWpfImage();
                image.Height = 20;

                stackPanel.Children.Add(image);
                stackPanel.Children.Add(textBlock);
                button.Control.Content = stackPanel;
            });
            Eto.Style.Add<SeparatorToolItemHandler>("sl-toolbar-separator", separator =>
            {
                separator.Control.Padding = new(5, 0, 5, 0);
                separator.Control.Margin = new(5, 0, 5, 0);
            });

            Application application = new(platform);
            MainForm mainForm = new();
            try
            {
                application.Run(mainForm);
            }
            catch (Exception ex)
            {
                mainForm.Log.LogCrash(ex);
            }
        }
    }
}
