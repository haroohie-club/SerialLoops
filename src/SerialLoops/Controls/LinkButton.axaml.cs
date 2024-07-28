using Avalonia.Controls;
using System;

namespace SerialLoops.Controls
{
    public partial class LinkButton : UserControl
    {
        public string Text { get => (string)LinkText.Content; set => LinkText.Content = value; }
        public delegate void OnClickDelegate(object sender, EventArgs e);
        public OnClickDelegate OnClick { get; set; }

        public LinkButton()
        {
            InitializeComponent();
        }

        private void Action_Execute(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (IsEnabled)
            {
                OnClick?.Invoke(sender, e);
            }
        }
    }
}