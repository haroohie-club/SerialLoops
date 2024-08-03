using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace SerialLoops.Utility
{
    public static class GuiExtensions
    {
        public static async Task<IStorageFile> ShowOpenFilePickerAsync(this Window window, string title, IReadOnlyList<FilePickerFileType> fileFilter, string suggestedStartLocation = "")
        {
            FilePickerOpenOptions options = new()
            {
                Title = title,
                FileTypeFilter = fileFilter,
            };
            if (!string.IsNullOrEmpty(suggestedStartLocation))
            {
                options.SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(suggestedStartLocation);
            }
            return (await window.StorageProvider.OpenFilePickerAsync(options)).FirstOrDefault();
        }
        public static async Task<IStorageFile> ShowSaveFilePickerAsync(this Window window, string title, IReadOnlyList<FilePickerFileType> fileFilter, string suggestedFileName = "")
        {
            FilePickerSaveOptions options = new()
            {
                Title = title,
                FileTypeChoices = fileFilter,
                ShowOverwritePrompt = true,
                SuggestedFileName = suggestedFileName,
            };
            return await window.StorageProvider.SaveFilePickerAsync(options);
        }
        public static async Task<IReadOnlyList<IStorageFile>> ShowOpenMultiFilePickerAsync(this Window window, string title, IReadOnlyList<FilePickerFileType> fileFilter)
        {
            FilePickerOpenOptions options = new()
            {
                Title = title,
                FileTypeFilter = fileFilter,
                AllowMultiple = true,
            };
            return await window.StorageProvider.OpenFilePickerAsync(options);
        }
        public static async Task<IStorageFolder> ShowOpenFolderPickerAsync(this Window window, string title)
        {
            FolderPickerOpenOptions options = new()
            {
                Title = title,
                AllowMultiple = false,
            };
            return (await window.StorageProvider.OpenFolderPickerAsync(options)).FirstOrDefault();
        }

        public static async Task<ButtonResult> ShowMessageBoxAsync(this Window window, string title, string message, ButtonEnum buttons, Icon icon, ILogger log)
        {
            MessageBoxStandardParams msgParams = new()
            {
                ButtonDefinitions = buttons,
                Icon = icon,
                WindowIcon = new(ControlGenerator.GetIcon("AppIcon", log)),
                ContentTitle = title,
                ContentHeader = title,
                ContentMessage = message,
                CanResize = false,
                CloseOnClickAway = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                SystemDecorations = SystemDecorations.Full,
            };

            return await MessageBoxManager.GetMessageBoxStandard(msgParams).ShowWindowDialogAsync(window);
        }

        public static void AddRange(this ItemCollection itemCollection, IEnumerable<ContentControl> items)
        {
            foreach (ContentControl item in items)
            {
                itemCollection.Add(item);
            }
        }

        public static void AddRange(this Avalonia.Controls.Controls controlsCollection, IEnumerable<Control> controlsToAdd)
        {
            foreach (Control control in controlsToAdd)
            {
                controlsCollection.Add(control);
            }
        }

        public static NativeMenuItem FindNativeMenuItem(this NativeMenu menu, string header)
        {
            foreach (NativeMenuItemBase itemBase in menu.Items)
            {
                if (itemBase is NativeMenuItem item)
                {
                    if (item.Header.Equals(header))
                    {
                        return item;
                    }
                    else
                    {
                        if (item.Menu?.FindNativeMenuItem(header) is not null)
                        {
                            return item;
                        }
                    }
                }
            }
            return null;
        }
    }
}
