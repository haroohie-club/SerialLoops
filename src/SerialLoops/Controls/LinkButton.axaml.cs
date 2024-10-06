using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SerialLoops.Controls
{
    public partial class LinkButton : UserControl
    {
        public string Icon
        {
            get => IconPath.Path;
            set
            {
                IconPath.Path = string.IsNullOrEmpty(value) ? string.Empty : $"avares://SerialLoops/Assets/Icons/{value}.svg";
                IconPath.IsVisible = !string.IsNullOrEmpty(value);
            }
        }
        public string Text
        {
            get => LinkText.Text;
            set => LinkText.Text = value;
        }
        public delegate void OnClickDelegate(object sender, EventArgs e);
        public OnClickDelegate OnClick { get; set; }

        public LinkButton()
        {
            InitializeComponent();
        }

        private void Action_Execute(object? sender, RoutedEventArgs e)
        {
            if (IsEnabled)
            {
                OnClick?.Invoke(sender, e);
            }
        }
    }
}
