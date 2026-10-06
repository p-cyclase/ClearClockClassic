using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ClearClock;

/// <summary>なでしこ版 ClearClock.nako の手続きを、対応が追える名称で移植する。</summary>
public partial class MainWindow : Window
{
    private enum PomodoroPhase { Stopped, Work, Break }

    private const int MinimumSize = 100;
    private const int MinimumWidth = 2;
    private readonly DispatcherTimer clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly System.Windows.Forms.NotifyIcon trayIcon;
    private string iniPath = string.Empty;

    // なでしこ版の「サイズ」「ウェイト」「カラー」に対応する状態。
    private int size = 200;
    private int lineWidth = 3;
    private Color color = Colors.Black;
    private Color notificationColor = Colors.Black;
    private int? alarmHour;
    private int? alarmMinute;
    private bool alarmNotifying;
    private PomodoroPhase pomodoroPhase;
    private DateTime pomodoroStartedAt;

    private Color DrawingColor => alarmNotifying || pomodoroPhase == PomodoroPhase.Break ? notificationColor : color;

    public MainWindow()
    {
        InitializeComponent();
        clockTimer.Tick += ClockLoop;
        trayIcon = CreateTrayIcon();
    }

    private System.Windows.Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("表示", null, (_, _) => Dispatcher.BeginInvoke(ShowFromTray));
        menu.Items.Add("終了", null, (_, _) => Dispatcher.BeginInvoke(Close));

        var icon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application,
            Text = "くりくろくらしっく（ClearClockClassic）",
            ContextMenuStrip = menu,
            Visible = true,
        };
        icon.DoubleClick += (_, _) => Dispatcher.BeginInvoke(ShowFromTray);
        return icon;
    }

    // 初期設定（ClearClock.nako: 3-46行）
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        iniPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ClearClock.ini");
        LoadSettings();
        CheckAdjustment();
        DrawFrame();
        ClockLoop(this, EventArgs.Empty);
        clockTimer.Start();
    }

    // 無限ループ（ClearClock.nako: 48-72行）を DispatcherTimer で置き換える。
    private void ClockLoop(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        if (pomodoroPhase == PomodoroPhase.Stopped) CheckAlarm(now);
        else UpdatePomodoro(now);
        DrawFrame();
        var center = size / 2d;
        DrawHand((now.Hour % 12 + now.Minute / 60d) * Math.PI / 6, size * 0.5 / 2, lineWidth, center);
        DrawHand((now.Minute + now.Second / 60d) * Math.PI / 30, size * 0.7 / 2, lineWidth, center);
        if (Sec.IsChecked == true)
            DrawHand(now.Second * Math.PI / 30, size * 0.8 / 2, Math.Max(1, lineWidth - 1), center);
    }

    private void RedrawClock() => ClockLoop(this, EventArgs.Empty);

    // 枠描画（ClearClock.nako: 74-88行）
    private void DrawFrame()
    {
        if (size <= 0) return;
        Width = size;
        Height = size;
        ClockCanvas.Children.Clear();
        var brush = new SolidColorBrush(DrawingColor);
        var halfWidth = Math.Floor(lineWidth / 2d);
        ClockCanvas.Children.Add(new Ellipse
        {
            Width = size - halfWidth * 2,
            Height = size - halfWidth * 2,
            Stroke = brush,
            StrokeThickness = lineWidth,
        });
        Canvas.SetLeft(ClockCanvas.Children.OfType<Ellipse>().Last(), halfWidth);
        Canvas.SetTop(ClockCanvas.Children.OfType<Ellipse>().Last(), halfWidth);

        if (Scale.IsChecked == true)
        {
            // 目盛りは枠線の内側から伸ばす。長さを線幅に依存させると、太線で枠に埋もれるため、
            // 時計の大きさを基準にしつつ、線幅の2倍以上を確保する。
            var center = Math.Floor(size / 2d);
            var tickOuterRadius = center - lineWidth;
            var tickLength = Math.Max(size * 0.05, lineWidth * 2d);
            var tickInnerRadius = Math.Max(0, tickOuterRadius - tickLength);
            for (var count = 0; count < 12; count++)
            {
                var angle = count * Math.PI / 6;
                AddLine(Math.Cos(angle) * tickOuterRadius + center, Math.Sin(angle) * tickOuterRadius + center,
                    Math.Cos(angle) * tickInnerRadius + center, Math.Sin(angle) * tickInnerRadius + center, lineWidth, brush);
            }
        }
    }

    private void DrawHand(double angle, double length, double width, double center)
        => AddLine(center, center, center + Math.Sin(angle) * length, center - Math.Cos(angle) * length, width, new SolidColorBrush(DrawingColor));

    private void AddLine(double x1, double y1, double x2, double y2, double width, Brush brush)
        => ClockCanvas.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = brush, StrokeThickness = width, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });

    // サイズ大・中・小・他（ClearClock.nako: 90-123行）
    private void SizeLarge(object sender, RoutedEventArgs e) { size = 300; CheckAdjustment(); RedrawClock(); }
    private void SizeMedium(object sender, RoutedEventArgs e) { size = 200; CheckAdjustment(); RedrawClock(); }
    private void SizeSmall(object sender, RoutedEventArgs e) { size = 100; CheckAdjustment(); RedrawClock(); }
    private void SizeOther(object sender, RoutedEventArgs e) { if (AskNumber("大きさの変更", "大きさ（100以上）", size, MinimumSize) is int value) size = value; CheckAdjustment(); RedrawClock(); }

    // ウェイト太・中・細・他（ClearClock.nako: 125-158行）
    private void WidthThick(object sender, RoutedEventArgs e) { lineWidth = 5; CheckAdjustment(); RedrawClock(); }
    private void WidthMedium(object sender, RoutedEventArgs e) { lineWidth = 3; CheckAdjustment(); RedrawClock(); }
    private void WidthThin(object sender, RoutedEventArgs e) { lineWidth = 2; CheckAdjustment(); RedrawClock(); }
    private void WidthOther(object sender, RoutedEventArgs e) { if (AskNumber("太さの変更", "太さ（2以上）", lineWidth, MinimumWidth) is int value) lineWidth = value; CheckAdjustment(); RedrawClock(); }

    // 色設定
    private void ColorSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new ColorSettingsWindow(color, notificationColor) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        color = dialog.NormalColor;
        notificationColor = dialog.NotificationColor;
        RedrawClock();
    }

    private void AlarmSet(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        var dialog = new AlarmDialogWindow(alarmHour ?? now.Hour, alarmMinute ?? now.Minute) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        StopPomodoro();
        alarmHour = dialog.Hour;
        alarmMinute = dialog.Minute;
        alarmNotifying = false;
        CheckAdjustment();
        RedrawClock();
    }

    private void PomodoroStartClick(object sender, RoutedEventArgs e)
    {
        DisableAlarm();
        pomodoroStartedAt = DateTime.Now;
        pomodoroPhase = PomodoroPhase.Work;
        PlayNotificationSound("start.wav");
        CheckAdjustment();
        RedrawClock();
    }

    private void TimeFeatureClearClick(object sender, RoutedEventArgs e)
    {
        DisableAlarm();
        StopPomodoro();
        CheckAdjustment();
        RedrawClock();
    }

    // 最前面・秒針・目盛り変更（ClearClock.nako: 168-184行）
    private void FrontChange(object sender, RoutedEventArgs e) { Topmost = Front.IsChecked == true; }
    private void SecondChange(object sender, RoutedEventArgs e) { RedrawClock(); }
    private void ScaleChange(object sender, RoutedEventArgs e) { RedrawClock(); }
    private void AntiAliasChange(object sender, RoutedEventArgs e) { CheckAdjustment(); RedrawClock(); }
    private void Popup_Opened(object sender, RoutedEventArgs e)
    {
        if (alarmNotifying)
        {
            DisableAlarm();
            RedrawClock();
        }
        CheckAdjustment();
    }

    // チェック調整（ClearClock.nako: 186-203行）
    private void CheckAdjustment()
    {
        Topmost = Front.IsChecked == true;
        RenderOptions.SetEdgeMode(ClockCanvas, AntiAlias.IsChecked == true ? EdgeMode.Unspecified : EdgeMode.Aliased);
        ClockCanvas.SnapsToDevicePixels = AntiAlias.IsChecked != true;
        SizeA.IsCheckable = SizeB.IsCheckable = SizeC.IsCheckable = true;
        WidthA.IsCheckable = WidthB.IsCheckable = WidthC.IsCheckable = true;
        SizeA.IsChecked = size == 300; SizeB.IsChecked = size == 200; SizeC.IsChecked = size == 100;
        WidthA.IsChecked = lineWidth == 5; WidthB.IsChecked = lineWidth == 3; WidthC.IsChecked = lineWidth == 2;
        PomodoroStart.IsEnabled = pomodoroPhase == PomodoroPhase.Stopped;
        TimeFeatureClear.IsEnabled = alarmHour.HasValue || pomodoroPhase != PomodoroPhase.Stopped;
    }

    // 終了処理（ClearClock.nako: 205-215行）
    private void HideToTray(object sender, RoutedEventArgs e) => Hide();

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ShowVersionInfo(object sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "不明";
        MessageBox.Show($"くりくろくらしっく（ClearClockClassic）\nバージョン {version}\n\n製作者: p_cyclase\nGitHub: https://github.com/p-cyclase/ClearClockClassic", "バージョン情報", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExitProcess(object sender, RoutedEventArgs e) => Close();
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        clockTimer.Stop();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        if (!string.IsNullOrEmpty(iniPath))
            File.WriteAllLines(iniPath, new[] { size.ToString(CultureInfo.InvariantCulture), lineWidth.ToString(CultureInfo.InvariantCulture), $"#{color.R:X2}{color.G:X2}{color.B:X2}", Flag(Front), Flag(Sec), Flag(Scale), Left.ToString(CultureInfo.InvariantCulture), Top.ToString(CultureInfo.InvariantCulture), Flag(AntiAlias), $"#{notificationColor.R:X2}{notificationColor.G:X2}{notificationColor.B:X2}" });
    }

    private void LoadSettings()
    {
        ResetSettingsToDefaults();
        if (!File.Exists(iniPath)) return;

        string[] values;
        try { values = File.ReadAllLines(iniPath); }
        catch { return; }
        if (values.Length < 6) return;
        size = ReadInt(values, 0, 200, MinimumSize); lineWidth = ReadInt(values, 1, 3, MinimumWidth);
        color = ReadColor(values[2]);
        Front.IsChecked = ReadFlag(values, 3, true); Sec.IsChecked = ReadFlag(values, 4, false); Scale.IsChecked = ReadFlag(values, 5, true);
        if (values.Length > 7 && double.TryParse(values[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var left) && double.TryParse(values[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var top)) { Left = left; Top = top; }
        AntiAlias.IsChecked = ReadFlag(values, 8, false);
        if (values.Length > 11 && TryReadAlarmTime(values[9], values[10], out _, out _)) notificationColor = ReadColor(values[11]);
        else if (values.Length > 9) notificationColor = ReadColor(values[9]);
    }

    private void ResetSettingsToDefaults()
    {
        size = 200;
        lineWidth = 3;
        color = Colors.Black;
        notificationColor = Colors.Black;
        alarmHour = null;
        alarmMinute = null;
        alarmNotifying = false;
        pomodoroPhase = PomodoroPhase.Stopped;
        pomodoroStartedAt = default;
        Front.IsChecked = true;
        Sec.IsChecked = false;
        Scale.IsChecked = true;
        AntiAlias.IsChecked = false;
        Left = double.NaN;
        Top = double.NaN;
    }

    private static int ReadInt(string[] values, int index, int fallback, int minimum)
        => index < values.Length && int.TryParse(values[index], out var number) && number >= minimum ? number : fallback;

    private static bool ReadFlag(string[] values, int index, bool fallback)
        => index < values.Length && int.TryParse(values[index], out var number) && (number == 0 || number == 1) ? number == 1 : fallback;

    private static bool TryReadAlarmTime(string hourText, string minuteText, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;
        return int.TryParse(hourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out hour) && hour is >= 0 and <= 23
            && int.TryParse(minuteText, NumberStyles.Integer, CultureInfo.InvariantCulture, out minute) && minute is >= 0 and <= 59;
    }

    // 旧なでしこ版は COLORREF の整数値（例: 16448）を保存する。
    // 現行版は #RRGGBB を保存するが、どちらも読み込めるようにする。
    private static Color ReadColor(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var legacyColor))
            return Color.FromRgb((byte)(legacyColor & 0xFF), (byte)((legacyColor >> 8) & 0xFF), (byte)((legacyColor >> 16) & 0xFF));

        try { return (Color)ColorConverter.ConvertFromString(value)!; }
        catch { return Colors.Black; }
    }

    private static string Flag(MenuItem item) => item.IsChecked == true ? "1" : "0";
    private void Clock_DragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (alarmNotifying)
        {
            DisableAlarm();
            CheckAdjustment();
            RedrawClock();
        }
        DragMove();
    }

    private void CheckAlarm(DateTime now)
    {
        if (alarmNotifying || alarmHour != now.Hour || alarmMinute != now.Minute) return;
        alarmNotifying = true;
        PlayNotificationSound("alarm.wav");
        ShowClockForNotification();
    }

    private void DisableAlarm()
    {
        alarmHour = null;
        alarmMinute = null;
        alarmNotifying = false;
    }

    private void UpdatePomodoro(DateTime now)
    {
        var elapsedMinutes = (now - pomodoroStartedAt).TotalMinutes;
        var phase = elapsedMinutes % 30d < 25d ? PomodoroPhase.Work : PomodoroPhase.Break;
        if (phase == pomodoroPhase) return;

        pomodoroPhase = phase;
        PlayNotificationSound(phase == PomodoroPhase.Work ? "start.wav" : "goal.wav");
        ShowClockForNotification();
    }

    private void StopPomodoro()
    {
        pomodoroPhase = PomodoroPhase.Stopped;
        pomodoroStartedAt = default;
    }

    private void ShowClockForNotification()
    {
        if (!IsVisible) ShowFromTray();
    }

    private static void PlayNotificationSound(string fileName)
    {
        try
        {
            var soundPath = System.IO.Path.Combine(AppContext.BaseDirectory, "sounds", fileName);
            if (File.Exists(soundPath)) new System.Media.SoundPlayer(soundPath).Play();
        }
        catch { }
    }

    private static int? AskNumber(string title, string label, int current, int minimum)
    {
        var input = new TextBox { Text = current.ToString(CultureInfo.InvariantCulture), Margin = new Thickness(12, 4, 12, 8), MinWidth = 210 };
        var panel = new StackPanel(); panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(12, 12, 12, 0) }); panel.Children.Add(input);
        var dialog = new Window { Title = title, Width = 280, Height = 150, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen, Content = panel };
        var ok = new Button { Content = "OK", IsDefault = true, Width = 72, Margin = new Thickness(0, 0, 12, 12), HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => dialog.DialogResult = true; panel.Children.Add(ok); input.SelectAll();
        return dialog.ShowDialog() == true && int.TryParse(input.Text, out var value) && value >= minimum ? value : null;
    }
}
