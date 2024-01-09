using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManager.Dtos
{
    public class KsiazkaDto
    {
        public int Id { get; set; }
        public string Tytul { get; set; }
        public DateTime RokWydania { get; set; }
        public AutorDto Autor { get; set; }
        public decimal Opinia { get; set; }
    }
}
