using Eto.Drawing;
using Eto.IO;

namespace SerialLoops.Gtk
{
    public class GtkIconHandler : SystemIcons.IHandler
    {
        public Icon GetFileIcon(string fileName, IconSize size)
        {
            switch (size)
            {
                default:
                case IconSize.Small:
                    return Icon.FromResource("SerialLoops.Gtk.AppIcon.png").WithSize(16, 16);

                case IconSize.Large:
                    return Icon.FromResource("SerialLoops.Gtk.AppIcon.png").WithSize(64, 64);
            }
        }

        public Icon GetStaticIcon(StaticIconType type, IconSize size)
        {
            switch (size)
            {
                default:
                case IconSize.Small:
                    return Icon.FromResource("SerialLoops.Gtk.AppIcon.png").WithSize(16, 16);

                case IconSize.Large:
                    return Icon.FromResource("SerialLoops.Gtk.AppIcon.png").WithSize(64, 64);
            }
        }
    }
}
