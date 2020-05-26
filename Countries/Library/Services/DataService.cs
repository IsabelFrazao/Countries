using Library;
using Library.Models;
using ServiceStack;
using Svg;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Documents;

namespace Services
{
    public class DataService
    {
        private SQLiteConnection connection;

        private SQLiteCommand command;

        private DialogService dialogService;

        /// <summary>
        /// This class is used when Information needs to be Saved or Loaded from the Database.
        /// It's used in the First Initialization, Update Request or Program working Offline.
        /// </summary>
        public DataService() //Creating the DataBase and the Tables on the Database 
        {
            dialogService = new DialogService();

            var pointCulture = new CultureInfo("en")
            {
                NumberFormat = { NumberDecimalSeparator = "." }
            };

            Thread.CurrentThread.CurrentCulture = pointCulture;

            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }

            var path = @"Data\Countries.sqlite";

            try
            {
                connection = new SQLiteConnection("Data Source =" + path);

                connection.Open();

                string sqlcommand = "CREATE TABLE IF NOT EXISTS CountryCurrency(" +
                                    "CountryCode varchar(3)," +
                                    "CurrencyCode varchar(3)," +
                                    "Primary Key(CountryCode, CurrencyCode)," +
                                    "Foreign Key(CountryCode) references Country(Alpha3Code)," +
                                    "Foreign Key(CurrencyCode) references Currency(Code))";

                command = new SQLiteCommand(sqlcommand, connection);
                command.ExecuteNonQuery();

                sqlcommand = "CREATE TABLE IF NOT EXISTS Country(" +
                             "Alpha3Code varchar(3) primary key not null, " +
                             "Alpha2Code varchar(2)," +
                             "Name varchar(50), " +
                             "Capital varchar(50), " +
                             "Region varchar(50), " +
                             "SubRegion varchar(50), " +
                             "Population int, " +
                             "GINI real)";

                command = new SQLiteCommand(sqlcommand, connection);
                command.ExecuteNonQuery();

                sqlcommand = "CREATE TABLE IF NOT EXISTS Currency(" +
                             "Code varchar(3) primary key," +
                             "Name char(250), " +
                             "Symbol varchar(3))";

                command = new SQLiteCommand(sqlcommand, connection);
                command.ExecuteNonQuery();

                sqlcommand = "CREATE TABLE IF NOT EXISTS Rate(" +
                             "RateId int primary key, " +
                             "Code varchar(5), " +
                             "TaxRate real, " +
                             "Name varchar(250))";

                command = new SQLiteCommand(sqlcommand, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        #region SAVE

        /// <summary>
        /// Calls specific methods to save information into Database asynchronously.
        /// </summary>
        /// <param name="countries"></param>
        /// <param name="rates"></param>
        /// <param name="progress"></param>
        /// <returns>Task</returns>
        public async Task SaveData(List<Country> countries, List<Rate> rates, IProgress<ProgressReport> progress)
        {
            ProgressReport report = new ProgressReport();

            List<string> ListCurrencyDist = new List<string>();

            try
            {
                await SaveDataRatesAsync(rates, progress);

                foreach (var country in countries)
                {
                    await SaveDataCountryAsync(country);

                    foreach (var currency in country.Currencies)
                    {
                        if (!ListCurrencyDist.Contains(currency.code))
                        {
                            await SaveDataCurrencyAsync(currency);

                            ListCurrencyDist.Add(currency.code);
                        }
                        await SaveDataCountryCurrencyAsync(country, currency);
                    }

                    report.SaveCountries.Add(country);
                    report.PercentageComplete = (report.SaveCountries.Count * 100) / countries.Count;
                    progress.Report(report);
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        /// <summary>
        /// Saves Country's General Information to the Database asynchronously.
        /// </summary>
        /// <param name="country"></param>
        /// <returns>Task</returns>
        private async Task SaveDataCountryAsync(Country country)
        {
            try
            {
                if (country.Gini == null)
                    country.Gini = 0;

                if (country.Name.Contains("'"))
                    country.Name = country.Name.Replace("'", "");
                if (country.Capital.Contains("'"))
                    country.Capital = country.Capital.Replace("'", "");

                string sqlcountries = $"INSERT OR IGNORE INTO Country VALUES ('{country.Alpha3Code}', '{country.Alpha2Code}', '{country.Name}', '{country.Capital}', '{country.Region}', '{country.SubRegion}', {country.Population}, {country.Gini})";// WHERE NOT EXISTS(SELECT 1 FROM Country WHERE Country.Alpha3Code = '{country.Alpha3Code}')";
                command = new SQLiteCommand(sqlcountries, connection);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        /// <summary>
        /// Saves Currency's General Information to the Database asynchronously.
        /// </summary>
        /// <param name="currency"></param>
        /// <returns>Task</returns>
        private async Task SaveDataCurrencyAsync(Currency currency)
        {
            try
            {
                if (currency.name == null)
                    currency.name = string.Empty;
                if (currency.name.Contains("'") && currency.name != null)
                    currency.name = currency.name.Replace("'", "");

                string sqlCurrency = $"INSERT OR IGNORE INTO Currency VALUES ('{currency.code}', '{currency.name}', '{currency.symbol}')";
                command = new SQLiteCommand(sqlCurrency, connection);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        /// <summary>
        /// Intersects and saves Country and Currency's Information matching the Country's Alpha3Code to the Database asynchronously.
        /// </summary>
        /// <param name="country"></param>
        /// <param name="currency"></param>
        /// <returns>Task</returns>
        private async Task SaveDataCountryCurrencyAsync(Country country, Currency currency)
        {
            try
            {
                string sqlcountryCurrency = $"INSERT OR IGNORE INTO CountryCurrency VALUES ('{country.Alpha3Code}', '{currency.code}')";
                command = new SQLiteCommand(sqlcountryCurrency, connection);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        /// <summary>
        /// Saves Rates' General Information to the Database asynchronously.
        /// </summary>
        /// <param name="rates"></param>
        /// <param name="progress"></param>
        /// <returns>Task</returns>
        private async Task SaveDataRatesAsync(List<Rate> rates, IProgress<ProgressReport> progress)
        {
            ProgressReport report = new ProgressReport();

            try
            {
                foreach (var rate in rates)
                {
                    string sqlrates = $"INSERT OR IGNORE INTO Rate VALUES ({rate.RateId}, '{rate.Code}', {rate.TaxRate}, '{rate.Name}')";
                    command = new SQLiteCommand(sqlrates, connection);

                    await Task.Run(() => command.ExecuteNonQuery());

                    report.SaveRates.Add(rate);
                    report.PercentageComplete = (report.SaveRates.Count * 100) / rates.Count;
                    progress.Report(report);
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        /// <summary>
        /// Saves the block of Text using the Wikipedia API in XML format as Text File for each Country asynchronously.
        /// After saving the file, reads it and validates its content.
        /// If not validated, copies from Backup to the main folder.
        /// </summary>
        /// <param name="alpha2Code"></param>
        /// <param name="output"></param>
        /// <returns>Task</returns>
        public async Task SaveWikiTextAsync(string alpha2Code, string output)
        {
            var fileName = Environment.CurrentDirectory + "/WikiText" + $"/{alpha2Code}.txt";
            var pathBackup = Environment.CurrentDirectory + "/WikiTextBackup" + $"/{alpha2Code}.txt";

            if (!Directory.Exists("WikiText"))
            {
                Directory.CreateDirectory("WikiText");
            }

            if (!File.Exists(fileName))
            {
                FileInfo textFile = new FileInfo(fileName);
                string Text = string.Empty;

                try
                {
                    StreamWriter sw = new StreamWriter(fileName, false);

                    if (!File.Exists(fileName))
                    {
                        sw = File.CreateText(fileName);
                    }

                    await Task.Run(() => sw.WriteLine(output));

                    sw.Close();

                    StreamReader sr;

                    if (File.Exists(fileName))
                    {
                        sr = File.OpenText(fileName);

                        string line = string.Empty;

                        while ((line = sr.ReadLine()) != null)
                        {
                            if (alpha2Code == "cg" || alpha2Code == "ge")
                                Text = string.Empty;
                            else
                            {
                                if (!string.IsNullOrEmpty(line))
                                    Text = line;
                            }
                        }

                        if (string.IsNullOrEmpty(Text))
                        {
                            if (File.Exists(pathBackup))
                            {
                                sr.Close();
                                textFile = new FileInfo(pathBackup);
                                File.Delete(fileName);
                                textFile.CopyTo(fileName);
                                sr = File.OpenText(fileName);
                            }
                        }
                        sr.Close();
                    }
                }
                catch
                {
                    if (File.Exists(pathBackup))
                    {
                        textFile = new FileInfo(pathBackup);
                        File.Delete(fileName);
                        textFile.CopyTo(fileName);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Flag Images using from an URL for each Country asynchronously.
        /// </summary>
        /// <param name="countries"></param>
        /// <param name="progress"></param>
        /// <returns>Task</returns>
        public async Task GetFlagsAsync(List<Country> countries, IProgress<ProgressReport> progress)
        {
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

                        if (ct.Alpha3Code.ToLower() != "bra")
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
                        if (File.Exists(pathBackup))
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

        /// <summary>
        /// Gets the Maps Images from an URL for each Country asynchronously.
        /// </summary>
        /// <param name="countries"></param>
        /// <param name="progress"></param>
        /// <returns>Task</returns>
        public async Task GetMapsAsync(List<Country> countries, IProgress<ProgressReport> progress)
        {
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
                        using (WebClient webClient = new WebClient())
                        {
                            //await webClient.DownloadFileTaskAsync("https://github.com/djaiss/mapsicon/tree/master/all/" + $"{ct.Alpha2Code.ToLower()}/128.png", @"Maps2\" + $"{ct.Alpha2Code.ToLower()}.png");

                            //https://github.com/djaiss/mapsicon/tree/master/all/pt/128.png
                            await webClient.DownloadFileTaskAsync("https://www.worldmap1.com/map/" + $"{ct.Name.Replace((' '), ('-')).ToLower()}/" + $"where_is_{ct.Name.Replace((' '), ('_')).ToLower()}_in_the_world.gif", @"Maps\" + $"{ct.Alpha2Code.ToLower()}.gif");
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
                            continue;
                        }
                    }
                    catch
                    {
                        if (File.Exists(pathBackup))
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

        /// <summary>
        /// Gets the Audio from an URL for each Country asynchronously.
        /// </summary>
        /// <param name="countries"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task GetAudioAsync(List<Country> countries, IProgress<ProgressReport> progress)
        {
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
                        if (File.Exists(pathBackup))
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

        #endregion SAVE

        #region LOAD

        /// <summary>
        /// Loads Country's General Information from Database asynchronously.
        /// </summary>
        /// <param name="progress"></param>
        /// <returns>List</returns>
        public async Task<List<Country>> LoadCountryDataAsync(IProgress<ProgressReport> progress)
        {
            List<Country> Countries = new List<Country>();
            ProgressReport report = new ProgressReport();

            try
            {
                string sql = "SELECT Alpha3Code, Alpha2Code, Country.Name, Capital, Region, SubRegion, Population, Gini FROM Country";

                command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (await Task.Run(() => reader.Read()))
                {
                    await Task.Run(() => Countries.Add(new Country
                    {
                        Alpha3Code = (string)reader["Alpha3Code"],
                        Alpha2Code = (string)reader["Alpha2Code"],
                        Name = (string)reader["Name"],
                        Capital = (string)reader["Capital"],
                        Region = (string)reader["Region"],
                        SubRegion = (string)reader["SubRegion"],
                        Population = (int)reader["Population"],
                        Gini = (double?)reader["Gini"],
                    }));
                }

                foreach (var country in Countries)
                {
                    country.Currencies = (await LoadCurrencyData(country.Alpha3Code));
                }

                report.SaveCountries = Countries;
                report.PercentageComplete = (report.SaveCountries.Count * 100) / Countries.Count;
                progress.Report(report);

                return Countries;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Loads Currency's General Information from Database asynchronously.
        /// </summary>
        /// <param name="alpha3code"></param>
        /// <returns>List</returns>
        public async Task<List<Currency>> LoadCurrencyData(string alpha3code)
        {
            List<Currency> Currencies = new List<Currency>();

            try
            {
                string sql = "SELECT Currency.Code, Currency.Name, Currency.Symbol FROM Currency " +
                    "INNER JOIN CountryCurrency ON CountryCurrency.CurrencyCode = Currency.Code " +
                    $"WHERE CountryCurrency.CountryCode = '{alpha3code}'";

                command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (await Task.Run(() => reader.Read()))
                {
                    await Task.Run(() => Currencies.Add(new Currency
                    {
                        code = (string)reader["code"],
                        name = (string)reader["name"],
                        symbol = (string)reader["symbol"]
                    }));
                }

                return Currencies;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Loads Rates' General Information from Database asynchronously.
        /// </summary>
        /// <returns>List</returns>
        public async Task<List<Rate>> LoadRatesData()
        {
            List<Rate> Rates = new List<Rate>();

            try
            {
                string sql = "SELECT RateId, Code, TaxRate, Name FROM Rate";

                command = new SQLiteCommand(sql, connection);

                SQLiteDataReader reader = command.ExecuteReader();

                while (await Task.Run(() => reader.Read()))
                {
                    await Task.Run(() => Rates.Add(new Rate
                    {
                        RateId = (int)reader["RateId"],
                        Code = (string)reader["Code"],
                        TaxRate = Convert.ToDouble((double)reader["TaxRate"]),
                        Name = (string)reader["Name"]
                    }));
                }

                connection.Close();

                return Rates;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Loads the text file so it can be displayed.
        /// </summary>
        /// <param name="alpha2Code"></param>
        /// <returns></returns>
        public string LoadWikiText(string alpha2Code)
        {
            try
            {
                string file = @"WikiText\" + $"{alpha2Code}.txt";

                string Text = string.Empty;

                StreamReader sr;

                if (File.Exists(file))
                {
                    sr = File.OpenText(file);

                    string line = string.Empty;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                            Text = line;
                    }

                    sr.Close();
                }
                return Text;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

        #endregion LOAD

        /// <summary>
        /// Deletes the Table in the Database and all its content asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task DeleteDataAsync()
        {
            try
            {
                string sql = "DELETE FROM Country";
                command = new SQLiteCommand(sql, connection);
                await command.ExecuteNonQueryAsync();

                sql = "DELETE FROM Currency";
                command = new SQLiteCommand(sql, connection);
                await command.ExecuteNonQueryAsync();

                sql = "DELETE FROM CountryCurrency";
                command = new SQLiteCommand(sql, connection);
                await command.ExecuteNonQueryAsync();

                sql = "DELETE FROM Rate";
                command = new SQLiteCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }
    }
}
