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
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

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
        private bool SavingInDatabase = false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            networkService = new NetworkService();

            apiService = new ApiService();

            dialogService = new DialogService();

            dataService = new DataService();

            LoadCountriesAsync();

            ConvertAsync();
        }

        private async void LoadCountriesAsync()//Tests the Internet Connection
        {
            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            bool load;

            var connection = networkService.CheckConnection();

            if (!connection.IsSuccess)
            {
                SavingInDatabase = false;

                lbl_Status.Content = "Loading...";
                LoadLocalCountries();
                load = false;
                lbl_Status.Content = "Loading Complete";
            }
            else
            {
                SavingInDatabase = false;

                lbl_Status.Content = "Loading...";
                await LoadApiCountriesAsync();
                await GetFlagsAsync(Countries, progress);
                await GetMapsAsync(Countries, progress);
                await GetAudioAsync(Countries, progress);
                //if (!Directory.Exists("Geography"))
                // await GetGeographyTextAsync(Countries);
                load = true;
                lbl_Status.Content = "Loading Complete";
            }

            if (Countries.Count == 0)
            {
                SavingInDatabase = false;

                lbl_Status.Content = "There is no Connection to the Internet" + Environment.NewLine + "Try again later!";

                lbl_Status.Content = "First initialization must have Internet Connection!";

                return;
            }

            listBoxCountries.ItemsSource = Countries;

            if (load)
            {
                SavingInDatabase = true;

                //await dataService.DeleteDataAsync();
                lbl_Status.Content = "Saving...";

                await dataService.SaveData(Countries, Rates, progress);

                lbl_Status.Content = "Saving Complete";

                lbl_Status.Content = "Loaded Successfully" + Environment.NewLine + "          Online";  
            }
            else
            {
                SavingInDatabase = false;

                lbl_Status.Content = "Loaded Successfully" + Environment.NewLine + "        Offline";
            }
        }

        private async Task LoadApiCountriesAsync()
        {
            SavingInDatabase = false;

            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            var response = await apiService.GetCountries("http://restcountries.eu", "/rest/v2/all", progress);

            Countries = (List<Country>)response.Result;
        }

        private async Task GetFlagsAsync(List<Country> countries, IProgress<ProgressReport> progress)
        {
            SavingInDatabase = false;

            ProgressReport report = new ProgressReport();

            if (!Directory.Exists("Flags"))
            {
                Directory.CreateDirectory("Flags");
            }

            foreach (var ct in countries)
            {
                var fileNameSVG = Environment.CurrentDirectory + "/Flags" + $"/{ct.Alpha3Code.ToLower()}.svg";//Path to save the image as SVG
                var fileNameJPG = Environment.CurrentDirectory + "/Flags" + $"/{ct.Alpha3Code.ToLower()}.jpg";
                var pathBackup = Environment.CurrentDirectory + "/FlagsBackup" + $"/{ct.Alpha3Code.ToLower()}.jpg";
                FileInfo imageFile = new FileInfo(fileNameSVG);

                if (!File.Exists(fileNameJPG))
                {
                    try
                    {       
                        //Save the image as SVG from the URL
                        string svgFileName = "https://restcountries.eu" + $"/data/{ct.Alpha3Code.ToLower()}.svg";

                        if(ct.Alpha3Code.ToLower() != "bra")
                        {
                            using (WebClient webClient = new WebClient())
                            {
                                await webClient.DownloadFileTaskAsync(svgFileName, fileNameSVG);
                            }
                        }
                        else
                        {
                            imageFile = new FileInfo(pathBackup);

                            File.Delete(fileNameSVG);

                            imageFile.CopyTo(fileNameJPG);
                        }
                        
                        //Read SVG Document from file system
                        var svgDocument = SvgDocument.Open(fileNameSVG);

                        try
                        {
                            var bitmap = svgDocument.Draw(100, 100); //If the Bitmap it's unable to be created, it will go to catch

                            bitmap.Save(fileNameJPG, ImageFormat.Jpeg);

                            File.Delete(fileNameSVG);
                        }
                        catch
                        {
                            if (File.Exists(pathBackup))
                            {
                                imageFile = new FileInfo(pathBackup);
                                File.Delete(fileNameSVG);
                                imageFile.CopyTo(fileNameJPG);
                            }
                        }
                    }
                    catch
                    {
                        if(File.Exists(pathBackup))
                        {
                            imageFile = new FileInfo(pathBackup);
                            File.Delete(fileNameSVG);
                            imageFile.CopyTo(fileNameJPG);
                        }                        
                        continue;
                    }
                }
                report.SaveCountries.Add(ct);
                report.PercentageComplete = (report.SaveCountries.Count * 100) / countries.Count;
                progress.Report(report);
            }
        }

        private async Task GetMapsAsync(List<Country> countries, IProgress<ProgressReport> progress)
        {
            SavingInDatabase = false;

            ProgressReport report = new ProgressReport();

            if (!Directory.Exists("Maps"))
            {
                Directory.CreateDirectory("Maps");
            }

            foreach (var ct in countries)
            {
                var fileName = Environment.CurrentDirectory + "/Maps" + $"/{ct.Alpha2Code.ToLower()}.gif";
                var pathBackup = Environment.CurrentDirectory + "/MapsBackup" + $"/{ct.Alpha2Code.ToLower()}.gif";
                FileInfo imageFile = new FileInfo(fileName);

                if (!File.Exists(fileName))
                {
                    try
                    {
                        string path1 = string.Empty;
                        string path2 = string.Empty;

                        if (ct.Name.Contains(' '))
                            path1 = ct.Name.Replace((' '), ('-')).ToLower();
                        else
                            path1 = ct.Name.ToLower();

                        if (ct.Name.Contains(' '))
                            path2 = ct.Name.Replace((' '), ('_')).ToLower();
                        else
                            path2 = ct.Name.ToLower();

                        using (WebClient webClient = new WebClient())
                        {
                            await webClient.DownloadFileTaskAsync("https://www.worldmap1.com/map/" + $"{path1}/" + $"where_is_{path2}_in_the_world.gif", @"Maps\" + $"{ct.Alpha2Code.ToLower()}.gif");
                        }

                        try
                        {
                            Bitmap img = new Bitmap(fileName); //If the Bitmap it's unable to be created, it will go to catch
                        }
                        catch
                        {
                            if (File.Exists(pathBackup))
                            {
                                imageFile = new FileInfo(pathBackup);
                                File.Delete(fileName);
                                imageFile.CopyTo(fileName);
                            }
                        }
                    }
                    catch
                    {
                        if(File.Exists(pathBackup))
                        {
                            imageFile = new FileInfo(pathBackup);
                            File.Delete(fileName);
                            imageFile.CopyTo(fileName);
                        }
                        continue;
                    }
                }
                report.SaveCountries.Add(ct);
                report.PercentageComplete = (report.SaveCountries.Count * 100) / countries.Count;
                progress.Report(report);
            }
        }

        private void LoadLocalCountries()
        {
            SavingInDatabase = false;

            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            Countries = dataService.GetCountryDataAsync(progress);
        }

        private void txtBoxCountries_TextChanged(object sender, TextChangedEventArgs e)
        {
            SavingInDatabase = false;

            if (Countries != null && !string.IsNullOrEmpty(txtBoxCountries.Text))
            {
                listBoxCountries.ItemsSource = null;

                var aux = Countries.FindAll(x => x.Name.ToLower().Contains(txtBoxCountries.Text.ToLower())).ToList();

                listBoxCountries.ItemsSource = aux;

                if (aux.Count == 0)
                {
                    MessageBox.Show("Country does not exist");
                }
            }
            else if (string.IsNullOrEmpty(txtBoxCountries.Text))
                listBoxCountries.ItemsSource = Countries;
        }

        private void listBoxCountries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SavingInDatabase = false;

            try
            {
                mediaPlayer.Close();
                lbl_Status.Content = string.Empty;
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

                Country country = (Country)listBoxCountries.SelectedItem;

                if (country != null)
                {
                    labelName.Content = country.Name;
                    labelCapital.Content = country.Capital;
                    labelRegion.Content = country.Region;
                    labelSubRegion.Content = country.SubRegion;
                    labelPopulation.Content = country.Population;
                    labelGini.Content = country.Gini;

                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    if (File.Exists(Environment.CurrentDirectory + "/Flags" + $"/{country.Alpha3Code.ToLower()}.jpg"))
                    {
                        img.UriSource = new Uri(Environment.CurrentDirectory + "/Flags" + $"/{country.Alpha3Code.ToLower()}.jpg");
                    }
                    else
                    {
                        img.UriSource = new Uri(Environment.CurrentDirectory + "/ImageUnavailable.jpg");
                        imageFlag1.Stretch = Stretch.None;
                    }
                    img.EndInit();
                    imageFlag1.Source = img;
                    imageFlag1.Stretch = Stretch.Fill;

                    BitmapImage img2 = new BitmapImage();
                    img2.BeginInit();
                    if (File.Exists(Environment.CurrentDirectory + @"\Maps\" + $"{country.Alpha2Code.ToLower()}.gif"))
                    {
                        img2.UriSource = new Uri(Environment.CurrentDirectory + @"\Maps\" + $"{country.Alpha2Code.ToLower()}.gif");
                    }
                    else
                    {
                        img2.UriSource = new Uri(Environment.CurrentDirectory + "/ImageUnavailable.jpg");
                        imageFlag1.Stretch = Stretch.None;
                    }
                    img2.EndInit();
                    imageMap.Source = img2;
                    //imageMap.Stretch = Stretch.Fill;

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

                    if (comboBoxConverterOutput.Items.Count == 0)
                        comboBoxConverterOutput.Text = "Unable to Convert";

                    textBoxGeography.Text = LoadGeography(country);

                    LoadAudio(country);
                }
            }
            catch(Exception ex)
            {
                dialogService.ShowMessage("An error occurred.\nPlease Update Info!", ex.Message);
            }
        }

        private void ReportProgress(object sender, ProgressReport e)
        {
            ProgressBarCountries.Value = e.PercentageComplete;
        }

        #region LANGUAGE TRANSLATOR

        private async void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

            if (string.IsNullOrEmpty(comboBoxTranslatorInput.Text) || string.IsNullOrEmpty(comboBoxTranslatorOutput.Text))
            {
                MessageBox.Show("   You must choose a Language", "", MessageBoxButton.OK);
            }
            else
            {
                string source = comboBoxTranslatorInput.Text.Split(' ')[0];
                string target = comboBoxTranslatorOutput.Text.Split(' ')[0];

                await TranslateAsync(source, target, txtBoxTranslatorInput.Text);
            }
        }

        private async Task TranslateAsync(string source, string target, string input)
        {
            SavingInDatabase = false;

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
            SavingInDatabase = false;

            txtBoxTranslatorInput.Text = string.Empty;
        }

        private void btnSwitchLanguage_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

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

        private async void ConvertAsync()
        {
            SavingInDatabase = false;

            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            var response = await apiService.GetRates("https://cambiosrafa.azurewebsites.net", "/api/rates", progress);

            Rates = (List<Rate>)response.Result;
        }

        private void txtBoxCountries_GotFocus(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

            txtBoxCountries.Text = string.Empty;
        }

        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

            Convert();
        }

        private void Convert()
        {
            SavingInDatabase = false;

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
            SavingInDatabase = false;

            txtBoxConverterInput.Text = string.Empty;
        }

        private void btnSwitchCurrency_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

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

        private async Task GetAudioAsync(List<Country> countries, IProgress<ProgressReport> progress)
        {
            SavingInDatabase = false;

            ProgressReport report = new ProgressReport();

            if (!Directory.Exists("Audio"))
            {
                Directory.CreateDirectory("Audio");
            }

            foreach (var country in countries)
            {
                var fileName = Environment.CurrentDirectory + "/Audio" + $"/{country.Alpha2Code.ToLower()}.mp3";
                var pathBackup = Environment.CurrentDirectory + "/AudioBackup" + $"/{country.Alpha2Code.ToLower()}.mp3";

                if (!File.Exists(fileName))
                {
                    FileInfo fileLength = new FileInfo(fileName);

                    try
                    {
                        using (var client = new WebClient())
                        {
                            await client.DownloadFileTaskAsync("http://www.nationalanthems.info/" + $"{country.Alpha2Code.ToLower()}.mp3", fileName);
                        }
                    }
                    catch
                    {
                        if(File.Exists(pathBackup))
                        {
                            File.Delete(fileName);
                            fileLength = new FileInfo(pathBackup);
                            fileLength.CopyTo(fileName);
                        }
                        continue;
                    }
                }
                report.SaveCountries.Add(country);
                report.PercentageComplete = (report.SaveCountries.Count * 100) / countries.Count;
                progress.Report(report);
            }
        }

        private void LoadAudio(Country country)
        {
            SavingInDatabase = false;

            mediaPlayer.Open(new Uri(Environment.CurrentDirectory + @"\Audio\" + $"{country.Alpha2Code.ToLower()}.mp3"));

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

            mediaPlayer.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

            mediaPlayer.Pause();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

            mediaPlayer.Stop();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            SavingInDatabase = false;

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
            SavingInDatabase = false;

            userIsDraggingSlider = true;
        }

        private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            SavingInDatabase = false;

            userIsDraggingSlider = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SavingInDatabase = false;

            if (mediaPlayer.Source != null)
                lblProgressStatus.Text = $"{TimeSpan.FromSeconds(sliProgress.Value).ToString(@"mm\:ss")} / {mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss")}";
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            SavingInDatabase = false;

            mediaPlayer.Volume += (e.Delta > 0) ? 0.1 : -0.1;
        }

        #endregion MUSIC PLAYER

        #region GEOGRAPHY TEXT

        private string LoadGeography(Country country)
        {
            try
            {
                string ficheiro = @"Geography\" + $"{country.Name}.txt";

                string Texto = string.Empty;

                StreamReader sr;

                if (File.Exists(ficheiro))//Só funciona se já existir um ficheiro
                {
                    sr = File.OpenText(ficheiro);

                    string linha = string.Empty;

                    while ((linha = sr.ReadLine()) != null)
                    {
                        Texto = linha;
                    }
                    sr.Close();
                }
                return Texto;
            }
            catch
            {
                MessageBox.Show("Error!");

                return null;
            }
        }

        /// <summary>
        /// Gets a block of Text from a Webpage using the URL path, decoding the Webpage HTML, and displays it on a TextBox
        /// </summary>
        /// <param name="selectedCountry"></param>
        private async Task GetGeographyTextAsync(List<Country> countries)
        {
            foreach (var country in countries)
            {
                try
                {
                    if (!Directory.Exists("Geography"))
                    {
                        Directory.CreateDirectory("Geography");
                    }

                    string ficheiro = @"Geography\" + $"{country.Name}.txt";

                    StreamWriter sw = new StreamWriter(ficheiro, false);

                    if (!File.Exists(ficheiro))
                    {
                        sw = File.CreateText(ficheiro);
                    }

                    if (country.Name.Contains(' '))
                        country.Name = country.Name.Replace((' '), ('-'));

                    WebClient wc = new WebClient();
                    string webData = wc.DownloadString("https://www.infoplease.com/world/countries/" + $"{country.Name}");

                    if (country.Name.Contains('-'))
                        country.Name = country.Name.Replace(('-'), (' '));

                    string Texto = StripHTML(webData.Split(new string[] { "Geography" }, StringSplitOptions.None)[3].Split(new string[] { "Government" }, StringSplitOptions.None)[0].ToString(), true);

                    await Task.Run(() => sw.WriteLine(Texto));

                    sw.Close();
                }
                catch
                {
                    if (country.Name.Contains('-'))
                        country.Name = country.Name.Replace(('-'), (' '));
                    continue;
                }
            }
        }

        /// <summary>
        /// Converts HTML into a String
        /// </summary>
        /// <param name="HTMLText"></param>
        /// <param name="decode"></param>
        /// <returns></returns>
        public static string StripHTML(string HTMLText, bool decode = true)
        {
            Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            var stripped = reg.Replace(HTMLText, "");
            return decode ? HttpUtility.HtmlDecode(stripped) : stripped;
        }

        #endregion GEOGRAPHY TEXT

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = true;

            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += ReportProgress;

            lbl_Status.Content = "Updating...";

            await dataService.DeleteDataAsync();

            await dataService.SaveData(Countries, Rates, progress);

            lbl_Status.Content = "Update Complete";

            this.Closing += Window_Closing;
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            SavingInDatabase = false;

            AboutWindow aw = new AboutWindow();
            aw.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (SavingInDatabase == true)
                {
                    e.Cancel = true;

                    MessageBox.Show("This will interrupt the Update!\nThe program will not run properly!\n" +
                        "Next time the program is in use, the Update will complete.");
                }
            }
            catch
            {
                Close();
            }            
        }
    }
}
