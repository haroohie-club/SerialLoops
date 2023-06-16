using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Controls;

public class ChibiDirectionSelector : Panel
{
    public event EventHandler DirectionChanged;
    private List<ChibiItem.Direction> _availableDirections;
    public List<ChibiItem.Direction> AvailableDirections
    {
        get => _availableDirections;
        set
        {
            _availableDirections = value;
            UpdateSelected();
        }
    }
    
    private ChibiItem.Direction _direction;
    public ChibiItem.Direction Direction
    {
        get => _direction;
        set
        {
            _direction = value;
            UpdateSelected();
            DirectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    private readonly ILogger _log;
    private TableLayout _grid;
    
    public ChibiDirectionSelector(ChibiItem chibi, string currentPrefix, ILogger log)
    {
        _availableDirections = Enum.GetValues<ChibiItem.Direction>().ToList();
        _log = log;
        InitializeComponent();
        UpdateAvailableDirections(chibi, currentPrefix);
    }
    
    private void InitializeComponent()
    {
        _grid = new(2, 2)
        {
            Padding = 0,
            Spacing = new(2, 2),
            Size = new(72, 72),
        };
        
        int x = 0;
        int y = 0;
        foreach (ChibiItem.Direction direction in Enum.GetValues<ChibiItem.Direction>())
        {
            var button = new Button
            {
                Image = ControlGenerator.GetIcon($"Chibi_{direction}", _log),
                Enabled = AvailableDirections.Contains(direction),
                Tag = direction,
                Size = new(35, 35),
                MinimumSize = new(35, 35),
                ToolTip = direction.ToString(),
            };
            if (Platform.IsGtk)
            {
                button.Width = 0;
            }
            button.Click += (sender, e) => { Direction = direction; };
            _grid.Add(button, x, y);
            
            x++;
            if (x <= 1) continue;
            x = 0;
            y++;
        }
        Content = _grid;
    }

    public void UpdateAvailableDirections(ChibiItem chibi, string prefix)
    {
        _availableDirections.Clear();
        foreach ((string name, ChibiEntry entry) in chibi.ChibiEntries.Where(c => c.Name.StartsWith(prefix)))
        {
            _availableDirections.Add(ChibiItem.CodeToDirection(name[^2..]));
        }
        UpdateSelected();
    }

    private void UpdateSelected()
    {
        _grid.Children
            .Select(b => b as Button).ToList()
            .ForEach(b => b.Enabled = b.Tag.ToString() != Direction.ToString() && AvailableDirections
                .Contains(Enum.Parse<ChibiItem.Direction>(b.Tag.ToString())));
    }
    
}