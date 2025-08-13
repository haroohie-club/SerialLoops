using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Dialogs;

namespace SerialLoops.Views.Dialogs;

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
        EndSampleSlider.Value = (double)EndSampleBox.Value!;
        StartSampleSlider.Value = (double)StartSampleBox.Value!;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        ((BgmLoopPropertiesDialogViewModel)DataContext)!.LoopPreviewPlayer.Stop();
    }

    private void StartSlider_ValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        BgmLoopPropertiesDialogViewModel viewModel = (BgmLoopPropertiesDialogViewModel)DataContext;
        if (StartSampleSlider.Value > EndSampleSlider.Value)
        {
            StartSampleSlider.ValueChanged -= StartSlider_ValueChanged;
            StartSampleSlider.Value = EndSampleSlider.Value;
            StartSampleSlider.ValueChanged += StartSlider_ValueChanged;
            return;
        }
        StartSampleBox.ValueChanged -= StartSampleBox_OnValueChanged;
        StartSampleBox.Value = (decimal)StartSampleSlider.Value;
        StartSampleBox.ValueChanged += StartSampleBox_OnValueChanged;

        viewModel!.LoopPreview.StartSample = viewModel.LoopPreview.GetSampleFromTimestamp(StartSampleSlider.Value);
        viewModel.LoopPreviewPlayer.Stop();
    }

    private void EndSlider_ValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        BgmLoopPropertiesDialogViewModel viewModel = (BgmLoopPropertiesDialogViewModel)DataContext;
        if (EndSampleSlider.Value < StartSampleSlider.Value)
        {
            EndSampleSlider.ValueChanged -= EndSlider_ValueChanged;
            EndSampleSlider.Value = StartSampleSlider.Value;
            EndSampleSlider.ValueChanged += EndSlider_ValueChanged;
            return;
        }
        EndSampleBox.ValueChanged -= EndSampleBox_OnValueChanged;
        EndSampleBox.Value = (decimal)EndSampleSlider.Value;
        EndSampleBox.ValueChanged += EndSampleBox_OnValueChanged;

        viewModel!.LoopPreview.EndSample = viewModel.LoopPreview.GetSampleFromTimestamp(EndSampleSlider.Value);
        viewModel.LoopPreviewPlayer.Stop();
    }

    private void StartSampleBox_OnValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
    {
        BgmLoopPropertiesDialogViewModel viewModel = (BgmLoopPropertiesDialogViewModel)DataContext;
        if (StartSampleBox is null || EndSampleBox is null || StartSampleSlider is null)
        {
            return;
        }
        if (StartSampleBox.Value > EndSampleBox.Value && EndSampleBox.Value != 0)
        {
            StartSampleBox!.ValueChanged -= StartSampleBox_OnValueChanged;
            StartSampleBox.Value = EndSampleBox!.Value;
            StartSampleBox.ValueChanged += StartSampleBox_OnValueChanged;
            return;
        }

        StartSampleSlider.ValueChanged -= StartSlider_ValueChanged;
        StartSampleSlider.Value = (double)(StartSampleBox?.Value ?? (decimal)viewModel!.LoopPreview.GetTimestampFromSample(viewModel.LoopPreview.StartSample));
        StartSampleSlider.ValueChanged += StartSlider_ValueChanged;

        viewModel!.LoopPreview.StartSample = viewModel.LoopPreview.GetSampleFromTimestamp(StartSampleSlider.Value);
        viewModel.LoopPreviewPlayer.Stop();
    }

    private void EndSampleBox_OnValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
    {
        BgmLoopPropertiesDialogViewModel viewModel = (BgmLoopPropertiesDialogViewModel)DataContext;
        if (StartSampleBox is null || EndSampleBox is null || EndSampleBox is null)
        {
            return;
        }
        if (EndSampleBox.Value < StartSampleBox?.Value)
        {
            EndSampleBox!.ValueChanged -= EndSampleBox_OnValueChanged;
            EndSampleBox.Value = StartSampleBox!.Value;
            EndSampleBox.ValueChanged += EndSampleBox_OnValueChanged;
            return;
        }

        EndSampleSlider.ValueChanged -= EndSlider_ValueChanged;
        EndSampleSlider.Value = (double)(EndSampleBox?.Value ?? (decimal)viewModel!.LoopPreview.GetTimestampFromSample(viewModel.LoopPreview.EndSample))!;
        EndSampleSlider.ValueChanged += EndSlider_ValueChanged;

        viewModel!.LoopPreview.EndSample = viewModel.LoopPreview.GetSampleFromTimestamp(EndSampleSlider.Value);
        viewModel.LoopPreviewPlayer.Stop();
    }
}
