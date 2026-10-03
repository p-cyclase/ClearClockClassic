using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ClearClock;

public partial class ColorSettingsWindow : Window
{
    private bool settingInputs;
    private Color normalColor;
    private Color notificationColor;

    public Color NormalColor => normalColor;
    public Color NotificationColor => notificationColor;

    public ColorSettingsWindow(Color initialNormalColor, Color initialNotificationColor)
    {
        InitializeComponent();
        normalColor = initialNormalColor;
        notificationColor = initialNotificationColor;
        SetInputs();
        UpdatePreviews();
        NormalRedInput.SelectAll();
        NormalRedInput.Focus();
    }

    private void NormalColorInputChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (settingInputs) return;
        if (TryGetColor(NormalRedInput.Text, NormalGreenInput.Text, NormalBlueInput.Text, out var selected)) normalColor = selected;
        UpdatePreviews();
        UpdateValidity();
    }

    private void NotificationColorInputChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (settingInputs) return;
        if (TryGetColor(NotificationRedInput.Text, NotificationGreenInput.Text, NotificationBlueInput.Text, out var selected)) notificationColor = selected;
        UpdatePreviews();
        UpdateValidity();
    }

    private void ConfirmColors(object sender, RoutedEventArgs e)
    {
        if (!TryGetColor(NormalRedInput.Text, NormalGreenInput.Text, NormalBlueInput.Text, out normalColor)) return;
        if (!TryGetColor(NotificationRedInput.Text, NotificationGreenInput.Text, NotificationBlueInput.Text, out notificationColor)) return;
        DialogResult = true;
    }

    private void SetInputs()
    {
        settingInputs = true;
        NormalRedInput.Text = normalColor.R.ToString(CultureInfo.InvariantCulture);
        NormalGreenInput.Text = normalColor.G.ToString(CultureInfo.InvariantCulture);
        NormalBlueInput.Text = normalColor.B.ToString(CultureInfo.InvariantCulture);
        NotificationRedInput.Text = notificationColor.R.ToString(CultureInfo.InvariantCulture);
        NotificationGreenInput.Text = notificationColor.G.ToString(CultureInfo.InvariantCulture);
        NotificationBlueInput.Text = notificationColor.B.ToString(CultureInfo.InvariantCulture);
        settingInputs = false;
        UpdateValidity();
    }

    private void UpdatePreviews()
    {
        NormalPreview.Background = new SolidColorBrush(normalColor);
        NotificationPreview.Background = new SolidColorBrush(notificationColor);
    }

    private void UpdateValidity()
    {
        var valid = TryGetColor(NormalRedInput.Text, NormalGreenInput.Text, NormalBlueInput.Text, out _)
            && TryGetColor(NotificationRedInput.Text, NotificationGreenInput.Text, NotificationBlueInput.Text, out _);
        OkButton.IsEnabled = valid;
        InputError.Visibility = valid ? Visibility.Collapsed : Visibility.Visible;
    }

    private static bool TryGetColor(string redText, string greenText, string blueText, out Color selected)
    {
        selected = default;
        if (!TryGetChannel(redText, out var red) || !TryGetChannel(greenText, out var green) || !TryGetChannel(blueText, out var blue)) return false;
        selected = Color.FromRgb(red, green, blue);
        return true;
    }

    private static bool TryGetChannel(string text, out byte channel)
    {
        channel = 0;
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is >= 0 and <= 255 && (channel = (byte)value) == value;
    }
}
