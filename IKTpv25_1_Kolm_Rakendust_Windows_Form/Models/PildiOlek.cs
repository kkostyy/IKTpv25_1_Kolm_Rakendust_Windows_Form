using System;
using System.Drawing;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Pildi olek Undo jaoks: taustapilt + joonistuskiht + mustvalge filtri seisund.
    // Kui Taust/Joonis on null, muutus oli ainult filtris ja pildikihte ei kopeerita (säästab mälu).
    public class PildiOlek : IDisposable
    {
        public Bitmap Taust { get; }
        public Bitmap Joonis { get; }
        public bool Halltoon { get; }

        public PildiOlek(Bitmap taust, Bitmap joonis, bool halltoon)
        {
            Taust = taust != null ? new Bitmap(taust) : null;
            Joonis = joonis != null ? new Bitmap(joonis) : null;
            Halltoon = halltoon;
        }

        public static PildiOlek AinultFilter(bool halltoon)
        {
            return new PildiOlek(null, null, halltoon);
        }

        public void Dispose()
        {
            Taust?.Dispose();
            Joonis?.Dispose();
        }
    }
}
