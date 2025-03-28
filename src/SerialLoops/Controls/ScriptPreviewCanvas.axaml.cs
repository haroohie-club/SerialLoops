using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using ReactiveUI;
using SerialLoops.ViewModels.Controls;

namespace SerialLoops.Controls;

public partial class ScriptPreviewCanvas : UserControl
{
    public ScriptPreviewCanvas()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        ((ScriptPreviewCanvasViewModel)DataContext)!.PreviewCanvas = this;
    }

    public void RunNonLoopingAnimations()
    {
        ChibiEmote.Animate();
        Item.Animate();
        TopicFlyout.Animate();
        foreach (ILogical logical in Sprites.GetLogicalChildren())
        {
            if (logical is ContentPresenter presenter)
            {
                AnimatedPositionedSprite sprite = (AnimatedPositionedSprite)presenter.DataContext;
                Animation spriteAnim = new()
                {
                    Duration = sprite!.AnimDuration,
                    Easing = sprite.AnimEasing,
                    Children =
                    {
                        new()
                        {
                            Cue = new(0.0),
                            Setters =
                            {
                                new Setter(Canvas.LeftProperty, sprite.StartXPosition),
                                new Setter(OpacityProperty, sprite.StartOpacity),
                            },
                        },
                        new()
                        {
                            Cue = new(1.0),
                            Setters =
                            {
                                new Setter(Canvas.LeftProperty, sprite.EndXPosition),
                                new Setter(OpacityProperty, sprite.EndOpacity),
                            },
                        },
                    },
                };
                presenter.WhenAnyValue(c => c.Child)
                    .Subscribe(c =>
                    {
                        if (c is AnimatedImage)
                        {
                            spriteAnim.RunAsync(presenter);
                        }
                    });
            }
        }
    }
}

