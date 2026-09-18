using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace ModularBatteries.Controls;

/// <summary>
/// Interactive node-graph canvas showing four AI module nodes wired to an
/// Orchestrator hub node with animated coloured connections.
/// </summary>
public sealed class NodeGraphCanvas : Canvas
{
    // -----------------------------------------------------------------------
    // Module descriptor
    // -----------------------------------------------------------------------

    public sealed record ModuleNode(
        string Name,
        string Badge,
        Color Color,
        double X,
        double Y
    );

    // -----------------------------------------------------------------------
    // Events / Bindable
    // -----------------------------------------------------------------------

    // Unused event removed to fix compiler warning

    // -----------------------------------------------------------------------
    // Layout constants
    // -----------------------------------------------------------------------

    private const double NodeW          = 255;
    private const double NodeH          = 84;
    private const double OrchestratorX  = 440;
    private const double OrchestratorY  = 160;
    private const double OrchestratorW  = 160;
    private const double OrchestratorH  = 80;
    private const double ConnectorR     = 6;

    // Module definitions (colour, position)
    private static readonly ModuleNode[] Modules =
    [
        new("Planner",    "PLAN",      Color.Parse("#F4B860"),  20, 20),
        new("Coding",     "BUILD",     Color.Parse("#6FB1F2"),  20, 130),
        new("Reasoning",  "THINK",     Color.Parse("#C69BF2"),  20, 240),
        new("Summarizer", "SUMMARIZE", Color.Parse("#72E0BD"),  20, 350),
    ];

    private static readonly Color OrchestratorColor = Color.Parse("#72E0BD");
    private static readonly Color BgColor            = Color.Parse("#0F1923");
    private static readonly Color PanelColor         = Color.Parse("#17212A");
    private static readonly Color GridColor          = Color.Parse("#1A2530");

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------

    private readonly Dictionary<string, ComboBox>  _selectors  = new();
    private readonly Dictionary<string, Line>      _wires      = new();
    private readonly Dictionary<string, Ellipse>   _connectors = new();
    private string?                                _activeModule;
    private DispatcherTimer?                       _animTimer;
    private double                                 _animPhase;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public NodeGraphCanvas()
    {
        Background = new SolidColorBrush(BgColor);
        Height = 460;

        // Build static layers once layout is ready
        LayoutUpdated += (_, _) => { if (Children.Count == 0) Build(); };
    }

    // -----------------------------------------------------------------------
    // Public: update model selectors
    // -----------------------------------------------------------------------

    public void SetModels(IReadOnlyList<string> modelIds)
    {
        foreach (var (name, cb) in _selectors)
        {
            cb.ItemsSource = modelIds;
            cb.SelectedIndex = 0;
        }
    }

    public string? GetSelectedModel(string module) =>
        _selectors.TryGetValue(module, out var cb) ? cb.SelectedItem as string : null;

    // -----------------------------------------------------------------------
    // Public: trigger wire animation for a module
    // -----------------------------------------------------------------------

    public void Activate(string module)
    {
        _activeModule = module;
        _animPhase    = 0;
        _animTimer?.Stop();
        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        int ticks = 0;
        _animTimer.Tick += (_, _) =>
        {
            _animPhase += 0.15;
            AnimateWires(module);
            if (++ticks > 60) { _animTimer.Stop(); ResetWires(); }
        };
        _animTimer.Start();
    }

    // -----------------------------------------------------------------------
    // Graph construction
    // -----------------------------------------------------------------------

    private void Build()
    {
        Children.Clear();

        DrawGrid();

        // Hub — Orchestrator node
        DrawOrchestratorHub();

        // Four module nodes + wires
        foreach (var mod in Modules)
        {
            DrawWire(mod);
            DrawModuleNode(mod);
        }
    }

    private void DrawGrid()
    {
        double w = Bounds.Width  > 0 ? Bounds.Width  : 700;
        double h = Bounds.Height > 0 ? Bounds.Height : 460;
        var brush = new SolidColorBrush(GridColor);
        const double step = 28;
        for (double x = 0; x < w; x += step)
            Children.Add(new Line { StartPoint = new Point(x, 0), EndPoint = new Point(x, h), Stroke = brush, StrokeThickness = 0.5 });
        for (double y = 0; y < h; y += step)
            Children.Add(new Line { StartPoint = new Point(0, y), EndPoint = new Point(w, y), Stroke = brush, StrokeThickness = 0.5 });
    }

    private void DrawOrchestratorHub()
    {
        // Glow border
        var glow = new Border
        {
            Width           = OrchestratorW + 4,
            Height          = OrchestratorH + 4,
            CornerRadius    = new CornerRadius(6),
            Background      = new SolidColorBrush(Color.FromArgb(60, 114, 224, 189)),
        };
        SetLeft(glow, OrchestratorX - 2);
        SetTop(glow,  OrchestratorY - 2);
        Children.Add(glow);

        // Main hub
        var hub = new Border
        {
            Width           = OrchestratorW,
            Height          = OrchestratorH,
            CornerRadius    = new CornerRadius(5),
            Background      = new SolidColorBrush(Color.Parse("#172A28")),
            BorderBrush     = new SolidColorBrush(OrchestratorColor),
            BorderThickness = new Thickness(1.5),
            Padding         = new Thickness(10),
            Child           = new TextBlock
            {
                Text            = "⬡  ORCHESTRATOR\n   ROUTER",
                Foreground      = new SolidColorBrush(OrchestratorColor),
                TextAlignment   = TextAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize        = 11,
                FontWeight      = FontWeight.Bold,
                LetterSpacing   = 0.8,
            }
        };
        SetLeft(hub, OrchestratorX);
        SetTop(hub,  OrchestratorY);
        Children.Add(hub);

        // Input port (left side of hub)
        var inPort = MakeDot(OrchestratorColor, 10);
        SetLeft(inPort, OrchestratorX - 5);
        SetTop(inPort,  OrchestratorY + OrchestratorH / 2.0 - 5);
        Children.Add(inPort);
    }

    private void DrawModuleNode(ModuleNode mod)
    {
        // Glow backdrop
        var glow = new Border
        {
            Width  = NodeW + 4,
            Height = NodeH + 4,
            CornerRadius = new CornerRadius(6),
            Background   = new SolidColorBrush(Color.FromArgb(30, mod.Color.R, mod.Color.G, mod.Color.B)),
        };
        SetLeft(glow, mod.X - 2);
        SetTop(glow,  mod.Y - 2);
        Children.Add(glow);

        // Node card
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(new TextBlock
        {
            Text        = mod.Badge,
            Foreground  = new SolidColorBrush(mod.Color),
            FontSize    = 10,
            FontWeight  = FontWeight.Bold,
            LetterSpacing = 1.4,
        });

        var selector = new ComboBox
        {
            ItemsSource     = new[] { "local/auto" },
            SelectedIndex   = 0,
            Height          = 30,
            FontSize        = 12,
        };
        _selectors[mod.Name] = selector;
        stack.Children.Add(selector);

        var node = new Border
        {
            Width           = NodeW,
            Height          = NodeH,
            CornerRadius    = new CornerRadius(5),
            Background      = new SolidColorBrush(PanelColor),
            BorderBrush     = new SolidColorBrush(mod.Color),
            BorderThickness = new Thickness(1.5),
            Padding         = new Thickness(12),
            Child           = stack,
        };
        SetLeft(node, mod.X);
        SetTop(node,  mod.Y);
        Children.Add(node);

        // Name label below
        var label = new TextBlock
        {
            Text       = mod.Name.ToUpperInvariant(),
            Foreground = new SolidColorBrush(mod.Color),
            FontSize   = 9,
            Opacity    = 0.7,
        };
        SetLeft(label, mod.X + 4);
        SetTop(label,  mod.Y + NodeH + 2);
        Children.Add(label);

        // Output connector dot
        var dot = MakeDot(mod.Color, ConnectorR * 2);
        double cx = mod.X + NodeW - ConnectorR;
        double cy = mod.Y + NodeH / 2.0 - ConnectorR;
        SetLeft(dot, cx);
        SetTop(dot,  cy);
        _connectors[mod.Name] = dot;
        Children.Add(dot);
    }

    private void DrawWire(ModuleNode mod)
    {
        double startX = mod.X + NodeW + ConnectorR;
        double startY = mod.Y + NodeH / 2.0;
        double endX   = OrchestratorX;
        double endY   = OrchestratorY + OrchestratorH / 2.0;

        // Bezier-style elbow wire using two Lines + corner
        double midX = (startX + endX) / 2.0;

        var seg1 = new Line
        {
            StartPoint      = new Point(startX, startY),
            EndPoint        = new Point(midX, startY),
            Stroke          = new SolidColorBrush(Color.FromArgb(100, mod.Color.R, mod.Color.G, mod.Color.B)),
            StrokeThickness = 2,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 6, 4 },
        };
        var seg2 = new Line
        {
            StartPoint      = new Point(midX, startY),
            EndPoint        = new Point(midX, endY),
            Stroke          = new SolidColorBrush(Color.FromArgb(100, mod.Color.R, mod.Color.G, mod.Color.B)),
            StrokeThickness = 2,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 6, 4 },
        };
        var seg3 = new Line
        {
            StartPoint      = new Point(midX, endY),
            EndPoint        = new Point(endX, endY),
            Stroke          = new SolidColorBrush(Color.FromArgb(100, mod.Color.R, mod.Color.G, mod.Color.B)),
            StrokeThickness = 2,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 6, 4 },
        };

        // Insert wires at bottom of z-order (index 0 after grid)
        Children.Insert(0, seg3);
        Children.Insert(0, seg2);
        Children.Insert(0, seg1);
        _wires[$"{mod.Name}_1"] = seg1;
        _wires[$"{mod.Name}_2"] = seg2;
        _wires[$"{mod.Name}_3"] = seg3;
    }

    // -----------------------------------------------------------------------
    // Animation helpers
    // -----------------------------------------------------------------------

    private void AnimateWires(string module)
    {
        var mod = Modules.FirstOrDefault(m => m.Name == module);
        if (mod is null) return;

        double t   = (Math.Sin(_animPhase) + 1) / 2.0;   // 0–1 pulsing
        byte  alpha = (byte)(120 + (int)(t * 135));        // 120–255
        var   color = Color.FromArgb(alpha, mod.Color.R, mod.Color.G, mod.Color.B);
        var   brush = new SolidColorBrush(color);

        foreach (var suffix in new[] { "_1", "_2", "_3" })
        {
            if (_wires.TryGetValue(module + suffix, out var wire))
            {
                wire.Stroke          = brush;
                wire.StrokeThickness = 2.5;
                wire.StrokeDashOffset = _animPhase * 4;
            }
        }
    }

    private void ResetWires()
    {
        foreach (var mod in Modules)
        {
            var dimBrush = new SolidColorBrush(Color.FromArgb(100, mod.Color.R, mod.Color.G, mod.Color.B));
            foreach (var suffix in new[] { "_1", "_2", "_3" })
            {
                if (_wires.TryGetValue(mod.Name + suffix, out var wire))
                {
                    wire.Stroke          = dimBrush;
                    wire.StrokeThickness = 2;
                    wire.StrokeDashOffset = 0;
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // Utilities
    // -----------------------------------------------------------------------

    private static Ellipse MakeDot(Color color, double size) => new()
    {
        Width  = size,
        Height = size,
        Fill   = new SolidColorBrush(color),
    };
}
