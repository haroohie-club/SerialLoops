using System;
using Avalonia;
using Avalonia.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.Controls;

public partial class ItemLink : UserControl
{
    public static readonly AvaloniaProperty<EditorTabsPanelViewModel> TabsProperty = AvaloniaProperty.Register<ItemLink, EditorTabsPanelViewModel>(nameof(Tabs));
    public static readonly AvaloniaProperty<ItemDescription> ItemProperty = AvaloniaProperty.Register<ItemLink, ItemDescription>(nameof(Item));

    public EditorTabsPanelViewModel Tabs
    {
        get => this.GetValue<EditorTabsPanelViewModel>(TabsProperty);
        set => SetValue(TabsProperty, value);
    }

    public ItemDescription Item
    {
        get => this.GetValue<ItemDescription>(ItemProperty);
        set
        {
            SetValue(ItemProperty, value);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemProperty)
        {
            if (Item is null)
            {
                Link.Text = "NONE";
                Link.Icon = null;
            }
            else
            {
                Link.Text = Item.DisplayName;
                Link.Icon = Item.Type.ToString();
            }
        }
    }

    public ItemLink()
    {
        InitializeComponent();
    }

    public void Link_PointerPressed(object? sender, EventArgs e)
    {
        if (Item is not null)
        {
            Tabs.OpenTab(Item);
        }
    }
}