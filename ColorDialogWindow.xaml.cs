using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ClearClock;

public partial class ColorDialogWindow : Window
{
    public Color SelectedColor { get; private set; }

    public ColorDialogWindow(Color initialColor)
    {
        InitializeComponent();
        SelectedColor = initialColor;
        RedInput.Text = initialColor.R.ToString(CultureInfo.InvariantCulture);
        GreenInput.Text = initialColor.G.ToString(CultureInfo.InvariantCulture);
        BlueInput.Text = initialColor.B.ToString(CultureInfo.InvariantCulture);
        RedInput.SelectAll();
        RedInput.Focus();
    }

    private void ColorInputChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (TryGetColor(out var selected))
        {
            ColorPreview.Background = new SolidColorBrush(selected);
            OkButton.IsEnabled = true;
            InputError.Visibility = Visibility.Collapsed;
            return;
        }

        ColorPreview.Background = Brushes.Transparent;
        OkButton.IsEnabled = false;
        InputError.Visibility = Visibility.Visible;
    }

    private void ConfirmColor(object sender, RoutedEventArgs e)
    {
        if (!TryGetColor(out var selected)) return;
        SelectedColor = selected;
        DialogResult = true;
    }

    private bool TryGetColor(out Color selected)
    {
        selected = default;
        if (!TryGetChannel(RedInput.Text, out var red) || !TryGetChannel(GreenInput.Text, out var green) || !TryGetChannel(BlueInput.Text, out var blue))
            return false;

        selected = Color.FromRgb(red, green, blue);
        return true;
    }

    private static bool TryGetChannel(string text, out byte channel)
    {
        channel = 0;
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is >= 0 and <= 255 && (channel = (byte)value) == value;
    }
}
