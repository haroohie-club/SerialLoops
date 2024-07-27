using Avalonia.Controls;
using System;

namespace SerialLoops.Controls
{
    public partial class LinkButton : UserControl
    {
        public string Text { get => LinkText.Text; set => LinkText.Text = value; }
        public delegate void OnClickDelegate(object sender, EventArgs e);
        public OnClickDelegate OnClick { get; set; }

        public LinkButton()
        {
            InitializeComponent();
        }

        private void Action_Execute(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            OnClick?.Invoke(sender, e);
        }
    }
}