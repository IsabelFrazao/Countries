﻿using Library;
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
    public class DataService //Criar uma Base de Dados a partir dos dados da Api
    {
        private SQLiteConnection connection;

        private SQLiteCommand command;

        private DialogService dialogService;

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
                             "Name varchar(50), " +
                             "Capital varchar(50), " +
                             "Region varchar(50), " +
                             "SubRegion varchar(50), " +
                             "Population int, " +
                             "GINI real, " +
                             "Flag varchar(100))";

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

        public async Task SaveData(List<Country> countries, List<Rate> rates)
        {
            try
            {
                List<string> ListCurrencyDist = new List<string>();

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
                }

                await SaveDataRatesAsync(rates);

                connection.Close();
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

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

                string sqlcountries = $"INSERT INTO Country VALUES ('{country.Alpha3Code}', '{country.Name}', '{country.Capital}', '{country.Region}', '{country.SubRegion}', {country.Population}, {country.Gini}, '{country.Flag}')";
                command = new SQLiteCommand(sqlcountries, connection);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (SqlException ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        private async Task SaveDataCurrencyAsync(Currency currency)
        {
            try
            {
                if (currency.name == null)
                    currency.name = string.Empty;
                if (currency.name.Contains("'") && currency.name != null)
                    currency.name = currency.name.Replace("'", "");

                string sqlCurrency = $"INSERT INTO Currency VALUES ('{currency.code}', '{currency.name}', '{currency.symbol}')";
                command = new SQLiteCommand(sqlCurrency, connection);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (SqlException ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        private async Task SaveDataCountryCurrencyAsync(Country country, Currency currency)
        {
            try
            {
                string sqlcountryCurrency = $"INSERT INTO CountryCurrency VALUES ('{country.Alpha3Code}', '{currency.code}')";
                command = new SQLiteCommand(sqlcountryCurrency, connection);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (SqlException ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        private async Task SaveDataRatesAsync(List<Rate> rates)
        {
            try
            {
                foreach (var rate in rates)
                {
                    string sqlrates = $"INSERT INTO Rate VALUES ({rate.RateId}, '{rate.Code}', {rate.TaxRate}, '{rate.Name}')";
                    command = new SQLiteCommand(sqlrates, connection);

                    await Task.Run(() => command.ExecuteNonQuery());
                }
            }
            catch (SqlException ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }

        public List<Country> GetCountryDataAsync()
        {
            List<Country> Countries = new List<Country>();

            try
            {
                string sql = "SELECT Alpha3Code, Country.Name, Capital, Region, SubRegion, Population, Gini, Flag FROM Country";

                command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Countries.Add(new Country
                    {
                        Alpha3Code = (string)reader["Alpha3Code"],
                        Name = (string)reader["Name"],
                        Capital = (string)reader["Capital"],
                        Region = (string)reader["Region"],
                        SubRegion = (string)reader["SubRegion"],
                        Population = (int)reader["Population"],
                        Gini = (double)reader["Gini"],
                        Flag = (string)reader["Flag"],
                    });
                }

                foreach (var country in Countries)
                {
                    country.Currencies = (GetCurrencyData(country.Alpha3Code));
                }

                connection.Close();

                return Countries;
            }
            catch (SqlException ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

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
            catch (SqlException ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
                return null;
            }
        }

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
            catch (SqlException ex)
            {
                dialogService.ShowMessage("Error", ex.Message);
            }
        }
    }
}
