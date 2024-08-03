using Avalonia.Controls;

namespace SerialLoops.Controls
{

    public partial class LabelWithIcon : UserControl
    {
        public string Icon
        {
            get => IconPath.Path;
            set => IconPath.Path = $"avares://SerialLoops/Assets/Icons/{value}.svg";
        }

        public string Text
        {
            get => LabelText.Text;
            set => LabelText.Text = value;
        }

        public LabelWithIcon()
        {
            InitializeComponent();
        }

    }

}
