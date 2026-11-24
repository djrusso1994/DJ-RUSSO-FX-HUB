using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace DJRussoFXHub;

// Appearance-only replacement for MessageBox.Show. The original message, icon,
// button set and synchronous MessageBoxResult are preserved at every call site.
internal static class NeonDialog
{
    public static MessageBoxResult Show(string message, string caption = "",
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        => Show(Application.Current?.MainWindow, message, caption, buttons, image);

    public static MessageBoxResult Show(Window? owner, string message, string caption = "",
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        Brush Resource(string name, Brush fallback)
            => owner?.TryFindResource(name) as Brush ?? fallback;

        var bg = Resource("BgBrush", new SolidColorBrush(Color.FromRgb(9, 12, 25)));
        var surface = Resource("PanelBrush", new SolidColorBrush(Color.FromRgb(21, 26, 48)));
        var panel = Resource("SidebarBrush", new SolidColorBrush(Color.FromRgb(16, 17, 39)));
        var fg = Resource("FgBrush", Brushes.White);
        var accent = Resource("AccentBrush", new SolidColorBrush(Color.FromRgb(163, 106, 255)));
        var cyan = Resource("SecondaryBrush", new SolidColorBrush(Color.FromRgb(125, 239, 255)));

        var result = buttons switch
        {
            MessageBoxButton.YesNo => MessageBoxResult.No,
            MessageBoxButton.YesNoCancel => MessageBoxResult.Cancel,
            MessageBoxButton.OKCancel => MessageBoxResult.Cancel,
            _ => MessageBoxResult.OK
        };

        var dialog = new Window
        {
            Title = string.IsNullOrWhiteSpace(caption) ? "DJ RUSSO FX HUB" : caption,
            Width = 630,
            MinWidth = 400,
            MaxWidth = 790,
            SizeToContent = SizeToContent.Height,
            MaxHeight = 650,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Background = Brushes.Transparent,
            Foreground = fg,
            FontFamily = new FontFamily("Segoe UI"),
            UseLayoutRounding = true,
            SnapsToDevicePixels = true
        };
        if (owner != null && owner != dialog && owner.IsLoaded) dialog.Owner = owner;

        // All visible content belongs to the dark/neon surface; no native white dialog chrome.
        var outer = new Border
        {
            BorderBrush = accent,
            BorderThickness = new Thickness(1.6),
            Background = bg,
            CornerRadius = new CornerRadius(12),
            SnapsToDevicePixels = true
        };
        var root = new DockPanel();
        // Clip header and body together to rounded geometry: no square corner seams.
        root.SizeChanged += (_, _) =>
        {
            var width = Math.Max(0, root.ActualWidth);
            var height = Math.Max(0, root.ActualHeight);
            root.Clip = new RectangleGeometry(new Rect(0, 0, width, height), 10.5, 10.5);
        };
        var header = new Grid { Background = panel, Height = 56 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new TextBlock
        {
            Text = dialog.Title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = cyan,
            Margin = new Thickness(21, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Children.Add(title);
        var close = MakeButton("✕", surface, fg, accent);
        close.Width = 40; close.Height = 34; close.Margin = new Thickness(0, 0, 10, 0);
        close.ToolTip = "Close / Chiudi";
        close.Click += (_, _) => dialog.Close();
        Grid.SetColumn(close, 1);
        header.Children.Add(close);
        header.MouseLeftButtonDown += (_, e) =>
        {
            if (e.OriginalSource is DependencyObject origin)
            {
                var node = origin;
                while (node != null && node != header)
                {
                    if (node is ButtonBase) return;
                    node = VisualTreeHelper.GetParent(node);
                }
            }
            try { dialog.DragMove(); } catch (InvalidOperationException) { }
        };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(24, 24, 24, 20)
        };
        string glyphPath = image switch
        {
            MessageBoxImage.Warning => "M10,1 L19,18 H1 Z M10,7 V12 M10,15 V16",
            MessageBoxImage.Error => "M10,1 A9,9 0 1 1 9.99,1 Z M6,6 L14,14 M14,6 L6,14",
            MessageBoxImage.Question => "M10,1 A9,9 0 1 1 9.99,1 Z M7,7 C7,3 14,3 14,8 C14,11 10,10 10,13 M10,16 V17",
            _ => "M10,1 A9,9 0 1 1 9.99,1 Z M10,8 V15 M10,5 V6"
        };
        Brush glyphColor = image switch
        {
            MessageBoxImage.Warning => new SolidColorBrush(Color.FromRgb(255, 205, 91)),
            MessageBoxImage.Error => new SolidColorBrush(Color.FromRgb(255, 119, 149)),
            _ => cyan
        };
        var symbolFrame = new Border
        {
            Width = 55, Height = 55, Background = panel,
            BorderBrush = glyphColor, BorderThickness = new Thickness(1.2),
            CornerRadius = new CornerRadius(13),
            Margin = new Thickness(0, 0, 20, 0),
            VerticalAlignment = VerticalAlignment.Top
        };
        symbolFrame.Child = new Path
        {
            Data = Geometry.Parse(glyphPath),
            Stroke = glyphColor,
            StrokeThickness = 1.7,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent,
            Stretch = Stretch.Uniform,
            Width = 27, Height = 27,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Children.Add(symbolFrame);
        var messageScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 365,
            MaxWidth = 510
        };
        // Reuse the exact main-window scrollbar template and current theme brushes.
        if (owner?.TryFindResource(typeof(ScrollBar)) is Style scrollStyle)
        {
            messageScroll.Resources[typeof(ScrollBar)] = scrollStyle;
            messageScroll.Resources["ScrollRailBrush"] = Resource("ScrollRailBrush", panel);
            messageScroll.Resources["ScrollThumbBrush"] = Resource("ScrollThumbBrush", accent);
        }
        messageScroll.Content = new TextBlock
        {
            Text = message ?? "",
            Foreground = fg,
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 24,
            MinWidth = 330,
            MaxWidth = 490
        };
        row.Children.Add(messageScroll);
        DockPanel.SetDock(row, Dock.Top);
        root.Children.Add(row);

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20, 0, 22, 22)
        };

        void Add(string text, MessageBoxResult value, bool primary = false, bool cancel = false)
        {
            var primaryFg = fg;
            if (accent is SolidColorBrush solidAccent)
            {
                var ac = solidAccent.Color;
                // Dark type on bright lavender, light type on dark accent.
                var luminance = (0.2126 * ac.R + 0.7152 * ac.G + 0.0722 * ac.B);
                primaryFg = luminance >= 128
                    ? new SolidColorBrush(Color.FromRgb(17, 20, 39))
                    : Brushes.White;
            }
            var button = MakeButton(text, primary ? accent : panel, primary ? primaryFg : fg, primary ? cyan : accent);
            button.MinWidth = 108; button.Height = 41;
            button.Margin = new Thickness(8, 0, 0, 0);
            button.IsDefault = primary;
            button.IsCancel = cancel;
            button.Click += (_, _) => { result = value; dialog.Close(); };
            footer.Children.Add(button);
        }
        var italian = false;
        if (owner is MainWindow mw)
        {
            // Use currently selected interface language without changing any dialog messages.
            italian = mw.DialogLanguageIndex == 1;
        }
        switch (buttons)
        {
            case MessageBoxButton.YesNo:
                Add(italian ? "No" : "No", MessageBoxResult.No, cancel: true);
                Add(italian ? "Sì" : "Yes", MessageBoxResult.Yes, primary: true);
                break;
            case MessageBoxButton.YesNoCancel:
                Add(italian ? "Annulla" : "Cancel", MessageBoxResult.Cancel, cancel: true);
                Add("No", MessageBoxResult.No);
                Add(italian ? "Sì" : "Yes", MessageBoxResult.Yes, primary: true);
                break;
            case MessageBoxButton.OKCancel:
                Add(italian ? "Annulla" : "Cancel", MessageBoxResult.Cancel, cancel: true);
                Add("OK", MessageBoxResult.OK, primary: true);
                break;
            default:
                Add("OK", MessageBoxResult.OK, primary: true, cancel: true);
                break;
        }
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);
        outer.Child = root;
        dialog.Content = outer;
        dialog.ShowDialog();
        return result;
    }

    private static Button MakeButton(string label, Brush background, Brush foreground, Brush border)
    {
        var button = new Button
        {
            Content = label,
            Background = background,
            Foreground = foreground,
            BorderBrush = border,
            BorderThickness = new Thickness(1.5),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Segoe UI"),
            Cursor = Cursors.Hand,
            Padding = new Thickness(12, 6, 12, 6)
        };
        var style = new Style(typeof(Button));
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        factory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        factory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(content);
        style.Setters.Add(new Setter(Button.TemplateProperty, new ControlTemplate(typeof(Button)) { VisualTree = factory }));
        button.Style = style;

        // Gentle spring on hover; always returns to Y=0. No delayed click or sticky displacement.
        var translation = new TranslateTransform();
        button.RenderTransform = translation;
        var neon = border is SolidColorBrush solid ? solid.Color : Color.FromRgb(90, 232, 255);
        var glow = new DropShadowEffect
        {
            Color = neon, BlurRadius = 17, ShadowDepth = 0, Opacity = 0.08
        };
        button.Effect = glow;
        void Glow(bool active)
        {
            glow.BeginAnimation(DropShadowEffect.OpacityProperty,
                new DoubleAnimation(active ? 0.82 : 0.08, TimeSpan.FromMilliseconds(165)));
            glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
                new DoubleAnimation(active ? 19 : 11, TimeSpan.FromMilliseconds(165)));
        }
        button.MouseEnter += (_, _) =>
        {
            Glow(true);
            var spring = new DoubleAnimationUsingKeyFrames();
            spring.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            spring.KeyFrames.Add(new EasingDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110))));
            spring.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(215))));
            spring.KeyFrames.Add(new EasingDoubleKeyFrame(-1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
            spring.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(390))));
            translation.BeginAnimation(TranslateTransform.YProperty, spring, HandoffBehavior.SnapshotAndReplace);
        };
        button.MouseLeave += (_, _) =>
        {
            Glow(false);
            translation.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(0, TimeSpan.FromMilliseconds(140)),
                HandoffBehavior.SnapshotAndReplace);
        };
        button.PreviewMouseLeftButtonDown += (_, _) =>
        {
            translation.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(1.5, TimeSpan.FromMilliseconds(70)),
                HandoffBehavior.SnapshotAndReplace);
        };
        button.PreviewMouseLeftButtonUp += (_, _) =>
        {
            translation.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(0, TimeSpan.FromMilliseconds(110)),
                HandoffBehavior.SnapshotAndReplace);
        };
        button.GotKeyboardFocus += (_, _) => Glow(true);
        button.LostKeyboardFocus += (_, _) => { if (!button.IsMouseOver) Glow(false); };
        return button;
    }
}
