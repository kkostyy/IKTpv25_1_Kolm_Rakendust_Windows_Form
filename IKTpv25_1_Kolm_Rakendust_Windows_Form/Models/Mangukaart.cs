using System.Drawing;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Mida kaart näitab: kas Wingdings sümbolit või pilti. Voti on paari võrdlemiseks.
    public class KaardiSisu
    {
        public char Sumbol { get; }
        public Bitmap Pilt { get; }
        public string Voti { get; }

        public KaardiSisu(char sumbol)
        {
            Sumbol = sumbol;
            Voti = "S:" + sumbol;
        }

        public KaardiSisu(Bitmap pilt, int number)
        {
            Pilt = pilt;
            Voti = "I:" + number;
        }
    }

    public class Mangukaart
    {
        public KaardiSisu Sisu { get; set; }
        public Button Nupp { get; }
        public bool OnPaaritud { get; set; }

        public Mangukaart(KaardiSisu sisu, Button nupp)
        {
            Sisu = sisu;
            Nupp = nupp;
            OnPaaritud = false;
        }

        public void NaitaSisu()
        {
            if (Sisu.Pilt != null)
            {
                Nupp.BackgroundImage = Sisu.Pilt;
                Nupp.BackgroundImageLayout = ImageLayout.Zoom;
                Nupp.Text = string.Empty;
            }
            else
            {
                Nupp.Text = Sisu.Sumbol.ToString();
            }
        }

        public void Peida()
        {
            if (!OnPaaritud)
            {
                TuhjendaNupp();
            }
        }

        public void MarkeeriPaaritud()
        {
            OnPaaritud = true;
            Nupp.BackColor = Color.LightGreen;
            Nupp.ForeColor = Color.FromArgb(40, 40, 40);
        }

        // Uue mängu jaoks: sama nupp, aga kaart on jälle peidus ja paaritamata
        public void Lahtesta()
        {
            OnPaaritud = false;
            TuhjendaNupp();
            Nupp.BackColor = UiStiil.Sinine;
            Nupp.ForeColor = Color.White;
        }

        private void TuhjendaNupp()
        {
            Nupp.Text = string.Empty;
            Nupp.BackgroundImage = null;
        }
    }
}
