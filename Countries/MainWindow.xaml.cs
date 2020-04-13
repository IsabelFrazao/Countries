using Library.Models;
using Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            networkService = new NetworkService();

            apiService = new ApiService();

            dialogService = new DialogService();

            dataService = new DataService();

            LoadCountriesAsync();
        }

        private async Task LoadCountriesAsync()
        {

            //labelResultado.Text = "A atualizar taxas...";

            var connection = networkService.CheckConnection();

            if (!connection.IsSuccess)
            {
            }
            else
            {
                await LoadApiCountries();
            }

            /*if (Countries.Count == 0)
            {
                //labelResultado.Text = "Não há ligação à Internet" + Environment.NewLine + "e não foram previamente carregadas as taxas" +
                   // Environment.NewLine + "Tente mais tarde!";

                //labelStatus.Text = "Primeira inicialização deverá ter ligação à Internet";

                return;
            }*/

            foreach (var country in Countries)
            {
                comboBoxPaises.Items.Add(country);
            }

            /*if (load)
            {
                labelStatus.Text = string.Format("Taxas carregadas da Internet em\n  {0:F}", DateTime.Now);
            }
            else
            {
                labelStatus.Text = string.Format("Taxas carregadas\nda Base de Dados.");
            }*/
        }
        /* private void LoadLocalRates()
         {
             Countries = dataService.GetData();
         }*/

        private async Task LoadApiCountries()
        {
            //progressBar1.Value = 0;

            var response = await apiService.GetCountries("http://restcountries.eu", "/rest/v2/all");

            Countries = (List<Country>)response.Result;

            //dataService.DeleteData();

            //dataService.SaveData(Countries);
        }

        private void comboBoxPaises_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Country country = (Country)comboBoxPaises.SelectedItem;

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
            img.UriSource = new Uri(Environment.CurrentDirectory + "/Flags" + $"/{flagAbrev}.jpg");
            img.EndInit();
            imageFlag1.Source = img;
            imageFlag1.Stretch = Stretch.Fill;
        }
    }
}