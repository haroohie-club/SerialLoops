using System;
using System.Linq;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Controls;

public class ChibiDirectionSelector : StackLayout
{
    public event EventHandler OnDirectionChanged;
    
    private ChibiItem.Direction _direction;
    public ChibiItem.Direction Direction
    {
        get => _direction;
        set
        {
            _direction = value;
            OnDirectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    private readonly ILogger _log;
    
    public ChibiDirectionSelector(ILogger log)
    {
        _log = log;
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        TableLayout grid = new(2, 2)
        {
            Padding = 0,
            Spacing = new(0, 0)
        };
        
        int x = 0;
        int y = 0;
        foreach (ChibiItem.Direction direction in Enum.GetValues<ChibiItem.Direction>())
        {
            var button = new Button
            {
                Image = ControlGenerator.GetIcon($"Chibi_{direction}", _log)
            };
            button.Click += (sender, e) =>
            {
                Direction = direction;
                grid.Children.Select(b => b as Button).ToList().ForEach(b => b.Enabled = true);
                button.Enabled = false;
            };
            grid.Add(button, x, y);
            
            x++;
            if (x <= 1) continue;
            x = 0;
            y++;
        }
        
        Content = grid;
    }
    
    
}