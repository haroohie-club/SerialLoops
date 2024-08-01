using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Dialogs;

namespace SerialLoops.Views.Dialogs
{
    public partial class BgmLoopPropertiesDialog : Window
    {
        public BgmLoopPropertiesDialog()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Order here matters. Do not change the order. I do not know why. Please don't change the order.
            EndSampleSlider.Value = (double)EndSampleBox.Value;
            StartSampleSlider.Value = (double)StartSampleBox.Value;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            ((BgmLoopPropertiesDialogViewModel)DataContext).LoopPreviewPlayer.Stop();
        }

        private void StartSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            BgmLoopPropertiesDialogViewModel viewModel = (BgmLoopPropertiesDialogViewModel)DataContext;
            if (StartSampleSlider.Value > EndSampleSlider.Value)
            {
                StartSampleSlider.ValueChanged -= StartSlider_ValueChanged;
                StartSampleSlider.Value = EndSampleSlider.Value;
                StartSampleSlider.ValueChanged += StartSlider_ValueChanged;
                return;
            }
            StartSampleBox.Value = (decimal)StartSampleSlider.Value;
            viewModel.LoopPreview.StartSample = viewModel.LoopPreview.GetSampleFromTimestamp(StartSampleSlider.Value);
            viewModel.LoopPreviewPlayer.Stop();
            viewModel.LoopPreviewPlayer.Sound = viewModel.LoopPreview.Wave;
        }

        private void EndSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            BgmLoopPropertiesDialogViewModel viewModel = (BgmLoopPropertiesDialogViewModel)DataContext;
            if (EndSampleSlider.Value < StartSampleSlider.Value)
            {
                EndSampleSlider.ValueChanged -= EndSlider_ValueChanged;
                EndSampleSlider.Value = StartSampleSlider.Value;
                EndSampleSlider.ValueChanged += EndSlider_ValueChanged;
                return;
            }
            EndSampleBox.Value = (decimal)EndSampleSlider.Value;
            viewModel.LoopPreview.EndSample = viewModel.LoopPreview.GetSampleFromTimestamp(EndSampleSlider.Value);
            viewModel.LoopPreviewPlayer.Stop();
        }
    }
}
