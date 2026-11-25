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
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, string? operation = null, string[]? successfulStatuses = null)
    {
        Brush Resource(string name, Brush fallback)
            => owner?.TryFindResource(name) as Brush ?? fallback;

        var bg = Resource("BgBrush", new SolidColorBrush(Color.FromRgb(9, 12, 25)));
        var surface = Resource("PanelBrush", new SolidColorBrush(Color.FromRgb(21, 26, 48)));
        var panel = Resource("SidebarBrush", new SolidColorBrush(Color.FromRgb(16, 17, 39)));
        var fg = Resource("FgBrush", Brushes.White);
        var accent = Resource("AccentBrush", new SolidColorBrush(Color.FromRgb(163, 106, 255)));
        var cyan = Resource("SecondaryBrush", new SolidColorBrush(Color.FromRgb(125, 239, 255)));

        int lang=owner is MainWindow window?window.DialogLanguageIndex:0;
        bool removal=operation=="remove";
        bool confirmation=operation!=null && buttons==MessageBoxButton.YesNo;
        bool summary=operation!=null && buttons==MessageBoxButton.OK;
        string action=removal?new[]{"Remove","Rimuovi","Eliminar","Supprimer","Entfernen"}[lang]:
            new[]{"Install","Installa","Instalar","Installer","Installieren"}[lang];
        if(operation!=null){
            string product=caption=="DJ RUSSO FX HUB"?"":caption;
            caption=confirmation?action+(product.Length>0?" "+product:" — "+new[]{"all packages","tutti i pacchetti","todos los paquetes","tous les paquets","alle Pakete"}[lang]):
                (removal?new[]{"Removal report","Esito rimozione","Resultado de eliminación","Résultat de suppression","Entfernungsergebnis"}[lang]:
                new[]{"Installation report","Esito installazione","Resultado de instalación","Résultat d’installation","Installationsergebnis"}[lang])+(product.Length>0?" • "+product:"");
        }
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
            FontSize = operation!=null?19:15,
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

        var row = new Grid
        {
            Margin = new Thickness(24, 24, 14, 20)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
            HorizontalAlignment = HorizontalAlignment.Stretch
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
            Margin = new Thickness(0, 0, 16, 0)
        };
        if(operation!=null){
            var content=new StackPanel{Margin=new Thickness(0,0,16,0)};
            foreach(var line in (message??"").Split('\n')){
                int separator=line.IndexOf(": ",StringComparison.Ordinal);
                var text=new TextBlock{Text=line,Foreground=fg,FontSize=14,TextWrapping=TextWrapping.Wrap,LineHeight=22};
                var item=new Border{Background=surface,CornerRadius=new CornerRadius(8),Padding=new Thickness(12),
                    Margin=new Thickness(0,0,0,7)};
                if(summary && separator>0){
                    var status=line.Substring(separator+2);
                    bool success=successfulStatuses!=null&&Array.Exists(successfulStatuses,x=>x==status);
                    var grid=new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
                    grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
                    text.Text=line.Substring(0,separator);text.FontWeight=FontWeights.SemiBold;
                    grid.Children.Add(text);
                    var statusText=new TextBlock{Text=(success?"✓  ":"")+status,TextWrapping=TextWrapping.Wrap,
                        TextAlignment=TextAlignment.Right,Foreground=success?cyan:fg,FontSize=14,Margin=new Thickness(10,0,0,0)};
                    Grid.SetColumn(statusText,1);grid.Children.Add(statusText);item.Child=grid;
                }else item.Child=text;
                content.Children.Add(item);
            }
            messageScroll.Content=content;
        }
        Grid.SetColumn(messageScroll, 1);
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
                Add(confirmation?new[]{"Cancel","Annulla","Cancelar","Annuler","Abbrechen"}[lang]:"No", MessageBoxResult.No, cancel: true);
                Add(confirmation?action:italian ? "Sì" : "Yes", MessageBoxResult.Yes, primary: true);
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
                Add(summary?new[]{"Close","Chiudi","Cerrar","Fermer","Schließen"}[lang]:"OK", MessageBoxResult.OK, primary: true, cancel: true);
                break;
        }
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);
        outer.Child = root;
        dialog.Content = outer;
        dialog.ShowDialog();
        return result;
    }

    // Dedicated diagnostics presentation; ordinary dialogs are unchanged.
    public static void ShowDiagnostics(Window owner, string report, string caption,
        string[] good, string[] neutral, string[] warning, string[] bad)
    {
        Brush R(string key, Brush fallback) => owner.TryFindResource(key) as Brush ?? fallback;
        var bg=R("BgBrush", Brushes.Black);
        var panel=R("PanelBrush", bg);
        var fg=R("FgBrush", Brushes.White);
        var accent=R("AccentBrush", Brushes.MediumPurple);
        var secondary=R("SecondaryBrush", fg);
        bool light=bg is SolidColorBrush b && (b.Color.R*0.2126+b.Color.G*0.7152+b.Color.B*0.0722)>150;
        if(caption.StartsWith("DJ RUSSO • ",StringComparison.Ordinal))caption=caption.Substring("DJ RUSSO • ".Length);
        var cardSurface=panel;
        if(!light && panel is SolidColorBrush p && bg is SolidColorBrush baseBrush){
            byte Blend(byte x,byte y)=>(byte)(x*0.82+y*0.18);
            cardSurface=new SolidColorBrush(Color.FromRgb(Blend(p.Color.R,baseBrush.Color.R),Blend(p.Color.G,baseBrush.Color.G),Blend(p.Color.B,baseBrush.Color.B)));
        }
        var dialog=new Window {
            Owner=owner, Title=caption, Width=780, MaxWidth=Math.Max(320,owner.ActualWidth-24),
            MaxHeight=Math.Max(300,Math.Min(880,SystemParameters.WorkArea.Height-48)),
            SizeToContent=SizeToContent.Height, WindowStartupLocation=WindowStartupLocation.CenterOwner,
            WindowStyle=WindowStyle.None, AllowsTransparency=true, Background=Brushes.Transparent,
            ResizeMode=ResizeMode.NoResize, ShowInTaskbar=false, Foreground=fg,
            FontFamily=new FontFamily("Segoe UI"), UseLayoutRounding=true, SnapsToDevicePixels=true
        };
        var root=new DockPanel();
        var title=new TextBlock {Text=caption,FontSize=23,FontWeight=FontWeights.SemiBold,
            Foreground=secondary,Margin=new Thickness(12,0,0,0),TextWrapping=TextWrapping.Wrap};
        var header=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(22,16,22,14)};
        header.Children.Add(new Path{Data=Geometry.Parse("M1,10 H5 L8,3 L12,17 L15,10 H20"),Stroke=secondary,
            StrokeThickness=1.8,StrokeLineJoin=PenLineJoin.Round,StrokeStartLineCap=PenLineCap.Round,
            StrokeEndLineCap=PenLineCap.Round,Width=25,Height=25,Stretch=Stretch.Uniform,VerticalAlignment=VerticalAlignment.Center});
        header.Children.Add(title);
        header.MouseLeftButtonDown+=(_,_)=>{try{dialog.DragMove();}catch(InvalidOperationException){}};
        DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        int language=owner is MainWindow mw?mw.DialogLanguageIndex:0;
        var close=MakeButton(new[]{"Close","Chiudi","Cerrar","Fermer","Schließen"}[language],panel,fg,accent);
        close.Width=110;close.Height=40;close.IsDefault=true;close.IsCancel=true;
        close.HorizontalAlignment=HorizontalAlignment.Right;close.Margin=new Thickness(18,10,22,18);
        close.Click+=(_,_)=>dialog.Close();
        DockPanel.SetDock(close,Dock.Bottom);root.Children.Add(close);
        var stack=new StackPanel{Margin=new Thickness(0,0,12,0)};
        var scroll=new ScrollViewer{Content=stack,Margin=new Thickness(20,0,10,0),
            VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,
            MaxHeight=Math.Max(160,dialog.MaxHeight-150)};
        if(owner.TryFindResource(typeof(ScrollBar)) is Style style){
            scroll.Resources[typeof(ScrollBar)]=style;
            scroll.Resources["ScrollRailBrush"]=R("ScrollRailBrush",panel);
            scroll.Resources["ScrollThumbBrush"]=R("ScrollThumbBrush",accent);
        }
        TextBlock Label(string value,Brush brush,double size=14)=>new TextBlock{
            Text=value,Foreground=brush,FontSize=size,TextWrapping=TextWrapping.Wrap,
            VerticalAlignment=VerticalAlignment.Center};
        var blocks=report.Split(new[]{"\n\n"},StringSplitOptions.RemoveEmptyEntries);
        var subtle=new SolidColorBrush(light?Color.FromRgb(210,214,225):Color.FromRgb(54,52,72));
        var success=new SolidColorBrush(light?Color.FromRgb(0,110,62):Color.FromRgb(110,230,166));
        var muted=new SolidColorBrush(light?Color.FromRgb(85,91,110):Color.FromRgb(166,163,188));
        string Status(string line){int i=line.IndexOf(": ",StringComparison.Ordinal);return i<0?"":line.Substring(i+2);}
        bool Is(string[] values,string value)=>Array.Exists(values,x=>string.Equals(x,value,StringComparison.Ordinal));
        // Summary uses only the existing report, never additional filesystem checks.
        if(blocks.Length>=6){
            var components=blocks[1].Split('\n');
            var packages=blocks[2].Split('\n');
            int installed=0,valid=0,attention=0;
            for(int i=1;i<components.Length;i++){
                var value=Status(components[i]);
                if(Is(good,value))installed++;
                if(Is(warning,value)||Is(bad,value))attention++;
            }
            for(int i=1;i<packages.Length;i++)if(Is(good,Status(packages[i])))valid++;
            var summary=new UniformGrid{Columns=3,Margin=new Thickness(0,0,0,14)};
            void Metric(string number,string label,Brush color){
                var content=new StackPanel();
                var count=Label(number,color,30);count.FontWeight=FontWeights.SemiBold;
                content.Children.Add(count);content.Children.Add(Label(label,muted,13));
                summary.Children.Add(new Border{Child=content,Background=cardSurface,CornerRadius=new CornerRadius(10),
                    BorderBrush=subtle,BorderThickness=new Thickness(1),Padding=new Thickness(12,9,12,9),Margin=new Thickness(3,0,3,0)});
            }
            Metric(installed+"/"+(components.Length-1),new[]{"Installed packages","Pacchetti installati","Paquetes instalados","Paquets installés","Installierte Pakete"}[language],secondary);
            Metric(valid+"/"+(packages.Length-1),new[]{"Valid integrity","Integrità valida","Integridad válida","Intégrité valide","Integrität gültig"}[language],valid==packages.Length-1?success:fg);
            Metric(attention.ToString(),new[]{"Component issues","Problemi componenti","Problemas de componentes","Problèmes composants","Komponentenprobleme"}[language],attention==0?success:fg);
            stack.Children.Add(summary);
        }
        foreach(var block in blocks){
            var lines=block.Split('\n');
            var section=new StackPanel();
            foreach(var line in lines){
                int split=line.IndexOf(": ",StringComparison.Ordinal);
                string value=split>=0?line.Substring(split+2):"";
                bool Has(string[] values)=>Array.Exists(values,x=>string.Equals(x,value,StringComparison.Ordinal));
                bool ok=Has(good), idle=Has(neutral), warn=Has(warning), error=Has(bad);
                if(split>=0&&(ok||idle||warn||error)){
                    var row=new Grid{Margin=new Thickness(0,5,0,5)};
                    row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
                    row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
                    var name=line.Substring(0,split);
                    var namePanel=new StackPanel{Orientation=Orientation.Horizontal};
                    var iconFrame=new Border{Width=30,Height=30,CornerRadius=new CornerRadius(7),Background=bg,Margin=new Thickness(0,0,10,0)};
                    string geometry=name.StartsWith("FX SHELL")?"M2,6 L10,2 L18,6 L10,10 Z M2,10 L10,14 L18,10 M2,14 L10,18 L18,14":
                        name.StartsWith("REVERB")?"M2,10 Q6,0 10,10 T18,10":
                        name.StartsWith("DELAY")?"M2,4 V16 M7,7 V13 M12,8 V12 M17,9 V11":
                        name.StartsWith("COMPRESSOR")?"M2,18 L10,10 Q12,8 14,7 L19,5 M10,3 V18":
                        name.StartsWith("METER")?"M3,18 V10 M8,18 V4 M13,18 V8 M18,18 V2":
                        "M10,2 L17,5 V11 Q17,16 10,19 Q3,16 3,11 V5 Z";
                    iconFrame.Child=new Path{Data=Geometry.Parse(geometry),Stroke=secondary,StrokeThickness=1.5,
                        Width=17,Height=17,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
                    namePanel.Children.Add(iconFrame);
                    var names=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
                    int versionAt=name.LastIndexOf(" v",StringComparison.Ordinal);
                    var nameText=Label(versionAt>0?name.Substring(0,versionAt):name,fg);
                    nameText.FontWeight=FontWeights.SemiBold;names.Children.Add(nameText);
                    if(versionAt>0)names.Children.Add(Label(name.Substring(versionAt+1),muted,12));
                    namePanel.Children.Add(names);row.Children.Add(namePanel);
                    var color=ok?new SolidColorBrush(light?Color.FromRgb(0,110,62):Color.FromRgb(110,230,166)):
                        warn?new SolidColorBrush(light?Color.FromRgb(145,83,0):Color.FromRgb(255,205,91)):
                        error?new SolidColorBrush(light?Color.FromRgb(170,25,48):Color.FromRgb(255,119,149)):fg;
                    var displayValue=idle&&name.StartsWith("METER X",StringComparison.Ordinal)&&language==1&&value=="Mancante"?"Non installato":value;
                    var status=Label((ok?"✓  ":warn?"!  ":error?"×  ":"–  ")+displayValue,idle?muted:color,13);
                    status.TextAlignment=TextAlignment.Right;
                    var pill=new Border{Child=status,Background=bg,BorderBrush=subtle,BorderThickness=new Thickness(1),
                        CornerRadius=new CornerRadius(8),Padding=new Thickness(9,4,9,4),Margin=new Thickness(10,0,0,0),
                        HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
                    Grid.SetColumn(pill,1);row.Children.Add(pill);section.Children.Add(row);
                }else{
                    bool heading=lines.Length>1&&line==lines[0]&&split<0;
                    var label=Label(line,heading?secondary:fg,heading?16:13);
                    if(heading)label.FontWeight=FontWeights.SemiBold;
                    label.Margin=new Thickness(0,3,0,heading?9:3);
                    section.Children.Add(label);
                }
            }
            stack.Children.Add(new Border{Child=section,Background=cardSurface,BorderBrush=subtle,
                BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9),
                Padding=new Thickness(14,10,14,10),Margin=new Thickness(0,0,0,9)});
        }
        root.Children.Add(scroll);
        dialog.Content=new Border{Child=root,Background=bg,BorderBrush=accent,
            BorderThickness=new Thickness(1.6),CornerRadius=new CornerRadius(12)};
        dialog.ShowDialog();
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
