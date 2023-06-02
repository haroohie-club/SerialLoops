using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Utility;
using System;

namespace SerialLoops.Controls
{
    public class ScreenSelector : Panel
    {

        public event EventHandler ScreenChanged;

        private ILogger _log;
        private Button _topScreenButton;
        private Button _bottomScreenButton;

        private bool _topScreenSelected;
        public bool TopScreenSelected 
        {
            get => _topScreenSelected;
            set
            {
                _topScreenSelected = value;
                _topScreenButton.Enabled = !value;
                _bottomScreenButton.Enabled = value;
                ScreenChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ScreenSelector(ILogger log, bool topScreenSelected = true)
        {
            _log = log;
            _topScreenSelected = topScreenSelected;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _topScreenButton = new Button
            {
                Image = ControlGenerator.GetIcon("Top_Screen", _log),
                Size = new(35, 21),
                Enabled = !_topScreenSelected
            };
            _topScreenButton.Click += TopScreenButton_Click;

            _bottomScreenButton = new Button
            {
                Image = ControlGenerator.GetIcon("Bottom_Screen", _log),
                Size = new(35, 21),
                Enabled = _topScreenSelected
            };
            _bottomScreenButton.Click += BottomScreenButton_Click;

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items = { _topScreenButton, _bottomScreenButton },
                Spacing = 2,
                Padding = 0,
            };
        }

        private void TopScreenButton_Click(object sender, EventArgs e)
        {
            TopScreenSelected = true;
        }

        private void BottomScreenButton_Click(object sender, EventArgs e)
        {
            TopScreenSelected = false;
        }
    }
}
