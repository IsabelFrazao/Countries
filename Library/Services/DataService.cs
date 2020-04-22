using Library;
using Library.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Services
{
    public class DataService //Criar uma Base de Dados a partir dos dados da Api
    {
        private SQLiteConnection connection;

        private SQLiteCommand command;

        private DialogService dialogService;

        public DataService() //Creating the DataBase and the Table on the Database
        {
            dialogService = new DialogService();

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
                                    "primary key(CountryCode, CurrencyCode))";

                command = new SQLiteCommand(sqlcommand, connection);
                command.ExecuteNonQuery();

                sqlcommand = "CREATE TABLE IF NOT EXISTS Country(" +
                             "Alpha3Code varchar(3) primary key not null, " +
                             "Name varchar(50), " +
                             "Capital varchar(50), " +
                             "Region varchar(50), " +
                             "SubRegion varchar(50), " +
                             "Population int, " +
                             "GINI real, " +
                             "Flag varchar(10)," +
                             "FOREIGN KEY (Alpha3Code) REFERENCES CountryCurrency(CountryCode))";

                command = new SQLiteCommand(sqlcommand, connection);
                command.ExecuteNonQuery();

                sqlcommand = "CREATE TABLE IF NOT EXISTS Currency(" +
                             "Code varchar(3) primary key," +
                             "Name char(250), " +
                             "Symbol varchar(3)," +
                             "foreign key (Code) references CountryCurrency(CurrencyCode))";

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

        public async Task SaveData(List<Country> countries, List<Rate> rates)
        {
            var pointCulture = new CultureInfo("en")
            {
                NumberFormat = { NumberDecimalSeparator = "." }
            };

            Thread.CurrentThread.CurrentCulture = pointCulture;

            try
            {
                await SaveDataCountry(countries);

                await SaveDataCurrency(countries);

                await SaveDataCountryCurrency(countries);

                await SaveDataRates(rates);

                connection.Close();
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        private async Task SaveDataCountry(List<Country> countries)
        {
            try
            {
                foreach (var country in countries)
                {
                    var countryFlag = $"{country.Flag.Split('/')[4].Split('.')[0]}.jpg";

                    if (country.Gini == null)
                        country.Gini = 0;

                    if (country.Name.Contains("'"))
                        country.Name = country.Name.Replace("'", "");
                    if (country.Capital.Contains("'"))
                        country.Capital = country.Capital.Replace("'", "");

                    string sqlcountries = $"INSERT INTO Country VALUES ('{country.Alpha3Code}', '{country.Name}', '{country.Capital}', '{country.Region}', '{country.SubRegion}', {country.Population}, {country.Gini}, '{countryFlag}')";
                    command = new SQLiteCommand(sqlcountries, connection);

                    await Task.Run(() => command.ExecuteNonQueryAsync());
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        private async Task SaveDataCurrency(List<Country> countries)
        {
            try
            {


                List<string> ListCurrencyDist = new List<string>();

                foreach (var ct in countries)
                {
                    foreach (var currency in ct.Currencies)
                    {
                        if (currency.code == null)
                            currency.code = string.Empty;
                        if (currency.code.Contains("'") && currency.code != null)
                            currency.code = currency.code.Replace("'", "");
                        if (currency.name == null)
                            currency.name = string.Empty;
                        if (currency.name.Contains("'") && currency.name != null)
                            currency.name = currency.name.Replace("'", "");
                        if (currency.symbol == null)
                            currency.symbol = string.Empty;
                        if (currency.symbol.Contains("'") && currency.symbol != null)
                            currency.symbol = currency.symbol.Replace("'", "");

                        if (!ListCurrencyDist.Contains(currency.code))
                        {
                            string sqlCurrency = $"INSERT INTO Currency VALUES ('{currency.code}', '{currency.name}', '{currency.symbol}')";
                            command = new SQLiteCommand(sqlCurrency, connection);

                            await Task.Run(() => command.ExecuteNonQueryAsync());

                            ListCurrencyDist.Add(currency.code);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        private async Task SaveDataCountryCurrency(List<Country> countries)
        {
            try
            {
                foreach (var country in countries)
                {
                    foreach (var ct in country.Currencies)
                    {
                        string sqlcountryCurrency = $"INSERT INTO CountryCurrency VALUES ('{country.Alpha3Code}', '{ct.code}')";
                        command = new SQLiteCommand(sqlcountryCurrency, connection);

                        await Task.Run(() => command.ExecuteNonQueryAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        private async Task SaveDataRates(List<Rate> rates)
        {
            try
            {
                foreach (var rate in rates)
                {
                    string sqlrates = $"INSERT INTO Rate VALUES ({rate.RateId}, '{rate.Code}', {rate.TaxRate}, '{rate.Name}')";
                    command = new SQLiteCommand(sqlrates, connection);

                    await Task.Run(() => command.ExecuteNonQueryAsync());
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        public List<Country> GetCountryData()
        {
            List<Country> Countries = new List<Country>();

            try
            {
                string sql = "SELECT Name, Capital, Region, SubRegion, Population, Gini, Flag FROM Country";

                command = new SQLiteCommand(sql, connection);

                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Countries.Add(new Country
                    {
                        Name = (string)reader["Name"],
                        Capital = (string)reader["Capital"],
                        Region = (string)reader["Region"],
                        SubRegion = (string)reader["SubRegion"],
                        Population = (int)reader["Population"],
                        Gini = (double)reader["Gini"],
                        Flag = (string)reader["Flag"]
                    });
                }
                connection.Close();

                return Countries;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

        public List<Currency> GetCurrencyData()
        {
            List<Currency> Currencies = new List<Currency>();

            try
            {
                string sql = "SELECT Code, Name, Symbol FROM Currency";

                command = new SQLiteCommand(sql, connection);

                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Currencies.Add(new Currency
                    {
                        code = (string)reader["Code"],
                        name = (string)reader["Name"],
                        symbol = (string)reader["symbol"]
                    });
                }
                connection.Close();

                return Currencies;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

        public async Task DeleteData()
        {
            try
            {
                string sql = "DELETE FROM Country";
                command = new SQLiteCommand(sql, connection);

                sql = "DELETE FROM Currency";
                command = new SQLiteCommand(sql, connection);

                sql = "DELETE FROM CountryCurrency";
                command = new SQLiteCommand(sql, connection);
                
                sql = "DELETE FROM Rate";
                command = new SQLiteCommand(sql, connection);

                await Task.Run(() => command.ExecuteNonQueryAsync());
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }
    }
}
