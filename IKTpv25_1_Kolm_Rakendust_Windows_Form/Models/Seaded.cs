using System;
using System.Collections.Generic;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Kõik, mis salvestatakse failina (JSON): parimad ajad, edetabel, viktoriini ajalugu jm
    public class Seaded
    {
        // Võti on mänguvälja suurus, nt "4x4" või "6x6"; väärtus on parim aeg sekundites
        public Dictionary<string, int> ParimadAjad { get; set; } = new Dictionary<string, int>();
        public List<MangutulemusKirje> Edetabel { get; set; } = new List<MangutulemusKirje>();
        public List<ViktoriiniTulemus> ViktoriiniAjalugu { get; set; } = new List<ViktoriiniTulemus>();
        public List<string> ViimasedFailid { get; set; } = new List<string>();
        public List<string> MangupildiFailid { get; set; } = new List<string>();
        public bool Heli { get; set; } = true;
        public string ViimaneNimi { get; set; } = string.Empty;
    }

    public class MangutulemusKirje
    {
        public string Nimi { get; set; } = string.Empty;
        public string Suurus { get; set; } = "4x4";
        public int Sekundid { get; set; }
        public int Kaike { get; set; }
        public DateTime Kuupaev { get; set; }
    }

    public class ViktoriiniTulemus
    {
        public DateTime Kuupaev { get; set; }
        public string Aste { get; set; } = string.Empty;
        public string Tehted { get; set; } = string.Empty;
        public int Oigeid { get; set; }
        public int Kokku { get; set; }
        public int Sekundid { get; set; }
    }
}
