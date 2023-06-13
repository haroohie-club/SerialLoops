using Eto.Forms;

namespace SerialLoops.Controls {
 
    public class CollapsiblePanel : Panel {
        public Control Control { get; set; } = new Panel();

        private string _titleText = "Title";
        public string Title {
            get => _titleText;
            set {
                _titleText = value;
                _title.Text = (_collapsed ? "▶ " : "▼ ") + value;
            }
        }
        private bool _collapsed;
        public bool Collapsed {
            get => _collapsed;
            set {
                _collapsed = value;
                _title.Text = (value ? "▶ " : "▼ ") + _titleText;
            }
        }

        private Label _title = new()
        {
            Text = "Title", 
            TextAlignment = TextAlignment.Left, 
            VerticalAlignment = VerticalAlignment.Center,
        };
        
        public CollapsiblePanel(string title, Control content) {
            Collapsed = true;
            Title = title;
            Control = content;
            Content = GetContent();
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            Control.Visible = !Collapsed;
            _title.MouseDown += (sender, args) =>
            {
                Collapsed = !Collapsed;
                Control.Visible = !Collapsed;
                Content = GetContent();
            };
        }

        public Control GetContent()
        {
            return new StackLayout 
            { 
                Padding = 10,
                Items = { _title, Control }
            };
        }

    }
    
}