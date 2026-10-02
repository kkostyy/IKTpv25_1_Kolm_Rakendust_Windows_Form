using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Loeb ja kirjutab seaded.json faili kausta %AppData%\KolmRakendust
    public static class SeadedHoidla
    {
        private static readonly string Kaust = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KolmRakendust");

        private static readonly string Fail = Path.Combine(Kaust, "seaded.json");

        private static readonly JsonSerializerOptions Valikud = new JsonSerializerOptions { WriteIndented = true };

        private static Seaded seaded;

        public static Seaded Praegune
        {
            get
            {
                if (seaded == null)
                {
                    Lae();
                }
                return seaded;
            }
        }

        private static void Lae()
        {
            try
            {
                if (File.Exists(Fail))
                {
                    seaded = JsonSerializer.Deserialize<Seaded>(File.ReadAllText(Fail));
                }
            }
            catch (Exception)
            {
                // Katkine fail: alustame puhtalt lehelt
                seaded = null;
            }

            if (seaded == null)
            {
                seaded = new Seaded();
            }

            if (seaded.ParimadAjad == null) seaded.ParimadAjad = new Dictionary<string, int>();
            if (seaded.Edetabel == null) seaded.Edetabel = new List<MangutulemusKirje>();
            if (seaded.ViktoriiniAjalugu == null) seaded.ViktoriiniAjalugu = new List<ViktoriiniTulemus>();
            if (seaded.ViimasedFailid == null) seaded.ViimasedFailid = new List<string>();
            if (seaded.MangupildiFailid == null) seaded.MangupildiFailid = new List<string>();
            if (seaded.ViimaneNimi == null) seaded.ViimaneNimi = string.Empty;
        }

        public static void Salvesta()
        {
            try
            {
                Directory.CreateDirectory(Kaust);
                File.WriteAllText(Fail, JsonSerializer.Serialize(Praegune, Valikud));
            }
            catch (Exception)
            {
                // Seadete salvestamine ei tohi programmi kokku jooksutada
            }
        }
    }
}
