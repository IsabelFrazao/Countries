using Library;
using Library.Models;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
        public DataService() //Creating the DataBase and the Table on the Database
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

        /// <summary>
        /// Calls specific methods to save information into Database
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

                await SaveDataRatesAsync(rates, progress);
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        /// <summary>
        /// Saves Country's General Information to the Database
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
        /// Saves Currency's General Information to the Database
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
        /// Intersects and saves Country and Currency's Information matching the Country's Alpha3Code to the Database
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
        /// Saves Rates' General Information to the Database
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
        /// Loads Country's General Information from Database
        /// </summary>
        /// <param name="progress"></param>
        /// <returns>List</returns>
        public List<Country> GetCountryDataAsync(IProgress<ProgressReport> progress)
        {
            List<Country> Countries = new List<Country>();
            ProgressReport report = new ProgressReport();

            try
            {
                string sql = "SELECT Alpha3Code, Alpha2Code, Country.Name, Capital, Region, SubRegion, Population, Gini FROM Country";

                command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Countries.Add(new Country
                    {
                        Alpha3Code = (string)reader["Alpha3Code"],
                        Alpha2Code = (string)reader["Alpha2Code"],
                        Name = (string)reader["Name"],
                        Capital = (string)reader["Capital"],
                        Region = (string)reader["Region"],
                        SubRegion = (string)reader["SubRegion"],
                        Population = (int)reader["Population"],
                        Gini = (double?)reader["Gini"],
                    });
                }

                foreach (var country in Countries)
                {
                    country.Currencies = (GetCurrencyData(country.Alpha3Code));
                }

                GetRatesData();

                report.SaveCountries = Countries;
                report.PercentageComplete = (report.SaveCountries.Count * 100) / Countries.Count;
                progress.Report(report);

                connection.Close();

                return Countries;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Loads Currency's General Information from Database
        /// </summary>
        /// <param name="alpha3code"></param>
        /// <returns>List</returns>
        public List<Currency> GetCurrencyData(string alpha3code)
        {
            List<Currency> Currencies = new List<Currency>();

            try
            {
                string sql = "SELECT Currency.Code, Currency.Name, Currency.Symbol FROM Currency " +
                    "INNER JOIN CountryCurrency ON CountryCurrency.CurrencyCode = Currency.Code " +
                    $"WHERE CountryCurrency.CountryCode = '{alpha3code}'";

                command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Currencies.Add(new Currency
                    {
                        code = (string)reader["code"],
                        name = (string)reader["name"],
                        symbol = (string)reader["symbol"]
                    });
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
        /// Loads Rates' General Information from Database
        /// </summary>
        /// <returns>List</returns>
        public List<Rate> GetRatesData()
        {
            List<Rate> Rates = new List<Rate>();

            try
            {
                string sql = "SELECT RateId, Code, TaxRate, Name FROM Rate";

                command = new SQLiteCommand(sql, connection);

                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Rates.Add(new Rate
                    {
                        RateId = (int)reader["RateId"],
                        Code = (string)reader["Code"],
                        TaxRate = Convert.ToDouble((double)reader["TaxRate"]),
                        Name = (string)reader["Name"]
                    });
                }

                return Rates;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Deletes the Table in the Database and all its content
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
