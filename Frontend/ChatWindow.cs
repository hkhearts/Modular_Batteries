using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ModularBatteries.Models;
using ModularBatteries.Services;

namespace ModularBatteries;

public sealed class ChatWindow : Window
{
    // Varnam Classic Colors
    static readonly Color ColSand    = Color.Parse("#CE9B61");
    static readonly Color ColOlive   = Color.Parse("#3E4D3E");
    static readonly Color ColDeepRed = Color.Parse("#B2222E");
    static readonly Color ColTeal    = Color.Parse("#206575");
    static readonly Color ColMagenta = Color.Parse("#C13B76");
    static readonly Color ColOrange  = Color.Parse("#E54D35");

    static readonly Color BgClassic  = Color.Parse("#F5F5F0"); // Light warm gray
    static readonly Color ColText    = Color.Parse("#222222");

    StackPanel _chatStack    = new();
    ScrollViewer _chatScroll = new();
    TextBox _promptBox       = new();
    TextBlock _wsLabel       = new();
    string _workspacePath    = "";

    readonly MainWindow _wireBoard;

    public ChatWindow(MainWindow wireBoard)
    {
        _wireBoard = wireBoard;

        Title      = "Modular Batteries - Workspace & Chat";
        Width      = 800;
        Height     = 900;
        Background = new SolidColorBrush(BgClassic);
        FontFamily = new FontFamily("Segoe UI, Arial");

        Content = BuildUI();
    }

    Control BuildUI()
    {
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto, *, Auto") };

        // Header
        var header = new Border { Background = new SolidColorBrush(ColOlive), Padding = new Thickness(20) };
        header.Child = new TextBlock 
        { 
            Text = "CHAT WORKSPACE", 
            FontSize = 28, 
            FontWeight = FontWeight.Bold, 
            Foreground = Brushes.White,
            LetterSpacing = 2
        };
        root.Children.Add(header);

        // Chat Body
        _chatStack = new StackPanel { Spacing = 20, Margin = new Thickness(30) };
        _chatScroll = new ScrollViewer { Content = _chatStack, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Grid.SetRow(_chatScroll, 1);
        root.Children.Add(_chatScroll);

        AddSystemMessage("READY", "The AI modules are listening. Choose a workspace folder if you want the AI to write files directly to your disk.");

        // Footer
        var footer = new Border { Background = new SolidColorBrush(Color.Parse("#EAEAEA")), BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")), BorderThickness = new Thickness(0, 2, 0, 0), Padding = new Thickness(30) };
        
        var stack = new StackPanel { Spacing = 15 };

        // Workspace Picker
        var wsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 15 };
        var wsBtn = new Button 
        { 
            Content = "Select Workspace Folder", 
            FontSize = 18, 
            Background = new SolidColorBrush(ColSand), 
            Foreground = Brushes.White,
            Padding = new Thickness(15, 8),
            CornerRadius = new CornerRadius(0) // Classic sharp corners
        };
        _wsLabel = new TextBlock { Text = "No folder selected", FontSize = 18, Foreground = new SolidColorBrush(Color.Parse("#555555")), VerticalAlignment = VerticalAlignment.Center };
        
        wsBtn.Click += async (_, _) =>
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select workspace" });
            if (folders.Count > 0) { _workspacePath = folders[0].Path.LocalPath; _wsLabel.Text = _workspacePath; }
        };
        wsRow.Children.Add(wsBtn);
        wsRow.Children.Add(_wsLabel);
        stack.Children.Add(wsRow);

        // Input Box
        _promptBox = new TextBox 
        { 
            AcceptsReturn = true, 
            MinHeight = 120, 
            MaxHeight = 300, 
            Watermark = "Type your request (e.g. 'Build a python script for...')", 
            FontSize = 22,
            Padding = new Thickness(15),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(Color.Parse("#999999")),
            CornerRadius = new CornerRadius(0)
        };
        stack.Children.Add(_promptBox);

        // Run Button
        var runBtn = new Button 
        { 
            Content = "RUN TASK", 
            FontSize = 24, 
            FontWeight = FontWeight.Bold, 
            Background = new SolidColorBrush(ColDeepRed), 
            Foreground = Brushes.White,
            Padding = new Thickness(30, 15),
            CornerRadius = new CornerRadius(0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        runBtn.Click += async (_, _) => await RunPromptAsync(runBtn);
        stack.Children.Add(runBtn);

        footer.Child = stack;
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        return root;
    }

    bool _waiting = false;
    Border? _waitBubble;
    DispatcherTimer? _waitTimer;
    int _waitFrame = 0;

    async Task RunPromptAsync(Button runBtn)
    {
        var prompt = _promptBox.Text?.Trim();
        if (string.IsNullOrEmpty(prompt) || _waiting) return;

        AddUserBubble(prompt);
        _promptBox.Text = "";
        _waiting = true;
        runBtn.IsEnabled = false;

        string currentPrompt = prompt;

        // 4-Stage Pipeline: Planner -> Reasoning -> Coding -> Summarizer
        string[] pipeline = ["Planner", "Reasoning", "Coding", "Summarizer"];

        foreach (var module in pipeline)
        {
            var modelId = _wireBoard.GetConnectedModel(module) ?? "local/auto";
            var color   = GetModuleColor(module);

            _waitBubble = AddWaitBubble(module, modelId);
            _chatScroll.ScrollToEnd();

            var response = await OrchestratorService.RunAsync(module, modelId, currentPrompt, _workspacePath);

            _waitTimer?.Stop();
            if (_waitBubble != null) _chatStack.Children.Remove(_waitBubble);
            
            AddAssistantBubble(module, modelId, response, color);
            _chatScroll.ScrollToEnd();

            // Feed output of this stage into the next stage as context
            currentPrompt = $"Previous Stage ({module}) Output:\n{response}\n\nOriginal Task: {prompt}";
        }

        _waiting = false;
        runBtn.IsEnabled = true;
        _chatScroll.ScrollToEnd();
    }

    void AddUserBubble(string text)
    {
        var border = new Border 
        { 
            Background = new SolidColorBrush(Color.Parse("#E0E8F0")), 
            BorderBrush = new SolidColorBrush(Color.Parse("#B0C4DE")), 
            BorderThickness = new Thickness(2), 
            Padding = new Thickness(20), 
            MaxWidth = 650, 
            HorizontalAlignment = HorizontalAlignment.Right 
        };
        border.Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 20, Foreground = new SolidColorBrush(ColText) };
        _chatStack.Children.Add(border);
    }

    Border AddWaitBubble(string module, string modelName)
    {
        var dots = new TextBlock { Text = "Thinking...", FontSize = 20, Foreground = new SolidColorBrush(Color.Parse("#666666")) };
        var border = new Border { Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")), BorderThickness = new Thickness(2), Padding = new Thickness(20), MaxWidth = 650, HorizontalAlignment = HorizontalAlignment.Left, Child = dots };
        _chatStack.Children.Add(border);
        _waitFrame = 0;
        _waitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _waitTimer.Tick += (_, _) => { _waitFrame++; dots.Text = $"Routing {module} to {modelName}{new string('.', _waitFrame % 4 + 1)}"; };
        _waitTimer.Start();
        return border;
    }

    void AddAssistantBubble(string module, string modelId, string text, Color color)
    {
        var header = new TextBlock { Text = $"{module.ToUpperInvariant()} via {modelId}", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(color), Margin = new Thickness(0, 0, 0, 10) };
        var body = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 20, Foreground = new SolidColorBrush(ColText), LineHeight = 32 };
        var inner = new StackPanel(); inner.Children.Add(header); inner.Children.Add(body);
        _chatStack.Children.Add(new Border { Background = Brushes.White, BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(3), Padding = new Thickness(20), MaxWidth = 700, HorizontalAlignment = HorizontalAlignment.Left, Child = inner });
    }

    void AddSystemMessage(string title, string body)
    {
        var inner = new StackPanel { Spacing = 8 };
        inner.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(ColTeal) });
        inner.Children.Add(new TextBlock { Text = body, FontSize = 18, Foreground = new SolidColorBrush(Color.Parse("#555555")), TextWrapping = TextWrapping.Wrap });
        _chatStack.Children.Add(new Border { Background = new SolidColorBrush(Color.Parse("#E6F2F5")), BorderBrush = new SolidColorBrush(ColTeal), BorderThickness = new Thickness(2), Padding = new Thickness(20), Child = inner });
    }

    static string GuessModule(string prompt)
    {
        var lower = prompt.ToLowerInvariant();
        if (lower.Contains("code") || lower.Contains("build") || lower.Contains("fix") || lower.Contains("script") || lower.Contains("app")) return "Coding";
        if (lower.Contains("summar") || lower.Contains("tldr")) return "Summarizer";
        if (lower.Contains("why") || lower.Contains("reason")) return "Reasoning";
        return "Planner";
    }

    static Color GetModuleColor(string module) => module switch
    {
        "Planner" => ColOrange,
        "Coding" => ColTeal,
        "Reasoning" => ColMagenta,
        "Summarizer" => ColOlive,
        _ => ColSand
    };
}
