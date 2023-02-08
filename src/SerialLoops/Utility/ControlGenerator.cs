﻿using System;
using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;

namespace SerialLoops.Utility
{
    public static class ControlGenerator
    {
        public static StackLayout GetControlWithLabel(string title, Control control)
        {
            return new StackLayout
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Items =
                {
                    title,
                    control,
                },
            };
        }

        public static TableLayout GetControlWithLabelTable(string title, Control control)
        {
            return new TableLayout(new TableRow(new Label { Text = title }, control))
            {
                Spacing = new Size(10, 5)
            };
        }
        
        public static StackLayout GetIconifiedFileName(ItemDescription.ItemType type, string fileName, ILogger log)
        {
            return new StackLayout
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                Spacing = 5,
                Items =
                {
                    new ImageView { Image = new Bitmap(GetItemIcon(type, log)) },
                    fileName
                }
            };
        }

        public static Icon GetItemIcon(ItemDescription.ItemType type, ILogger log)
        {
            try
            {
                return Icon.FromResource($"SerialLoops.Icons.{type}.png").WithSize(16, 16);
            }
            catch (Exception exc)
            {
                log.LogWarning($"Failed to load icon.\n{exc.Message}\n\n{exc.StackTrace}");
                return null;
            }
        }
    }
}
