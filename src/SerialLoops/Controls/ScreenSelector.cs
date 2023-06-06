using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Utility;
using System;
using static SerialLoops.Lib.Script.Parameters.ScreenScriptParameter;

namespace SerialLoops.Controls
{
    public class ScreenSelector : Panel
    {

        public event EventHandler ScreenChanged;

        private ILogger _log;
        private Button _topScreenButton;
        private Button _bottomScreenButton;
        private CheckBox _bothScreensCheckbox;

        private bool _allowSelectingBoth;
        private DsScreen _selectedScreen;
        public DsScreen SelectedScreen 
        {
            get => _selectedScreen;
            set
            {
                _selectedScreen = value;
                _topScreenButton.Enabled = value is not (DsScreen.TOP or DsScreen.BOTH);
                _bottomScreenButton.Enabled = value is not (DsScreen.BOTTOM or DsScreen.BOTH);
                if (_allowSelectingBoth)
                {
                    _bothScreensCheckbox.Checked = value == DsScreen.BOTH;
                }
                ScreenChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ScreenSelector(ILogger log, DsScreen selectedScreen, bool allowSelectingBoth)
        {
            _log = log;
            _allowSelectingBoth = allowSelectingBoth;
            _selectedScreen = selectedScreen;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _topScreenButton = new Button
            {
                Image = ControlGenerator.GetIcon("Top_Screen", _log),
                Size = new(35, 20),
                Enabled = _selectedScreen is not (DsScreen.TOP or DsScreen.BOTH)
            };
            _topScreenButton.Click += TopScreenButton_Click;

            _bottomScreenButton = new Button
            {
                Image = ControlGenerator.GetIcon("Bottom_Screen", _log),
                Size = new(35, 20),
                Enabled = _selectedScreen is not (DsScreen.BOTTOM or DsScreen.BOTH)
            };
            _bottomScreenButton.Click += BottomScreenButton_Click;

            StackLayout layout = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment = VerticalAlignment.Center,
                Items =
                {
                    new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        Items = { _topScreenButton, _bottomScreenButton },
                        Spacing = 2,
                        Padding = 0
                    }
                },
                Spacing = 5,
                Padding = 0,
            };
            
            if (_allowSelectingBoth)
            {
                _bothScreensCheckbox = new CheckBox
                {
                    Text = "Both",
                    Checked = _selectedScreen == DsScreen.BOTH
                };
                _bothScreensCheckbox.CheckedChanged += BothScreensCheckbox_CheckedChanged;
                layout.Items.Add(_bothScreensCheckbox);
            }
            
            Content = layout;
        }

        private void BothScreensCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            SelectedScreen = _bothScreensCheckbox.Checked is true ? DsScreen.BOTH : DsScreen.TOP;
        }

        private void TopScreenButton_Click(object sender, EventArgs e)
        {
            SelectedScreen = DsScreen.TOP;
        }

        private void BottomScreenButton_Click(object sender, EventArgs e)
        {
            SelectedScreen = DsScreen.BOTTOM;
        }
    }
}
