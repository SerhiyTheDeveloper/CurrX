using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CurrX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SetupComponents();
        }

        private void SetupComponents()
        {
            currencyCombo.Items.Add(new CurrencyItem { Name = "Долар США", Code = "USD" });
            currencyCombo.Items.Add(new CurrencyItem { Name = "Євро", Code = "EUR" });
            currencyCombo.Items.Add(new CurrencyItem { Name = "Злотий", Code = "PLN" });
            currencyCombo.Items.Add(new CurrencyItem { Name = "Фунт стерлінгів", Code = "GBP" });
            currencyCombo.Items.Add(new CurrencyItem { Name = "Чеська крона", Code = "CZK" });
            currencyCombo.SelectedIndex = 0;
        }
        private void SetStatus(string text, bool isError = false)
        {
            statusLabel.Text = text;
            statusBorder.Background = (Brush)FindResource(isError ? "DangerSoftBrush" : "AccentSoftBrush");
            statusLabel.Foreground = (Brush)FindResource(isError ? "DangerBrush" : "AccentDarkBrush");
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) { }
        private async void loadButton_Click(object sender, RoutedEventArgs e)
        {
            loadButton.IsEnabled = false;
            SetStatus("Завантаження...");

            try
            {
                if (currencyCombo.SelectedItem is CurrencyItem selectedCurrency)
                {
                    string url = $"https://bank.gov.ua/NBUStatService/v1/statdirectory/exchangenew?json&valcode={selectedCurrency.Code}";
                    RateEntry rateEntry = await RateService.LoadRateAsync(url);
                    nameLabel.Text = rateEntry.Currency;
                    rateLabel.Text = rateEntry.Rate.ToString();
                    codeLabel.Text = rateEntry.Code;
                    dateLabel.Text = rateEntry.ExchangeDate;
                    SetStatus("Завантаження завершено.");
                }
            }
            catch (Exception)
            {
                SetStatus("Не вдалося отримати дані. Перевірте інтернет і спробуйте ще раз.", isError: true);
            }
            finally
            {
                loadButton.IsEnabled = true;
            }
        }
        private void saveButton_Click(object sender, RoutedEventArgs e) { }
        private void openButton_Click(object sender, RoutedEventArgs e) { }
        private void saveToDatabaseButton_Click(object sender, RoutedEventArgs e) { }
        private void refreshDatabaseButton_Click(object sender, RoutedEventArgs e) { }
        private void savedGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void updateButton_Click(object sender, RoutedEventArgs e) { }
        private void deleteButton_Click(object sender, RoutedEventArgs e) { }
    }
}