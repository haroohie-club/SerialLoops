using System;
using System.Collections.ObjectModel;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;

namespace SerialLoops
{
    public class GraphicSelectionDialog : Dialog<IPreviewableGraphic>
    {
        private readonly ObservableCollection<IPreviewableGraphic> _items;
        private readonly IPreviewableGraphic _currentSelection;
        private readonly Project _project;
        private readonly ILogger _log;
        
        private TextBox _filter;
        private ListBox _selector;
        private Panel _preview;

        public GraphicSelectionDialog(ObservableCollection<IPreviewableGraphic> items, IPreviewableGraphic currentSelection, Project project, ILogger log)
        {
            _items = items;
            _currentSelection = currentSelection;
            _project = project;
            _log = log;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Select Graphic";
            MinimumSize = new Size(450, 400);
            Padding = 10;

            _filter = new TextBox
            {
                PlaceholderText = "Filter by name",
                Width = 150,
            };
            _filter.TextChanged += (sender, args) =>
            {
                _selector.DataStore = new ObservableCollection<IPreviewableGraphic>(_items
                    .Where(i => ((ItemDescription)i).Name.ToLower().Contains(_filter.Text.ToLower())));
            };
            
            _selector = new ListBox
            {
                Size = new Size(150, 390),
                DataStore = _items,
                SelectedIndex = _items.IndexOf(_currentSelection),
                ItemTextBinding = Binding.Delegate<IPreviewableGraphic, string>(i => ((ItemDescription)i).Name),
                ItemKeyBinding = Binding.Delegate<IPreviewableGraphic, string>(i => ((ItemDescription)i).Name),
            };
            
            _preview = new StackLayout
            {
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = 10
            };
            _selector.SelectedValueChanged += (sender, args) =>
            {
                _preview.Content = GeneratePreview();
            };
            
            Button closeButton = new() { Text = "Confirm" };
            closeButton.Click += (sender, args) => Close(_selector.SelectedValue as IPreviewableGraphic);
            Button cancelButton = new() { Text = "Cancel" };
            cancelButton.Click += (sender, args) => Close(_currentSelection);
            
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    new TableLayout(new TableRow
                    {
                        Cells =
                        {
                            new StackLayout
                            {
                                Orientation = Orientation.Vertical,
                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                Spacing = 10,
                                Items =
                                {
                                    _filter,
                                    _selector
                                }
                            },
                            _preview
                        }
                    }),
                    new StackLayout
                    {
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Items =
                        {
                            closeButton,
                            cancelButton
                        }
                    }
                }
            };
        }
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _filter.Focus();
            _preview.Content = GeneratePreview();
        }
        
        private StackLayout GeneratePreview()
        {
            Label backgroundTypeLabel = new();
            if (_selector.SelectedValue is not null)
            {
                if (((ItemDescription)_selector.SelectedValue).Type == ItemDescription.ItemType.Background)
                {
                    backgroundTypeLabel.Text = ((BackgroundItem)_selector.SelectedValue).BackgroundType.ToString();
                }
            }

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Spacing = 10,
                Items =
                {
                    new Label { Text = _selector.SelectedValue == null ? "No preview available" : ((ItemDescription)_selector.SelectedValue).Name },
                    new SKGuiImage(_selector.SelectedValue == null ? new SKBitmap(64, 64) : 
                        ((IPreviewableGraphic) _selector.SelectedValue).GetPreview(_project, 250, 350)),
                    backgroundTypeLabel,
                }
            };
        }
    }   
}