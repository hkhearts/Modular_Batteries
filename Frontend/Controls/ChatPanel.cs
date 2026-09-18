using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ModularBatteries.Models;

namespace ModularBatteries.Controls;

/// <summary>
/// Scrollable chat-bubble panel. Renders user messages and AI responses
/// with colour coding per module.
/// </summary>
public sealed class ChatPanel : UserControl
{
    // Module → accent colour
    private static readonly Dictionary<string, Color> ModuleColors = new()
    {
        ["Planner"]    = Color.Parse("#F4B860"),
        ["Coding"]     = Color.Parse("#6FB1F2"),
        ["Reasoning"]  = Color.Parse("#C69BF2"),
        ["Summarizer"] = Color.Parse("#72E0BD"),
        ["System"]     = Color.Parse("#72E0BD"),
        ["user"]       = Color.Parse("#AAAAAA"),
    };

    private static readonly Color UserBg      = Color.Parse("#1E2D3A");
    private static readonly Color AssistantBg = Color.Parse("#14202B");
    private static readonly Color BorderBase  = Color.Parse("#2A3944");

    private readonly StackPanel  _stack;
    private readonly ScrollViewer _scroll;

    public ChatPanel()
    {
        _stack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 4, 0, 4) };
        _scroll = new ScrollViewer
        {
            Content              = _stack,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        };
        Content = _scroll;

        // Welcome system message
        AddSystemMessage("SYSTEM READY",
            "Select a workspace folder, choose a model for each module,\n" +
            "then type a task prompt and click RUN ORCHESTRATOR.\n\n" +
            "The Mojo orchestrator will classify your request and route it to\n" +
            "the appropriate plug (Planner / Coding / Reasoning / Summarizer).");
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public void AddMessage(ChatMessage msg)
    {
        var bubble = msg.Role == "user"
            ? BuildUserBubble(msg)
            : BuildAssistantBubble(msg);
        _stack.Children.Add(bubble);
        _scroll.ScrollToEnd();
    }

    public void Clear()
    {
        _stack.Children.Clear();
        AddSystemMessage("CHAT CLEARED", "Ready for a new task.");
    }

    // -----------------------------------------------------------------------
    // Bubble builders
    // -----------------------------------------------------------------------

    private static Border BuildUserBubble(ChatMessage msg)
    {
        var header = new TextBlock
        {
            Text       = $"YOU  ·  {msg.Timestamp:HH:mm:ss}",
            FontSize   = 10,
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            Margin     = new Thickness(0, 0, 0, 4),
        };
        var body = new TextBlock
        {
            Text        = msg.Text,
            TextWrapping = TextWrapping.Wrap,
            FontSize    = 13,
            Foreground  = Brushes.White,
        };
        return WrapBubble(new StackPanel { Children = { header, body } },
            UserBg, Color.Parse("#2A3944"), HorizontalAlignment.Right);
    }

    private static Border BuildAssistantBubble(ChatMessage msg)
    {
        var moduleColor = ModuleColors.TryGetValue(msg.Module, out var c) ? c : Color.Parse("#72E0BD");

        var badge = new Border
        {
            Background   = new SolidColorBrush(Color.FromArgb(40, moduleColor.R, moduleColor.G, moduleColor.B)),
            CornerRadius = new CornerRadius(3),
            Padding      = new Thickness(6, 2),
            Margin       = new Thickness(0, 0, 8, 0),
            Child        = new TextBlock
            {
                Text       = msg.Module.ToUpperInvariant(),
                FontSize   = 10,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(moduleColor),
            },
        };
        var modelTb = new TextBlock
        {
            Text       = msg.Model,
            FontSize   = 10,
            Foreground = new SolidColorBrush(Color.Parse("#666666")),
        };
        var timeTb = new TextBlock
        {
            Text       = msg.Timestamp.ToString("HH:mm:ss"),
            FontSize   = 10,
            Foreground = new SolidColorBrush(Color.Parse("#555555")),
            Margin     = new Thickness(8, 0, 0, 0),
        };
        var headerRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin      = new Thickness(0, 0, 0, 6),
            Children    = { badge, modelTb, timeTb },
        };
        var body = new TextBlock
        {
            Text         = msg.Text,
            TextWrapping = TextWrapping.Wrap,
            FontSize     = 13,
            Foreground   = Brushes.White,
            LineHeight   = 22,
        };
        return WrapBubble(
            new StackPanel { Children = { headerRow, body } },
            AssistantBg,
            moduleColor,
            HorizontalAlignment.Left);
    }

    private void AddSystemMessage(string title, string body)
    {
        _stack.Children.Add(BuildSystemCard(title, body));
    }

    private static Border BuildSystemCard(string title, string body)
    {
        var tb = new TextBlock { TextWrapping = TextWrapping.Wrap };
        tb.Inlines!.Add(new Avalonia.Controls.Documents.Run(title + "\n")
        {
            Foreground = new SolidColorBrush(Color.Parse("#72E0BD")),
            FontWeight = FontWeight.Bold,
            FontSize   = 11,
        });
        tb.Inlines.Add(new Avalonia.Controls.Documents.Run(body)
        {
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            FontSize   = 12,
        });
        return new Border
        {
            Background      = new SolidColorBrush(Color.Parse("#111B22")),
            BorderBrush     = new SolidColorBrush(Color.Parse("#1E2D3A")),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(5),
            Padding         = new Thickness(14, 10),
            Child           = tb,
        };
    }

    private static Border WrapBubble(Control content, Color bg, Color accent, HorizontalAlignment align)
    {
        var border = new Border
        {
            Background      = new SolidColorBrush(bg),
            BorderBrush     = new SolidColorBrush(Color.FromArgb(80, accent.R, accent.G, accent.B)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(5),
            Padding         = new Thickness(14, 10),
            MaxWidth        = 680,
            HorizontalAlignment = align,
            Child           = content,
        };
        return border;
    }
}
