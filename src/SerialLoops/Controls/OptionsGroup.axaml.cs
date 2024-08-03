using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Controls
{
    public partial class OptionsGroup : UserControl
    {
        public string Text { get; set; }

        public OptionsGroup()
        {
            InitializeComponent();
        }

        public void InitializeOptions(string name, List<Option> options, int columns = 1)
        {
            Text = name;
            Margin = new(10);

            int currentRow = 1;
            List<Grid> grids = [];
            for (int i = 0; i < options.Count; i++)
            {
                Grid grid = options[i].GetOptionRow();
                Grid.SetRow(grid, currentRow);
                Grid.SetColumn(grid, i % columns);
                if (i % columns == columns - 1)
                {
                    currentRow++;
                }
                grids.Add(grid);
            }

            Grid bigGrid = new()
            {
                RowDefinitions = new("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
                ColumnDefinitions = new("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
            };
            bigGrid.Children.AddRange(grids);
            Content = bigGrid;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            this.TryFindResource("GroupLineColor", ActualThemeVariant, out object? brush);
            context.DrawRectangle(new Pen((ImmutableSolidColorBrush)brush), new Rect(RenderTransformOrigin.Point, new Point(RenderTransformOrigin.Point.X + Bounds.Size.Width - 2, RenderTransformOrigin.Point.Y + Bounds.Size.Height - 2)), 5);
        }
    }

    public abstract class Option
    {
        public string OptionName { get; set; }

        public bool Enabled { get; set; } = true;

        protected abstract Control GetControl();

        protected virtual Control GetText()
        {
            return new TextBlock { Text = OptionName, Margin = new(5), VerticalAlignment = VerticalAlignment.Center };
        }

        public Grid GetOptionRow()
        {
            Grid grid = new()
            {
                ColumnDefinitions = new("Auto,*,Auto"),
                Margin = new(10),
                MinWidth = 465
            };
            Control textBlock = GetText();
            Control control = GetControl();
            control.SetValue(Control.NameProperty, $"{OptionName.Replace(" ", "").Replace("-", "")}Control");

            Grid.SetColumn(textBlock, 0);
            Grid.SetColumn(control, 2);

            grid.Children.Add(textBlock);
            grid.Children.Add(control);

            return grid;
        }
    }
    public class TextOption : Option
    {
        public Action<string> OnChange { get; set; }
        protected TextBox _textBox;

        public string Value
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        public TextOption()
        {
            _textBox = new TextBox { Text = "", Width = 225 };
            _textBox.TextChanged += (sender, args) => { OnChange?.Invoke(Value); };
        }

        protected override Control GetControl()
        {
            StackPanel panel = new()
            {
                Margin = new(5),
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = Enabled,
            };
            panel.Children.Add(_textBox);
            return panel;
        }
    }

    public class ComboBoxOption : Option
    {
        public Action<string> OnChange { get; set; }
        protected ComboBox _comboBox { get; set; }

        public string Value
        {
            get => (string)((ComboBoxItem)_comboBox.Items[_comboBox.SelectedIndex]).Tag;
            set => _comboBox.SelectedIndex = _comboBox.Items.ToList().FindIndex(i => value.Equals((string)((ComboBoxItem)i).Tag));
        }

        public ComboBoxOption(List<(string Key, string Value)> options)
        {
            _comboBox = new ComboBox();
            _comboBox.Items.AddRange(options.Select(o => new ComboBoxItem { Tag = o.Key, Content = o.Value }));
            _comboBox.SelectionChanged += (sender, args) => { OnChange?.Invoke(Value); };
        }

        protected override Control GetControl()
        {
            StackPanel panel = new()
            {
                Margin = new(5),
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = Enabled,
            };
            panel.Children.Add(_comboBox);
            return panel;
        }
    }

    public class BooleanOption : Option
    {
        public Action<bool> OnChange { get; set; }

        public bool Value
        {
            get => _checkBox.IsChecked is true;
            set => _checkBox.IsChecked = value;
        }

        private readonly CheckBox _checkBox;

        public BooleanOption()
        {
            _checkBox = new CheckBox { IsChecked = false };
            _checkBox.IsCheckedChanged += (sender, e) => OnChange?.Invoke(Value);
        }

        protected override Control GetControl()
        {
            StackPanel panel = new()
            {
                Margin = new(5),
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = Enabled,
            };
            panel.Children.Add(_checkBox);
            return panel;
        }
    }

    public class ItemBooleanOption : BooleanOption
    {

        private readonly ILogger _logger;

        public ItemBooleanOption(ILogger logger) : base()
        {
            _logger = logger;
        }

        public ItemDescription.ItemType Type
        {
            get => Enum.Parse<ItemDescription.ItemType>(OptionName);
            set => OptionName = value.ToString();
        }

        protected override Control GetText()
        {
            return ControlGenerator.GetControlWithIcon(new TextBlock { Text = OptionName.Replace("_", " ") }, OptionName, _logger);
        }
    }

    public class BooleanToggleOption : BooleanOption
    {

        public LinkButton ToggleButton;
        private string _buttonText => Value ? Strings.All_On : Strings.All_Off;

        public BooleanToggleOption(List<Option> options)
        {
            ToggleButton = new LinkButton { Text = _buttonText, IsEnabled = Enabled };
            ToggleButton.OnClick = (sender, args) =>
            {
                options.OfType<BooleanOption>().ToList().ForEach(option => option.Value = Value);
                Value = !Value;
                ToggleButton.Text = _buttonText;
            };
        }

        protected override Control GetControl()
        {
            return ToggleButton;
        }
    }

    public class FileOption : Option
    {
        public Action<string> OnChange { get; set; }
        protected Window _window;

        public string Path
        {
            get => _pathBox.Text;
            set
            {
                _pathBox.Text = value;
            }
        }

        protected TextBox _pathBox;
        private readonly Button _pickerButton;

        public FileOption(Window window)
        {
            _pathBox = new TextBox { Text = "", Width = 225 };
            _pathBox.TextChanged += (sender, args) => { OnChange?.Invoke(Path); };
            _pathBox.IsEnabled = Enabled;

            _pickerButton = new Button() { Content = "Select...", IsEnabled = Enabled };
            _pickerButton.Command = ReactiveCommand.Create(SelectButton_OnClick);
            _window = window;
        }

        protected override Control GetControl()
        {
            StackPanel panel = new()
            {
                Margin = new(5),
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(_pathBox);
            panel.Children.Add(_pickerButton);
            return panel;
        }

        protected virtual async Task SelectButton_OnClick()
        {
            IStorageFile file = await _window.ShowOpenFilePickerAsync(string.Empty, []);
            if (file is not null)
            {
                _pathBox.Text = file.Path.LocalPath;
            }
        }
    }

    public class FolderOption : FileOption
    {
        public FolderOption(Window window) : base(window)
        {
        }

        protected override async Task SelectButton_OnClick()
        {
            IStorageFolder folder = await _window.ShowOpenFolderPickerAsync(string.Empty);
            if (folder is not null)
            {
                _pathBox.Text = folder.Path.LocalPath;
            }
        }
    }

}
