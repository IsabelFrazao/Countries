using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public class Stats
    {
        public int elapsed_time { get; set; }
        public int nb_characters { get; set; }
        public int nb_tokens { get; set; }
        public int nb_tus { get; set; }
        public int nb_tus_failed { get; set; }
    }
}
