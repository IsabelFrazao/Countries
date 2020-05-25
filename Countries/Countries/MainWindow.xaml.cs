using Library;
using Library.Models;
using Services;
using Svg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using MessageBox = System.Windows.MessageBox;

namespace Countries
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        #region Atributos
        private List<Country> Countries;
        private NetworkService networkService;
        private ApiService apiService;
        private DialogService dialogService;
        private DataService dataService;
        private List<Rate> Rates = new List<Rate>();
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private bool userIsDraggingSlider = false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            networkService = new NetworkService();
            apiService = new ApiService();
            dialogService = new DialogService();
            dataService = new DataService();

            LoadInfoAsync();
        }

        /// <summary>
        /// Tests the Internet Connection.
        /// If successful loads the information from the Web.
        /// If failed, loads the information from the saved Database.
        /// </summary>
        private async void LoadInfoAsync()
        {
            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            bool load;

            var connection = networkService.CheckConnection();

            if (!connection.IsSuccess)
            {
                lbl_Status.Content = "Loading...";
                btnUpdate.IsEnabled = false;
                await LoadLocalCountriesAsync();
                load = false;
                lbl_Status.Content = "Loading Complete";
            }
            else
            {
                lbl_Status.Content = "Loading...";
                btnUpdate.IsEnabled = false;
                await LoadApiCountriesAsync();
                await LoadApiRatesAsync();
                await LoadApiWikiTextAsync(Countries, progress);
                await dataService.GetFlagsAsync(Countries, progress);
                await dataService.GetMapsAsync(Countries, progress);
                await dataService.GetAudioAsync(Countries, progress);
                load = true;
                lbl_Status.Content = "Loading Complete";
                btnUpdate.IsEnabled = true;
            }

            if (Countries.Count == 0)
            {
                lbl_Status.Content = "There is no Connection to the Internet" + Environment.NewLine + "Try again later!";
                lbl_Status.Content = "First initialization must have Internet Connection!";

                return;
            }

            listBoxCountries.ItemsSource = Countries;

            if (load)
            {
                lbl_Status.Content = "Saving...";
                btnUpdate.IsEnabled = false;
                await dataService.SaveData(Countries, Rates, progress);
                lbl_Status.Content = "Saving Complete";
                lbl_Status.Content = "Loaded Successfully" + Environment.NewLine + "          Online";
                btnUpdate.IsEnabled = true;
            }
            else
            {
                lbl_Status.Content = "Loaded Successfully" + Environment.NewLine + "        Offline";
                txtBoxTranslatorOutput.Text = "-- Translation Unavailable --";
                txtBoxTranslatorInput.Text = "-- Translation Unavailable --";
            }
        }

        /// <summary>
        /// Event to display the progress report status in the Progress Bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportProgress(object sender, ProgressReport e)
        {
            ProgressBarCountries.Value = e.PercentageComplete;
        }

        #region COUNTRIES

        /// <summary>
        /// Redirects to the ApiService Class to make the API Call to the Countries Rest API asynchronously.
        /// </summary>
        /// <returns>Task</returns>
        private async Task LoadApiCountriesAsync()
        {
            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            var response = await apiService.GetCountries("http://restcountries.eu", "/rest/v2/all", progress);

            Countries = (List<Country>)response.Result;
        }

        /// <summary>
        /// Accesses the DataService Class to load the information if the Software is loaded Offline
        /// </summary>
        private async Task LoadLocalCountriesAsync()
        {
            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            Countries = await dataService.LoadCountryDataAsync(progress);

            Rates = await dataService.LoadRatesData();
        }

        #endregion COUNTRIES

        #region WIKIPEDIA TEXT

        /// <summary>
        /// Redirects to the ApiService Class to make the API Call to the Wikipedia API asynchronously.
        /// </summary>
        /// <param name="countries"></param>
        /// <param name="progress"></param>
        /// <returns>Task</returns>
        private async Task LoadApiWikiTextAsync(List<Country> countries, IProgress<ProgressReport> progress)
        {
            ProgressReport report = new ProgressReport();

            foreach (var country in countries)
            {
                var fileName = Environment.CurrentDirectory + "/WikiText" + $"/{country.Alpha2Code.ToLower()}.txt";

                if (!File.Exists(fileName))
                {
                    try
                    {
                        var response = await apiService.GetWikiText("https://en.wikipedia.org/w/api.php", $"?format=xml&action=query&prop=extracts&titles={country.Name.Replace((' '), ('_'))}&redirects=true", country.Name);

                        string output = (string)response.Result;

                        await dataService.SaveWikiTextAsync(country.Alpha2Code.ToLower(), output);
                    }
                    catch
                    {
                        continue;
                    }
                }
                report.SaveCountries.Add(country);
                report.PercentageComplete = (report.SaveCountries.Count * 100) / countries.Count;
                progress.Report(report);
            }
        }

        #endregion WIKIPEDIA TEXT

        #region LANGUAGE TRANSLATOR

        private async void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(comboBoxTranslatorInput.Text) || string.IsNullOrEmpty(comboBoxTranslatorOutput.Text))
            {
                MessageBox.Show("   You must choose a Language", "", MessageBoxButton.OK);
            }
            else
            {
                string source = comboBoxTranslatorInput.Text.Split(' ')[0];
                string target = comboBoxTranslatorOutput.Text.Split(' ')[0];

                await LoadApiTranslateAsync(source, target, txtBoxTranslatorInput.Text);
            }
        }

        /// <summary>
        /// Redirects to the ApiService Class to make the API Call to the Translator API asynchronously.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="input"></param>
        /// <returns>Task</returns>
        private async Task LoadApiTranslateAsync(string source, string target, string input)
        {
            var response = await apiService.GetTranslation("https://systran-systran-platform-for-language-processing-v1.p.rapidapi.com", $"/translation/text/translate?source={source}&target={target}&input={input}");

            string output = (string)response.Result;

            if (string.IsNullOrEmpty(output))
            {
                txtBoxTranslatorOutput.Text = "-- Translation Unavailable --";
            }
            else
            {
                txtBoxTranslatorOutput.Text = output;
            }
        }

        private void txtBoxTranslatorInput_GotFocus(object sender, RoutedEventArgs e)
        {
            txtBoxTranslatorInput.Text = string.Empty;
        }

        private void btnSwitchLanguage_Click(object sender, RoutedEventArgs e)
        {
            List<string> LangList = new List<string>();

            foreach (var lang in comboBoxTranslatorOutput.Items)
            {
                LangList.Add(lang.ToString());
            }

            var itemIn = (string)comboBoxTranslatorInput.SelectedItem;
            var itemOut = (string)comboBoxTranslatorOutput.SelectedItem;

            comboBoxTranslatorOutput.ItemsSource = null;
            comboBoxTranslatorOutput.Items.Clear();

            var source = comboBoxTranslatorInput.ItemsSource;
            comboBoxTranslatorInput.ItemsSource = LangList;
            comboBoxTranslatorOutput.ItemsSource = source;

            comboBoxTranslatorInput.Text = itemOut;
            comboBoxTranslatorOutput.Text = itemIn;

            var label = labelSelectLang.Content;
            labelSelectLang.Content = labelSelectLangCountry.Content;
            labelSelectLangCountry.Content = label;

            if (txtBoxTranslatorOutput.Text != "Translated Text")
            {
                var text = txtBoxTranslatorInput.Text;
                txtBoxTranslatorInput.Text = txtBoxTranslatorOutput.Text;
                txtBoxTranslatorOutput.Text = text;
            }
        }

        #endregion LANGUAGE TRANSLATOR

        #region CURRENCY CONVERTER

        /// <summary>
        /// Redirects to the ApiService Class to make the API Call to the Rates API asynchronously.
        /// </summary>
        /// <returns></returns>
        private async Task LoadApiRatesAsync()
        {
            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            var response = await apiService.GetRates("https://cambiosrafa.azurewebsites.net", "/api/rates", progress);

            Rates = (List<Rate>)response.Result;
        }

        private void txtBoxCountries_GotFocus(object sender, RoutedEventArgs e)
        {
            txtBoxCountries.Text = string.Empty;
        }

        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            Convert();
        }

        /// <summary>
        /// Gets and Sets the value to be Converted, calculating it according to the Tax Rates.
        /// </summary>
        private void Convert()
        {
            if (string.IsNullOrEmpty(txtBoxConverterInput.Text))
            {
                dialogService.ShowMessage("Error", "Insert a Value to Convert");
                return;
            }

            decimal value;

            if (!decimal.TryParse(txtBoxConverterInput.Text, out value))
            {
                dialogService.ShowMessage("Conversion Error", "Inserted Value has to be Numeric");
                return;
            }

            if (comboBoxConverterInput.SelectedItem == null)
            {
                dialogService.ShowMessage("Error", "Select a Currency for the Value to be Converted from");
                return;
            }

            if (comboBoxConverterOutput.SelectedItem == null)
            {
                dialogService.ShowMessage("Error", "Select a Currency for the Conversion");
                return;
            }

            var Input = comboBoxConverterInput.Text.Split(' ')[0];
            var Output = comboBoxConverterOutput.Text.Split(' ')[0];

            var InputTax = Rates.Find(x => x.Code.ToString() == Input);
            var OutputTax = Rates.Find(x => x.Code.ToString() == Output);

            var convertedValue = value / (decimal)InputTax.TaxRate * (decimal)OutputTax.TaxRate;

            txtBoxConverterOutput.Text = Math.Round(convertedValue, 2).ToString();
        }

        private void txtBoxConverterInput_GotFocus(object sender, RoutedEventArgs e)
        {
            txtBoxConverterInput.Text = string.Empty;
        }

        private void btnSwitchCurrency_Click(object sender, RoutedEventArgs e)
        {
            List<string> CurrencyList = new List<string>();

            foreach (var currency in comboBoxConverterOutput.Items)
            {
                CurrencyList.Add(currency.ToString());
            }

            var itemIn = comboBoxConverterInput.Text;
            var itemOut = comboBoxConverterOutput.Text;

            comboBoxConverterOutput.ItemsSource = null;
            comboBoxConverterOutput.Items.Clear();

            var source = comboBoxConverterInput.ItemsSource;
            comboBoxConverterInput.ItemsSource = CurrencyList;
            comboBoxConverterOutput.ItemsSource = source;

            comboBoxConverterInput.Text = itemOut;
            comboBoxConverterOutput.Text = itemIn;

            var label = labelSelectCurrency.Content;
            labelSelectCurrency.Content = labelSelectCurrencyCountry.Content;
            labelSelectCurrencyCountry.Content = label;

            if (txtBoxConverterOutput.Text != "Converted Value")
            {
                var text = txtBoxConverterInput.Text;
                txtBoxConverterInput.Text = txtBoxConverterOutput.Text;
                txtBoxConverterOutput.Text = text;
            }
        }

        #endregion CURRENCY CONVERTER

        #region MUSIC PLAYER

        /// <summary>
        /// Loads the Audio file from the Audio Folder to be displayed.
        /// </summary>
        /// <param name="alpha2Code"></param>
        private void LoadAudio(string alpha2Code)
        {
            mediaPlayer.Open(new Uri(Environment.CurrentDirectory + @"\Audio\" + $"{alpha2Code}.mp3"));

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
        }

        /// <summary>
        /// Event to set the Timer, Slider Bar and Volume Bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timer_Tick(object sender, EventArgs e)
        {
            if ((mediaPlayer.Source != null) && (mediaPlayer.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
            {
                sliProgress.Minimum = 0;
                sliProgress.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliProgress.Value = mediaPlayer.Position.TotalSeconds;

                pbVolume.Value = mediaPlayer.Volume;
            }
        }

        private void sliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer.Source != null)
                lblProgressStatus.Text = $"{TimeSpan.FromSeconds(sliProgress.Value).ToString(@"mm\:ss")} / {mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss")}";
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            mediaPlayer.Volume += (e.Delta > 0) ? 0.1 : -0.1;
        }

        #endregion MUSIC PLAYER

        private void txtBoxCountries_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Countries != null && !string.IsNullOrEmpty(txtBoxCountries.Text))
            {
                listBoxCountries.ItemsSource = null;

                var aux = Countries.FindAll(x => x.Name.ToLower().Contains(txtBoxCountries.Text.ToLower())).ToList();

                listBoxCountries.ItemsSource = aux;

                if (aux.Count == 0)
                {
                    MessageBox.Show("Country does not Exist!");
                }
            }
            else if (string.IsNullOrEmpty(txtBoxCountries.Text))
                listBoxCountries.ItemsSource = Countries;
        }

        private void listBoxCountries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                mediaPlayer.Close();
                comboBoxTranslatorInput.ItemsSource = null;
                comboBoxTranslatorOutput.ItemsSource = null;
                comboBoxTranslatorOutput.Items.Clear();
                comboBoxConverterInput.ItemsSource = null;
                comboBoxConverterOutput.ItemsSource = null;
                comboBoxConverterOutput.Items.Clear();
                if (txtBoxTranslatorInput.Text != "Insert the Text to Translate")
                    txtBoxTranslatorInput.Text = "Insert the Text to Translate";
                if (txtBoxTranslatorOutput.Text != "Translated Text")
                    txtBoxTranslatorOutput.Text = "Translated Text";
                comboBoxTranslatorInput.SelectedItem = "en - English";
                if (txtBoxConverterInput.Text != "Insert the Value to Convert")
                    txtBoxConverterInput.Text = "Insert the Value to Convert";
                if (txtBoxConverterOutput.Text != "Converted Value")
                    txtBoxConverterOutput.Text = "Converted Value";

                Country country = (Country)listBoxCountries.SelectedItem;

                if(country.Languages == null)
                {
                    txtBoxTranslatorOutput.Text = "-- Translation Unavailable --";
                    txtBoxTranslatorInput.Text = "-- Translation Unavailable --";
                }                

                if (country != null)
                {
                    labelName.Content = country.Name;
                    labelCapital.Content = country.Capital;
                    labelRegion.Content = country.Region;
                    labelSubRegion.Content = country.SubRegion;
                    labelPopulation.Content = country.Population;
                    labelGini.Content = country.Gini;

                    string fileNameFlags = Environment.CurrentDirectory + "/Flags" + $"/{country.Alpha3Code.ToLower()}.jpg";

                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    if (File.Exists(fileNameFlags))
                    {
                        img.UriSource = new Uri(fileNameFlags);
                    }
                    else
                    {
                        img.UriSource = new Uri(Environment.CurrentDirectory + "/ImageUnavailable.jpg");
                        imageFlag1.Stretch = Stretch.None;
                    }
                    img.EndInit();
                    imageFlag1.Source = img;

                    string fileNameMaps = Environment.CurrentDirectory + @"\Maps\" + $"{country.Alpha2Code.ToLower()}.gif";

                    BitmapImage img2 = new BitmapImage();
                    img2.BeginInit();
                    if (File.Exists(fileNameMaps))
                    {
                        img2.UriSource = new Uri(fileNameMaps);
                    }
                    else
                    {
                        img2.UriSource = new Uri(Environment.CurrentDirectory + "/ImageUnavailable.jpg");
                        imageFlag1.Stretch = Stretch.None;
                    }
                    img2.EndInit();
                    imageMap.Source = img2;

                    List<string> LangDistinct = new List<string>();

                    if (country.Languages != null)
                    {
                        foreach (var ct in Countries)
                        {
                            foreach (var lg in ct.Languages)
                            {
                                if (!LangDistinct.Contains(lg.ToString()))
                                    LangDistinct.Add(lg.ToString());
                            }
                        }

                    }
                    comboBoxTranslatorInput.ItemsSource = LangDistinct;

                    if (country.Languages != null)
                    {
                        foreach (var lg in country.Languages)
                        {
                            comboBoxTranslatorOutput.Items.Add(lg.ToString());
                        }
                    }

                    comboBoxConverterInput.ItemsSource = Rates;

                    if (Rates != null)
                    {
                        foreach (var cr in country.Currencies)
                        {
                            foreach (var rate in Rates)
                            {
                                if (cr.code != null)
                                {
                                    if (cr.code.ToLower() == rate.Code.ToLower())
                                        comboBoxConverterOutput.Items.Add(rate);
                                }
                            }
                        }
                    }

                    if (comboBoxConverterOutput.Items.Count == 0)
                        comboBoxConverterOutput.Text = "Unable to Convert";

                    textBoxWiki.Text = dataService.LoadWikiText(country.Alpha2Code.ToLower());

                    LoadAudio(country.Alpha2Code.ToLower());
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("An error occurred.\nPlease Update Info!", ex.Message);
            }
        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            lbl_Status.Content = "Updating...";

            btnUpdate.IsEnabled = false;

            await dataService.DeleteDataAsync();

            await dataService.SaveData(Countries, Rates, progress);

            lbl_Status.Content = "Update Complete";

            this.Closing += Window_Closing;

            btnUpdate.IsEnabled = true;
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (((string)lbl_Status.Content == "Saving...") || ((string)lbl_Status.Content == "Updating...") || ((string)lbl_Status.Content == "Loading..."))
                {
                    MessageBoxResult result = MessageBox.Show("Do you wish to exit the program?\n The program might not run properly next time!\n",
                        "", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                        return;
                    else
                        e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
