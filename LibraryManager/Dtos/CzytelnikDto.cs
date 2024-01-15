using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManager.Dtos
{
    public class CzytelnikDto
    {
        public int Id { get; set; }
        public string Imie { get; set; }
        public string Nazwisko { get; set; }
        public string Adres { get; set; }
        public string Email { get; set; }
        public string Telefon { get; set; }
        public long IloscWypozyczen { get; set; }
        public string PelneImieNazwisko { get; set; }

    }
}
