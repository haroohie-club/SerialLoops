using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;

namespace SerialLoops.Utility
{

    internal class OptionsGroup : GroupBox
    {

        public OptionsGroup(string name, List<Option> options, int columns = 1)
        {
            Text = name;
            Padding = 10;

            // Create a table for each column
            List<TableLayout> columnTables = new();
            for (int i = 0; i < columns; i++)
            {
                columnTables.Add(new TableLayout { Spacing = new(1, 1) });
            }

            // Populate the tables with the options
            options.ForEach(option => columnTables[options.IndexOf(option) % columns].Rows.Add(option.GetOptionRow()));
            Content = new TableLayout(new TableRow(columnTables.Select(t => new TableCell(t)).ToArray()))
            {
                Spacing = new(10, 5)
            };
        }
    }

    internal abstract class Option
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        protected abstract Control GetControl();

        protected virtual Control GetLabel()
        {
            return new Label { Text = Name, VerticalAlignment = VerticalAlignment.Center };
        }

        public TableRow GetOptionRow()
        {
            return new TableRow(GetLabel(), GetControl());
        }
    }

    internal class TextOption : Option
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
            return new StackLayout
            {
                Padding = 5,
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment = VerticalAlignment.Center,
                Items = { _textBox },
                Enabled = Enabled,
            };
        }
    }

    internal class BooleanOption : Option
    {
        public Action<bool> OnChange { get; set; }

        public bool Value
        {
            get => _checkBox.Checked is true;
            set => _checkBox.Checked = value;
        }

        private readonly CheckBox _checkBox;

        public BooleanOption()
        {
            _checkBox = new CheckBox { Checked = false };
            _checkBox.CheckedChanged += (sender, e) => OnChange?.Invoke(Value);
        }

        protected override Control GetControl()
        {
            return new StackLayout
            {
                Padding = 5,
                Items = { _checkBox },
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment = VerticalAlignment.Center,
                Enabled = Enabled,
            };
        }
    }

    internal class ItemBooleanOption : BooleanOption
    {

        private readonly ILogger _logger;

        public ItemBooleanOption(ILogger logger) : base()
        {
            _logger = logger;
        }

        public ItemDescription.ItemType Type
        {
            get => Enum.Parse<ItemDescription.ItemType>(Name);
            set => Name = value.ToString();
        }

        protected override Control GetLabel()
        {
            return ControlGenerator.GetControlWithIcon(Name.Replace("_", " "), Name, _logger);
        }
    }

    internal class BooleanToggleOption : BooleanOption
    {

        public LinkButton ToggleButton;
        private string _buttonText => Value ? "All On" : "All Off";

        public BooleanToggleOption(List<Option> options)
        {
            ToggleButton = new LinkButton { Text = _buttonText, Enabled = Enabled };
            ToggleButton.Click += (sender, args) =>
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

    internal class FolderOption : FileOption
    {
        protected override void SelectButton_OnClick(object sender, EventArgs e)
        {
            SelectFolderDialog selectFolderDialog = new();
            if (selectFolderDialog.ShowAndReportIfFolderSelected(GetControl()))
            {
                _pathBox.Text = selectFolderDialog.Directory;
            }
        }
    }

    internal class FileOption : Option
    {
        public Action<string> OnChange { get; set; }

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

        public FileOption()
        {
            _pathBox = new TextBox { Text = "", Width = 225 };
            _pathBox.TextChanged += (sender, args) => { OnChange?.Invoke(Path); };
            _pathBox.Enabled = Enabled;

            _pickerButton = new Button() { Text = "Select...", Enabled = Enabled };
            _pickerButton.Click += SelectButton_OnClick;
        }

        protected override Control GetControl()
        {
            return new StackLayout
            {
                Padding = 5,
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment = VerticalAlignment.Center,
                Items = { _pathBox, _pickerButton }
            };
        }

        protected virtual void SelectButton_OnClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowAndReportIfFileSelected(GetControl()))
            {
                _pathBox.Text = openFileDialog.FileName;
            }
        }
    }

}