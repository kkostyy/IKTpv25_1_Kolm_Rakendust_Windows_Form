using System;

namespace Naidis_IKTpv25_Windows_Forms
{
    public class MatemaatikaUlesanne
    {
        // Põhitehted ja laiendatud komplekt (jääk ja astendamine)
        public static readonly char[] PohiTehted = { '+', '-', '×', '÷' };
        public static readonly char[] LaiendatudTehted = { '+', '-', '×', '÷', '%', '^' };

        private static readonly Random rnd = new Random();

        public int Arv1 { get; private set; }
        public int Arv2 { get; private set; }
        public char Tehe { get; }
        public int OigeVastus { get; private set; }
        public Raskusaste Aste { get; set; }

        public MatemaatikaUlesanne(char tehe, Raskusaste aste = Raskusaste.Keskmine)
        {
            Tehe = tehe;
            Aste = aste;
            GenereeriUuesti();
        }

        // Arvude vahemikud sõltuvad raskusastmest
        public void GenereeriUuesti()
        {
            int liitMin, liitMax, lahMin, lahMax, korMin, korMax;
            switch (Aste)
            {
                case Raskusaste.Lihtne:
                    liitMin = 1; liitMax = 20;
                    lahMin = 10; lahMax = 30;
                    korMin = 2; korMax = 5;
                    break;
                case Raskusaste.Raske:
                    liitMin = 50; liitMax = 200;
                    lahMin = 100; lahMax = 500;
                    korMin = 6; korMax = 15;
                    break;
                default:
                    liitMin = 10; liitMax = 50;
                    lahMin = 20; lahMax = 90;
                    korMin = 2; korMax = 9;
                    break;
            }

            switch (Tehe)
            {
                case '+':
                    Arv1 = rnd.Next(liitMin, liitMax + 1);
                    Arv2 = rnd.Next(liitMin, liitMax + 1);
                    OigeVastus = Arv1 + Arv2;
                    break;

                case '-':
                    Arv1 = rnd.Next(lahMin, lahMax + 1);
                    Arv2 = rnd.Next(1, Arv1);
                    OigeVastus = Arv1 - Arv2;
                    break;

                case '×':
                    Arv1 = rnd.Next(korMin, korMax + 1);
                    Arv2 = rnd.Next(korMin, korMax + 1);
                    OigeVastus = Arv1 * Arv2;
                    break;

                case '÷':
                    Arv2 = rnd.Next(korMin, korMax + 1);
                    OigeVastus = rnd.Next(korMin, korMax + 1);
                    Arv1 = Arv2 * OigeVastus;
                    break;

                case '%':
                    {
                        // Jääk jagamisel: jääk on alati vähemalt 1, et vastus ei oleks vaikimisi 0
                        Arv2 = rnd.Next(korMin, korMax + 1);
                        int jagatis = rnd.Next(korMin, korMax + 1);
                        int jaak = rnd.Next(1, Arv2);
                        Arv1 = Arv2 * jagatis + jaak;
                        OigeVastus = Arv1 % Arv2;
                        break;
                    }

                case '^':
                    {
                        // Astendamine: tulemus mahub alati vastuseväljale (kuni 729)
                        int alusMin = Aste == Raskusaste.Raske ? 3 : 2;
                        int alusMax = Aste == Raskusaste.Lihtne ? 5 : 9;
                        Arv1 = rnd.Next(alusMin, alusMax + 1);
                        Arv2 = Aste == Raskusaste.Raske ? 3 : 2;
                        OigeVastus = (int)Math.Pow(Arv1, Arv2);
                        break;
                    }

                default:
                    throw new ArgumentException("Tundmatu tehe: " + Tehe);
            }
        }

        public bool KontrolliVastust(int vastus) => vastus == OigeVastus;
    }
}
