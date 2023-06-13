using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;

namespace SerialLoops.Utility {
    
    internal class OptionsGroup : GroupBox {
        public OptionsGroup(string name, List<Option> options, int columns = 1)
        {
            Text = name;
            Padding = 10;

            // Create a table for each column
            List<TableLayout> columnTables = new();
            for (int i = 0; i < columns; i++)
            {
                columnTables.Add(new TableLayout { Spacing = new(2, 2) });
            }
            
            // Populate the tables with the options
            options.ForEach(option => columnTables[options.IndexOf(option) % columns].Rows.Add(option.GetOptionRow()));
            Content = new TableLayout(new TableRow(columnTables.Select(t => new TableCell(t)).ToArray()))
            {
                Spacing = new(10, 5)
            };
        }
    }

    internal abstract class Option {
        public string Name { get; set; }

        protected abstract Control GetControl();

        public TableRow GetOptionRow()
        {
            return new TableRow(
                new Label { Text = Name, VerticalAlignment = VerticalAlignment.Center }, 
                GetControl()
            );
        }
    }

    internal class TextOption : Option {
        public Action<string> OnChange { get; set; }
        protected TextBox _textBox;

        public string Value
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        public TextOption()
        {
            _textBox = new TextBox() {Text = "", Width = 225};
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
                Items = {_textBox}
            };
        }
    }

    internal class BooleanOption : Option {
        public Action<bool> OnChange { get; set; }

        public bool Value
        {
            get => _checkBox.Checked is true;
            set => _checkBox.Checked = value;
        }

        private readonly CheckBox _checkBox;

        public BooleanOption()
        {
            _checkBox = new CheckBox
                {Checked = false};
            _checkBox.CheckedChanged += (sender, e) => OnChange?.Invoke(Value);
        }

        protected override Control GetControl()
        {
            return new StackLayout
            {
                Padding = 5,
                Items = {_checkBox},
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
        }
    }

    internal class FolderOption : FileOption {
        protected override void SelectButton_OnClick(object sender, EventArgs e)
        {
            SelectFolderDialog selectFolderDialog = new();
            if (selectFolderDialog.ShowAndReportIfFileSelected(GetControl()))
            {
                _pathBox.Text = selectFolderDialog.Directory;
            }
        }
    }

    internal class FileOption : Option {
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
            _pathBox = new TextBox
                {Text = "", Width = 225};
            _pathBox.TextChanged += (sender, args) => { OnChange?.Invoke(Path); };

            _pickerButton = new Button() {Text = "Select..."};
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
                Items = {_pathBox, _pickerButton}
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