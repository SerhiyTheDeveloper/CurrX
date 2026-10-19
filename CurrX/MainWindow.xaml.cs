using CurrX.Entities;
using CurrX.Services;
using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Text.Json;
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
        private RateEntry? _currentRateEntry;
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

        private void SetBusy(bool isBusy)
        {
            loadButton.IsEnabled = !isBusy;
            openButton.IsEnabled = !isBusy;
            saveButton.IsEnabled = !isBusy && _currentRateEntry is not null;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) { }

        private async void loadButton_Click(object sender, RoutedEventArgs e)
        {
            SetBusy(true);
            SetStatus("Завантаження...");

            try
            {
                if (currencyCombo.SelectedItem is CurrencyItem selectedCurrency)
                {
                    string url = $"https://bank.gov.ua/NBUStatService/v1/statdirectory/exchangenew?json&valcode={selectedCurrency.Code}&date={DateTime.Now:yyyyMMdd}";
                    _currentRateEntry = await RateService.LoadRateAsync(url);
                    ShowRateEntry(_currentRateEntry);
                    SetStatus("Завантаження завершено.");
                }
            }
            catch (HttpRequestException)
            {
                SetStatus("Не вдалося з’єднатися із сервером.", isError: true);
            }
            catch (TaskCanceledException)
            {
                SetStatus("Сервер не відповів за відведений час.", isError: true);
            }
            catch (JsonException)
            {
                SetStatus("Сервер повернув дані неочікуваного формату.", isError: true);
            }
            catch (Exception ex)
            {
                SetStatus("Сталася непередбачена помилка.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ShowRateEntry(RateEntry rateEntry)
        {
            nameLabel.Text = rateEntry.Currency;
            rateLabel.Text = rateEntry.Rate.ToString();
            codeLabel.Text = rateEntry.Code;
            dateLabel.Text = rateEntry.ExchangeDate;
        }

        private async void saveButton_Click(object sender, RoutedEventArgs e) 
        {
            if (_currentRateEntry is null)
            {
                SetStatus("Спочатку завантажте або відкрийте дані.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Зберегти дані про погоду",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                AddExtension = true,
                FileName = $"weather-{DateTime.Now:yyyy-MM-dd}.json"
            };

            if (dialog.ShowDialog() != true) 
            {
                SetStatus("Збереження скасовано.");
                return;
            }

            SetBusy(true);
            SetStatus("Збереження файла...");

            try
            {
                RateEntrySnapshot snapshot = new()
                {
                    DataSource = "Національний банк України",
                    SourceUrl = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchangenew?json",
                    FormatVersion = "1.0",
                    SavedAt = DateTimeOffset.Now,
                    RateEntry = _currentRateEntry
                };
                await JsonFileService.SaveAsync(
                dialog.FileName, snapshot);
                SetStatus($"Файл збережено: {Path.GetFileName(dialog.FileName)}");
            }
            catch (UnauthorizedAccessException)
            {
                SetStatus("Немає дозволу на запис у вибране місце.", isError: true);
            }
            catch (IOException)
            {
                SetStatus("Не вдалося записати файл. Можливо, він уже відкритий.", isError: true);
            }
            catch (Exception ex)
            {
                SetStatus("Не вдалося зберегти файл.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void openButton_Click(object sender, RoutedEventArgs e) 
        {
            OpenFileDialog dialog = new()
            {
                Title = "Відкрити дані про погоду",
                Filter = "JSON files (*.json)|*.json",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                SetStatus("Відкриття скасовано.");
                return;
            }

            SetBusy(true);
            SetStatus("Відкриття файла...");

            try
            {
                RateEntrySnapshot snapshot = await JsonFileService
                    .LoadAsync<RateEntrySnapshot>(dialog.FileName);
                if (snapshot.FormatVersion != "1.0")
                {
                    SetStatus("Файл має невідому версію формату.", isError: true);
                    return;
                }
                RateEntry? entry = snapshot.RateEntry ?? throw new JsonException(
                    "У файлі відсутній об’єкт RateEntry.");
                _currentRateEntry = entry;
                ShowRateEntry(entry);

                string localSavedTime = snapshot.SavedAt
                .ToLocalTime()
                .ToString("g");
                statusLabel.Text =
                $"Відкрито {Path.GetFileName(dialog.FileName)}. " +
                $"Збережено: {localSavedTime}.";
            }
            catch (JsonException)
            {
                SetStatus("Файл не є коректним файлом даних цього застосунку.", isError: true);
            }
            catch (UnauthorizedAccessException)
            {
                SetStatus("Немає дозволу на читання вибраного файла.", isError: true);
            }
            catch (IOException)
            {
                SetStatus("Не вдалося прочитати файл.", isError: true);
            }
            catch (Exception ex)
            {
                SetStatus("Не вдалося відкрити файл.", isError: true);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }
        private void saveToDatabaseButton_Click(object sender, RoutedEventArgs e) { }
        private void refreshDatabaseButton_Click(object sender, RoutedEventArgs e) { }
        private void savedGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void updateButton_Click(object sender, RoutedEventArgs e) { }
        private void deleteButton_Click(object sender, RoutedEventArgs e) { }
    }
}