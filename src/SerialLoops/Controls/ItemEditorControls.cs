using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace SerialLoops.Controls
{
    public class ScriptCommandDropDown : DropDown
    {
        public ScriptItemCommand Command { get; set; }
        public int ParameterIndex { get; set; }
        public ClearableLinkButton Link { get; set; }
    }
    public class ScriptCommandCheckBox : CheckBox
    {
        public ScriptItemCommand Command { get; set; }
        public int ParameterIndex { get; set; }
    }
    public class ScriptCommandColorPicker : ColorPicker
    {
        public ScriptItemCommand Command { get; set; }
        public int ParameterIndex { get; set; }
    }
    public class ScriptCommandTextBox : TextBox
    {
        public ScriptItemCommand Command { get; set; }
        public int ParameterIndex { get; set; }
    }
    public class ScriptCommandTextArea : TextArea
    {
        public ScriptItemCommand Command { get; set; }
        public int ParameterIndex { get; set; }
    }
    public class ScriptCommandNumericStepper : NumericStepper
    {
        public ScriptItemCommand Command { get; set; }
        public int ParameterIndex { get; set; }
    }
    public class TopicSelectButton : Button
    {
        public ScriptItemCommand ScriptCommand { get; set; }
        public int ParameterIndex { get; set; }
        public StackLayout Layout { get; set; }
    }

    public class ScenarioCommandDropDown : DropDown
    {
        public int CommandIndex { get; set; }
        public bool ModifyCommand { get; set; } // false = modify parameter
        public ClearableLinkButton Link { get; set; }
        public StackLayout ParameterLayout { get; set; }
    }
    public class ScenarioCommandTextBox : TextBox
    {
        public int CommandIndex { get; set; }
    }
    public class ScenarioCommandButton : Button
    {
        public int CommandIndex { get; set; }
    }

    public class ClearableLinkButton : LinkButton
    {
        public List<EventHandler<EventArgs>> ClickEvents { get; set; } = new();

        public event EventHandler<EventArgs> ClickUnique
        {
            add
            {
                Click += value;
                ClickEvents.Add(value);
            }
            remove
            {
                Click -= value;
                ClickEvents.Remove(value);
            }
        }

        public void RemoveAllClickEvents()
        {
            foreach (EventHandler<EventArgs> eh in ClickEvents)
            {
                Click -= eh;
            }
            ClickEvents.Clear();
        }
    }

    public class ChibiStackLayout : StackLayout
    {
        public int ChibiIndex { get; set; }
        public SKBitmap ChibiBitmap { get; set; }
    }

    public class CommandGraphicSelectionButton : Panel
    {
        private readonly EditorTabsPanel _editorTabs;
        private readonly ILogger _log;
        public IPreviewableGraphic Selected { get; private set; }
        public ScriptItemCommand Command { get; set; }
        public int ParameterIndex { get; set; }
        public Command SelectedChanged { get; }
        public List<IPreviewableGraphic> Items { get; }
        public Project Project { get; set; }

        public CommandGraphicSelectionButton(IPreviewableGraphic selected, EditorTabsPanel editorTabs, ILogger log)
        {
            _editorTabs = editorTabs;
            _log = log;
            Selected = selected;
            
            SelectedChanged = new();
            Items = new List<IPreviewableGraphic>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Content = GetButtonPanel();
        }

        private StackLayout GetButtonPanel()
        {
            Button button = new() { Text = "Select..." };
            button.Click += GraphicSelectionButton_Click;

            return new StackLayout
            {
                Spacing = 10,
                Orientation = Orientation.Horizontal,
                Items =
                {
                    button,
                    ControlGenerator.GetFileLink(((ItemDescription)Selected), _editorTabs, _log)
                }
            };
        }

        private void GraphicSelectionButton_Click(object sender, EventArgs e)
        {
            GraphicSelectionDialog dialog = new(new(Items), Selected, Project, _log);
            IPreviewableGraphic description = dialog.ShowModal(this);
            if (description == null) return;
            Selected = description;
            SelectedChanged?.Execute();
            Content = GetButtonPanel();
        }
    }
}