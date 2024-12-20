using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Layout;
using NUnit.Framework;

namespace SerialLoops.Tests.Headless;

public static class Helpers
{
    public static void CaptureAndSaveFrame(this Window window, string artifactsDir, string nameOfTest, ref int currentFrame)
    {
        nameOfTest = nameOfTest.Replace("\"", "").Replace("(", "_").Replace(")", "");
        if (!Directory.Exists(Path.Combine(artifactsDir, nameOfTest)))
        {
            Directory.CreateDirectory(Path.Combine(artifactsDir, nameOfTest));
        }
        string file = Path.Combine(artifactsDir, nameOfTest, $"{currentFrame++:D2}.png");
        window.CaptureRenderedFrame()?.Save(file);
        TestContext.AddTestAttachment(file, $"{currentFrame}");
    }

    public static FuncControlTemplate GetTreeDataGridTemplate()
    {
        return new FuncControlTemplate<TreeDataGrid>((parent, scope) =>
        {
            return new DockPanel
            {
                Children =
                {
                    new TreeDataGridColumnHeadersPresenter
                    {
                        Name = "PART_ColumnHeadersPresenter",
                        [DockPanel.DockProperty] = Dock.Top,
                        [!TreeDataGridColumnHeadersPresenter.ElementFactoryProperty] =
                            parent[!TreeDataGrid.ElementFactoryProperty],
                        [!TreeDataGridColumnHeadersPresenter.ItemsProperty] = parent[!TreeDataGrid.ColumnsProperty],
                    }.RegisterInNameScope(scope),
                    new ScrollViewer
                    {
                        Name = "PART_ScrollViewer",
                        Template = new FuncControlTemplate<ScrollViewer>((x, ns) =>
                            new Grid
                            {
                                ColumnDefinitions =
                                    new()
                                    {
                                        new(1, GridUnitType.Star),
                                        new(GridLength.Auto),
                                    },
                                RowDefinitions =
                                    new()
                                    {
                                        new(1, GridUnitType.Star), new(GridLength.Auto),
                                    },
                                Children =
                                {
                                    new ScrollContentPresenter
                                    {
                                        Name = "PART_ContentPresenter",
                                        [~ContentPresenter.ContentProperty] = x[~ContentControl.ContentProperty],
                                        [~~ScrollContentPresenter.OffsetProperty] =
                                            x[~~ScrollViewer.OffsetProperty],
                                    }.RegisterInNameScope(ns),
                                    new ScrollBar
                                    {
                                        Name = "horizontalScrollBar",
                                        Orientation = Orientation.Horizontal,
                                        [~ScrollBar.VisibilityProperty] =
                                            x[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
                                        [Grid.RowProperty] = 1,
                                    }.RegisterInNameScope(ns),
                                    new ScrollBar
                                    {
                                        Name = "verticalScrollBar",
                                        Orientation = Orientation.Vertical,
                                        [~ScrollBar.VisibilityProperty] =
                                            x[~ScrollViewer.VerticalScrollBarVisibilityProperty],
                                        [Grid.ColumnProperty] = 1,
                                    }.RegisterInNameScope(ns),
                                }
                            }),
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = new TreeDataGridRowsPresenter
                        {
                            Name = "PART_RowsPresenter",
                            [!TreeDataGridRowsPresenter.ColumnsProperty] = parent[!TreeDataGrid.ColumnsProperty],
                            [!TreeDataGridRowsPresenter.ElementFactoryProperty] =
                                parent[!TreeDataGrid.ElementFactoryProperty],
                            [!TreeDataGridRowsPresenter.ItemsProperty] = parent[!TreeDataGrid.RowsProperty],
                        }.RegisterInNameScope(scope),
                    }.RegisterInNameScope(scope),
                }
            };
        });
    }

    public static IControlTemplate GetTreeDataGridRowTemplate()
    {
        return new FuncControlTemplate<TreeDataGridRow>((x, ns) =>
            new DockPanel
            {
                Children =
                {
                    new TreeDataGridCellsPresenter
                    {
                        Name = "PART_CellsPresenter",
                        [!TreeDataGridCellsPresenter.ElementFactoryProperty] = x[!TreeDataGridRow.ElementFactoryProperty],
                        [!TreeDataGridCellsPresenter.ItemsProperty] = x[!TreeDataGridRow.ColumnsProperty],
                        [!TreeDataGridCellsPresenter.RowsProperty] = x[!TreeDataGridRow.RowsProperty],
                    }.RegisterInNameScope(ns),
                }
            });
    }

    public static IControlTemplate TreeDataGridExpanderCellTemplate()
    {
        return new FuncControlTemplate<TreeDataGridExpanderCell>((x, ns) =>
            new DockPanel
            {
                Children =
                {
                    new DockPanel
                    {
                        Children =
                        {
                            new ToggleButton
                            {
                                [!!ToggleButton.IsCheckedProperty] = x[!!TreeDataGridExpanderCell.IsExpandedProperty],
                            },
                            new Decorator
                            {
                                Name = "PART_Content",
                            }.RegisterInNameScope(ns),
                        }
                    },
                }
            });
    }

    public static IControlTemplate GetTreeDataGridTemplateCellTemplate()
    {
        return new FuncControlTemplate<TreeDataGridTemplateCell>((x, ns) =>
            new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentPresenter.ContentProperty] = x[~TreeDataGridTemplateCell.ContentProperty],
                [~ContentPresenter.ContentTemplateProperty] = x[~TreeDataGridTemplateCell.ContentTemplateProperty],
            });
    }
}
