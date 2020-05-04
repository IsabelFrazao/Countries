using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public class ProgressReport
    {
        public int PercentageComplete { get; set; } = 0;

        public List<Country> SaveCountries { get; set; } = new List<Country>();

        public List<Rate> SaveRates { get; set; } = new List<Rate>();
    }
}
