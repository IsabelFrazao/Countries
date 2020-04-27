using Library;
using Library.Models;
using Services;
using Svg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        List<Rate> Rates = new List<Rate>();
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
            bool load;

            labelResult.Content = "Loading...";

            var connection = networkService.CheckConnection();

            if (!connection.IsSuccess)
            {
                LoadLocalCountries();
                load = false;
            }
            else
            {
                await LoadApiCountries();
                GetFlags(Countries);
                load = true;
            }

            if (Countries.Count == 0)
            {
                labelResult.Content = "There is no Connection to the Internet" + Environment.NewLine + "Try again later!";

                labelResult.Content = "First initialization must have Internet Connection!";

                return;
            }

            listBoxCountries.ItemsSource = Countries;

            if (load)
            {
                labelResult.Content = "Loaded Successfully";//"(Loaded from the Internet on\n  {0:F}", DateTime.Now)  

                await dataService.DeleteDataAsync();

                await dataService.SaveData(Countries, Rates);
            }
            else
            {
                labelResult.Content = string.Format("Loaded Successfully from the Database");
            }
        }

        private void LoadLocalCountries()
        {
            Countries = dataService.GetCountryDataAsync();
        }

        private async Task LoadApiCountries()
        {
            var response = await apiService.GetCountries("http://restcountries.eu", "/rest/v2/all");

            Countries = (List<Country>)response.Result;
        }

        private void GetFlags(List<Country> countries)
        {
            //Se a pasta Flags não existir, criar
            if (!Directory.Exists("Flags"))
            {
                Directory.CreateDirectory("Flags");
            }

            foreach (var ct in countries)
            {
                string flagNameAb = ct.Flag.Split('/')[4].Split('.')[0];

                if (!File.Exists(Environment.CurrentDirectory + "/Flags" + $"/{flagNameAb}.jpg"))
                {
                    try
                    {
                        //Create a Path inside the Flags Folder to save the Flag Image with the name abbreviated                    
                        var path = @"Flags\" + $"{flagNameAb}.svg";//Path to save the image as SVG

                        //Save the image as SVG from the URL
                        string svgFileName = "https://restcountries.eu" + $"/data/{flagNameAb}";


                        using (WebClient webClient = new WebClient())
                        {
                            webClient.DownloadFile(svgFileName, path);
                        }

                        #region Open the SVG Image and Save it in the Flags Folder as JPEG

                        var path2 = @"Flags\" + $"{flagNameAb}.jpg";//Path to Save the image as JPEG

                        //Read SVG Document from file system
                        var svgDocument = SvgDocument.Open(path);
                        var bitmap = svgDocument.Draw(100, 100);

                        //Save converted SVG to file system
                        if (!File.Exists(path2))
                        {
                            bitmap.Save(path2, ImageFormat.Jpeg);
                        }

                        //Delete the SVG Images
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    #endregion

                    catch
                    {
                        continue;
                    }
                }
            }
        }

        private async void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(comboBoxTranslatorInput.Text) || string.IsNullOrEmpty(comboBoxTranslatorOutput.Text))
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

        private void txtBoxCountries_TextChanged(object sender, TextChangedEventArgs e)
        {
            listBoxCountries.ItemsSource = null;

            if (Countries != null)
            {
                var aux = Countries.FindAll(x => x.Name.ToLower().Contains(txtBoxCountries.Text.ToLower())).ToList();

                listBoxCountries.ItemsSource = aux;

                if (aux.Count == 0)
                {
                    MessageBox.Show("The specified country does not exist");
                }
            }
        }

        private void btnCountryOK_Click(object sender, RoutedEventArgs e)
        {
            labelResult.Content = string.Empty;
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

            labelName.Content = country.Name;
            labelCapital.Content = country.Capital;
            labelRegion.Content = country.Region;
            labelSubRegion.Content = country.SubRegion;
            labelPopulation.Content = country.Population;
            labelGini.Content = country.Gini;

            string flagNameAbrev = country.Flag.Split('/')[4];

            string flagAbrev = flagNameAbrev.Split('.')[0];
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            if (File.Exists(Environment.CurrentDirectory + "/Flags" + $"/{flagAbrev}.jpg"))
            {
                img.UriSource = new Uri(Environment.CurrentDirectory + "/Flags" + $"/{flagAbrev}.jpg");
            }
            else
            {
                img.UriSource = new Uri(Environment.CurrentDirectory + "/flagUnavailable.jpg");
                imageFlag1.Stretch = Stretch.None;
            }
            img.EndInit();
            imageFlag1.Source = img;
            imageFlag1.Stretch = Stretch.Fill;

            if(country.Languages != null)
            {
                List<string> LangDistinct = new List<string>();

                foreach (var ct in Countries)
                {
                    foreach (var lg in ct.Languages)
                    {
                        if (!LangDistinct.Contains(lg.ToString()))
                            LangDistinct.Add(lg.ToString());
                    }
                }

                comboBoxTranslatorInput.ItemsSource = LangDistinct;

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
                    if (cr.code.ToLower() == rate.Code.ToLower() && cr.code != null)
                        comboBoxConverterOutput.Items.Add(rate);
                }
            }

            if (comboBoxConverterOutput.Items.Count == 0)
                comboBoxConverterOutput.Text = "Unable to Convert";
        }

        private async void ConvertAsync()
        {
            var response = await apiService.GetRates("https://cambiosrafa.azurewebsites.net", "/api/rates");

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
    }
}
