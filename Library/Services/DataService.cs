namespace Services
{
    public class DataService //Criar uma Base de Dados a partir dos dados da Api
    {
        /*private SQLiteConnection connection;

        private SQLiteCommand command;

        private DialogService dialogService;

        public DataService()
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

                string sqlcommand = "create table if not exists Countries(Name varchar(250), Capital varchar(250), Region varchar(250), SubRegion varchar(250), Population real, GINI real, Flag varchar(250))";

                command = new SQLiteCommand(sqlcommand, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Erro", ex.Message);
            }
        }

        public void SaveData(List<RootObject> Countries)
        {
            try
            {
                foreach (var country in Countries)
                {
                    string sql = string.Format("insert into Countries (RateId, Code, TaxRate, Name) values({0}, '{1}', '{2}', '{3}')", rate.RateId, rate.Code, rate.TaxRate, rate.Name);

                    command = new SQLiteCommand(sql, connection);

                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Erro", ex.Message);
            }
        }

        public List<Rate> GetData()
        {
            List<Rate> Rates = new List<Rate>();

            try
            {
                string sql = "select RateId,Code, taxRate, Name from Rates";

                command = new SQLiteCommand(sql, connection);

                //Lê cada registo(linha)
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Rates.Add(new Rate
                    {
                        RateId = (int)reader["RateId"],
                        Code = (string)reader["Code"],
                        TaxRate = (double)reader["TaxRate"],
                        Name = (string)reader["Name"]
                    });
                }
                connection.Close();

                return Rates;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Erro", ex.Message);
                return null;
            }
        }

        public void DeleteData()
        {
            try
            {
                string sql = "delete from Rates";

                command = new SQLiteCommand(sql, connection);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage("Erro", ex.Message);
            }
        }*/
    }
}
