using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZakupkiGov.Model
{
    public class Zakupka
    {
        public string Number { get; private set; }

        public string Info { get; set; }
        public string NameSite { get; internal set; }
        public string StartMaxPrice { get; internal set; }
        public string Place { get; internal set; }

        public string EndDate { get; internal set; }
        public string AuctionDate { get; internal set; }

        public List<ZakupkaFile> Files { get; private set; }

        public string Directory { get; internal set; }

        public Zakupka(string number)
        {
            Number = number;
            Files = new List<ZakupkaFile>();
        }
    }
}
