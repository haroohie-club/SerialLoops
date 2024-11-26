using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SerialLoops.Controls;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Controls;

namespace SerialLoops.Tests.Headless.Controls;

[TestFixture]
public class ScreenSelectorTests
{
    private ScreenSelectorViewModel _vm;
    private ScreenSelector _selector;

    [SetUp]
    public void Setup()
    {
        _vm = new(ScreenScriptParameter.DsScreen.BOTTOM, true);
        _selector = new() { DataContext = _vm };
    }

    [AvaloniaTest]
    public void ScreenSelector_TwoOptionsAlwaysSelectable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_vm.SelectedScreen, Is.EqualTo(ScreenScriptParameter.DsScreen.BOTTOM));
            Assert.That(_selector.BottomButton.IsEnabled, Is.False);
            Assert.That(_selector.TopButton.IsEnabled, Is.True);
            Assert.That(_selector.BothCheckBox.IsChecked, Is.False);
        });

        _vm.SelectTopCommand.Execute(null);
        Assert.Multiple(() =>
        {
            Assert.That(_vm.SelectedScreen, Is.EqualTo(ScreenScriptParameter.DsScreen.TOP));
            Assert.That(_selector.BottomButton.IsEnabled, Is.True);
            Assert.That(_selector.TopButton.IsEnabled, Is.False);
            Assert.That(_selector.BothCheckBox.IsChecked, Is.False);
        });

        _vm.SelectBothCommand.Execute(false);
        Assert.Multiple(() =>
        {
            Assert.That(_vm.SelectedScreen, Is.EqualTo(ScreenScriptParameter.DsScreen.BOTH));
            Assert.That(_selector.BottomButton.IsEnabled, Is.True);
            Assert.That(_selector.TopButton.IsEnabled, Is.True);
            Assert.That(_selector.BothCheckBox.IsChecked, Is.True);
        });

        _vm.SelectBottomCommand.Execute(null);
        Assert.Multiple(() =>
        {
            Assert.That(_vm.SelectedScreen, Is.EqualTo(ScreenScriptParameter.DsScreen.BOTTOM));
            Assert.That(_selector.BottomButton.IsEnabled, Is.False);
            Assert.That(_selector.TopButton.IsEnabled, Is.True);
            Assert.That(_selector.BothCheckBox.IsChecked, Is.False);
        });
    }

    [AvaloniaTest]
    public void ScreenSelector_UncheckingBothSelectsBottom()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_vm.SelectedScreen, Is.EqualTo(ScreenScriptParameter.DsScreen.BOTTOM));
            Assert.That(_selector.BottomButton.IsEnabled, Is.False);
            Assert.That(_selector.TopButton.IsEnabled, Is.True);
            Assert.That(_selector.BothCheckBox.IsChecked, Is.False);
        });

        _vm.SelectBothCommand.Execute(false);
        Assert.Multiple(() =>
        {
            Assert.That(_vm.SelectedScreen, Is.EqualTo(ScreenScriptParameter.DsScreen.BOTH));
            Assert.That(_selector.BottomButton.IsEnabled, Is.True);
            Assert.That(_selector.TopButton.IsEnabled, Is.True);
            Assert.That(_selector.BothCheckBox.IsChecked, Is.True);
        });

        _vm.SelectBothCommand.Execute(true);
        Assert.Multiple(() =>
        {
            Assert.That(_vm.SelectedScreen, Is.EqualTo(ScreenScriptParameter.DsScreen.BOTTOM));
            Assert.That(_selector.BottomButton.IsEnabled, Is.False);
            Assert.That(_selector.TopButton.IsEnabled, Is.True);
            Assert.That(_selector.BothCheckBox.IsChecked, Is.False);
        });

        _vm.SelectTopCommand.Execute(null);
        _vm.SelectBothCommand.Execute(false);
        _vm.SelectBothCommand.Execute(true);
        Assert.Multiple(() =>
        {
            Assert.That(_vm.SelectedScreen, Is.EqualTo(ScreenScriptParameter.DsScreen.BOTTOM));
            Assert.That(_selector.BottomButton.IsEnabled, Is.False);
            Assert.That(_selector.TopButton.IsEnabled, Is.True);
            Assert.That(_selector.BothCheckBox.IsChecked, Is.False);
        });
    }
}
