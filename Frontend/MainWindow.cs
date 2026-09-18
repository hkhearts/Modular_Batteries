using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ModularBatteries.Models;
using ModularBatteries.Services;

namespace ModularBatteries;

public sealed class MainWindow : Window
{
    // Varnam Classic Colors
    static readonly Color ColSand    = Color.Parse("#CE9B61");
    static readonly Color ColOlive   = Color.Parse("#3E4D3E");
    static readonly Color ColDeepRed = Color.Parse("#B2222E");
    static readonly Color ColTeal    = Color.Parse("#206575");
    static readonly Color ColMagenta = Color.Parse("#C13B76");
    static readonly Color ColOrange  = Color.Parse("#E54D35");

    static readonly Color BgClassic  = Color.Parse("#EBEBEB");
    static readonly Color ColText    = Color.Parse("#111111");
    static readonly Color ColDim     = Color.Parse("#555555");

    static readonly (string Name, Color Color, string Icon)[] Modules =
    [
        ("Planner",    ColOrange,  "📋"),
        ("Coding",     ColTeal,    "💻"),
        ("Reasoning",  ColMagenta, "🧠"),
        ("Summarizer", ColOlive,   "📝"),
    ];

    List<DiscoveredModel> _models = [];
    readonly Dictionary<string, string> _connections = new();
    
    // UI Refs
    Canvas _wireCanvas = new();
    readonly Dictionary<string, Border> _modelCards = new();
    readonly Dictionary<string, Ellipse> _moduleJacks = new();
    
    // Drag and Drop state
    string? _draggedModelId;
    Color _dragColor;
    Line? _activeDragLine;

    ChatWindow? _chatWindow;

    public MainWindow()
    {
        Title      = "Modular Batteries - Connector Board";
        Width      = 900;
        Height     = 900;
        Background = new SolidColorBrush(BgClassic);
        FontFamily = new FontFamily("Segoe UI, Arial");

        Content = new TextBlock { Text = "Loading AI Models...", FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        Opened += async (_, _) => await BootAsync();
        Closed += (_, _) => _chatWindow?.Close();
    }

    public string? GetConnectedModel(string moduleName) => _connections.TryGetValue(moduleName, out var id) ? id : null;

    async Task BootAsync()
    {
        _models = [.. await ModelDiscoveryService.DiscoverAllAsync()];
        Content = BuildWireBoard();
        
        // Open the chat window alongside this one
        _chatWindow = new ChatWindow(this);
        _chatWindow.Show();
        
        // Move chat window to the right of the main window
        if (Screens.Primary != null)
        {
            var screenBounds = Screens.Primary.WorkingArea;
            var startX = (screenBounds.Width - (int)Width - (int)_chatWindow.Width) / 2;
            if (startX < 0) startX = 0;
            
            Position = new PixelPoint(startX, Position.Y);
            _chatWindow.Position = new PixelPoint(startX + (int)Width + 20, Position.Y);
        }
    }

    Control BuildWireBoard()
    {
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("350, *, 350"), Margin = new Thickness(30) };

        root.Children.Add(BuildModelPanel());

        _wireCanvas = new Canvas { Background = Brushes.Transparent };
        _wireCanvas.PointerMoved += OnCanvasPointerMoved;
        _wireCanvas.PointerReleased += OnCanvasPointerReleased;
        Grid.SetColumn(_wireCanvas, 1);
        root.Children.Add(_wireCanvas);

        var modulePanel = BuildModulePanel();
        Grid.SetColumn(modulePanel, 2);
        root.Children.Add(modulePanel);

        _wireCanvas.PropertyChanged += (_, e) => { if (e.Property.Name == "Bounds") RedrawWires(); };

        return root;
    }

    Control BuildModelPanel()
    {
        var panel = new StackPanel { Spacing = 15 };
        panel.Children.Add(new TextBlock { Text = "MODELS", FontSize = 24, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(ColDeepRed) });

        foreach (var model in _models)
        {
            var srcColor = model.Source == "Ollama" ? ColTeal : ColMagenta;
            var nameTb = new TextBlock { Text = model.DisplayName, FontSize = 22, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(ColText) };
            var detailTb = new TextBlock { Text = $"{model.Source} {model.Detail}", FontSize = 16, Foreground = new SolidColorBrush(ColDim) };

            var dot = new Ellipse { Width = 20, Height = 20, Fill = new SolidColorBrush(ColSand), VerticalAlignment = VerticalAlignment.Center };
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            row.Children.Add(new StackPanel { Children = { nameTb, detailTb } });
            Grid.SetColumn(dot, 1);
            row.Children.Add(dot);

            var card = new Border
            {
                Background = Brushes.White, BorderBrush = new SolidColorBrush(ColSand), BorderThickness = new Thickness(3),
                Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 10), Cursor = new Cursor(StandardCursorType.Hand),
                Child = row
            };

            _modelCards[model.Id] = card;
            
            card.PointerPressed += (s, e) => 
            {
                _draggedModelId = model.Id;
                _dragColor = srcColor;
                var pt = e.GetPosition(_wireCanvas);
                _activeDragLine = new Line { StartPoint = pt, EndPoint = pt, Stroke = new SolidColorBrush(srcColor), StrokeThickness = 6 };
                _wireCanvas.Children.Add(_activeDragLine);
                e.Pointer.Capture(_wireCanvas);
            };
            panel.Children.Add(card);
        }
        return new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }

    Control BuildModulePanel()
    {
        var panel = new StackPanel { Spacing = 15 };
        panel.Children.Add(new TextBlock { Text = "MODULES", FontSize = 24, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(ColDeepRed) });

        foreach (var (name, color, icon) in Modules)
        {
            var dot = new Ellipse { Width = 24, Height = 24, Fill = new SolidColorBrush(Color.Parse("#CCCCCC")), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0) };
            _moduleJacks[name] = dot;

            var info = new StackPanel { Spacing = 5 };
            info.Children.Add(new TextBlock { Text = $"{icon} {name}", FontSize = 22, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(ColText) });
            var connectedTb = new TextBlock { Text = "— empty —", FontSize = 16, Foreground = new SolidColorBrush(ColDim) };
            info.Children.Add(connectedTb);

            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            row.Children.Add(dot);
            Grid.SetColumn(info, 1);
            row.Children.Add(info);

            var card = new Border
            {
                Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")), BorderThickness = new Thickness(3),
                Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 15),
                Child = row, Tag = name, Name = $"mod_{name}" // used for hit testing
            };

            // Associate the textblock with the name so we can update it
            _moduleJacks[name].Tag = connectedTb;
            panel.Children.Add(card);
        }
        return panel;
    }

    void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_activeDragLine != null)
        {
            _activeDragLine.EndPoint = e.GetPosition(_wireCanvas);
        }
    }

    void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_activeDragLine != null && _draggedModelId != null)
        {
            _wireCanvas.Children.Remove(_activeDragLine);
            e.Pointer.Capture(null);

            // Did we drop on a module?
            var pt = e.GetPosition(this);
            var hit = this.InputHitTest(pt) as Control;
            
            // Walk up the tree to find the module card
            while (hit != null && hit is not Border { Tag: string modName } && hit != this)
            {
                hit = hit.Parent as Control;
            }

            if (hit is Border b && b.Tag is string moduleName)
            {
                ConnectModel(_draggedModelId, moduleName);
            }

            _activeDragLine = null;
            _draggedModelId = null;
        }
    }

    void ConnectModel(string modelId, string moduleName)
    {
        _connections[moduleName] = modelId;
        var model = _models.FirstOrDefault(m => m.Id == modelId);
        
        var color = Modules.FirstOrDefault(m => m.Name == moduleName).Color;
        var dot = _moduleJacks[moduleName];
        dot.Fill = new SolidColorBrush(color);
        
        if (dot.Tag is TextBlock tb)
        {
            tb.Text = $"← {model?.DisplayName}";
            tb.Foreground = new SolidColorBrush(color);
        }

        RedrawWires();
    }

    void RedrawWires()
    {
        _wireCanvas.Children.Clear();

        foreach (var (modName, modColor, _) in Modules)
        {
            if (!_connections.TryGetValue(modName, out var modelId) || 
                !_modelCards.TryGetValue(modelId, out var modelCard) || 
                !_moduleJacks.TryGetValue(modName, out var jackDot)) continue;

            var srcPoint = modelCard.TranslatePoint(new Point(modelCard.Bounds.Width, modelCard.Bounds.Height / 2), _wireCanvas);
            var dstPoint = jackDot.TranslatePoint(new Point(0, jackDot.Bounds.Height / 2), _wireCanvas);
            if (srcPoint is null || dstPoint is null) continue;

            // Straight solid line as requested
            var line = new Line
            {
                StartPoint = srcPoint.Value,
                EndPoint = dstPoint.Value,
                Stroke = new SolidColorBrush(modColor),
                StrokeThickness = 6
            };
            _wireCanvas.Children.Add(line);
        }
    }
}