using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ClearClock;

public partial class AlarmDialogWindow : Window
{
    public int Hour { get; private set; }
    public int Minute { get; private set; }

    public AlarmDialogWindow(int hour, int minute)
    {
        InitializeComponent();
        HourInput.Text = hour.ToString(CultureInfo.InvariantCulture);
        MinuteInput.Text = minute.ToString(CultureInfo.InvariantCulture);
        HourInput.SelectAll();
        HourInput.Focus();
    }

    private void TimeInputChanged(object sender, TextChangedEventArgs e)
    {
        var isValid = TryGetTime(out _, out _);
        OkButton.IsEnabled = isValid;
        InputError.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ConfirmAlarm(object sender, RoutedEventArgs e)
    {
        if (!TryGetTime(out var hour, out var minute)) return;
        Hour = hour;
        Minute = minute;
        DialogResult = true;
    }

    private bool TryGetTime(out int hour, out int minute)
    {
        hour = 0;
        minute = 0;
        return int.TryParse(HourInput.Text, NumberStyles.None, CultureInfo.InvariantCulture, out hour) && hour is >= 0 and <= 23
            && int.TryParse(MinuteInput.Text, NumberStyles.None, CultureInfo.InvariantCulture, out minute) && minute is >= 0 and <= 59;
    }
}
