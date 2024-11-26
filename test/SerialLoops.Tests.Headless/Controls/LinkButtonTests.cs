using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using ReactiveUI;
using SerialLoops.Controls;

namespace SerialLoops.Tests.Headless.Controls;

[TestFixture]
public class LinkButtonTests
{
    [AvaloniaTest]
    public void LinkButton_ClickTriggersAction()
    {
        bool test = false;
        LinkButton linkButton = new()
        {
            Text = "Link Button",
            Command = ReactiveCommand.Create(() =>
            {
                test = true;
            }),
        };

        Window window = new() { Content = new StackPanel { Children = { linkButton, }, } };
        window.Show();
        window.MouseDown(new (30, linkButton.Bounds.Center.Y), MouseButton.Left);
        window.MouseUp(new (30, linkButton.Bounds.Center.Y), MouseButton.Left);
        Assert.That(test, Is.True);
    }
}
