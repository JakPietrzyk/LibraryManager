using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManager.Dtos
{
    public class RentalDto
    {
        public int Id { get; set; }
        public string Nazwisko { get; set; }
        public string Imie { get; set; }
        public string Tytul { get; set; }
        public DateTime DataWypozyczenia { get; set; }
        public DateTime DataZwrotu { get; set; }
        public DateTime RokWydania { get; set; }
    }

}
