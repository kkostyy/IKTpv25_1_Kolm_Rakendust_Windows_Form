using System.Drawing.Imaging;

namespace Naidis_IKTpv25_Windows_Forms
{
    public static class PildiTootlus
    {
        // Kiire halltoonide maatriks (ColorMatrix): kasutatakse kuvamisel ja salvestamisel,
        // pilt ise jääb värviliseks, seega filtri saab igal ajal välja lülitada
        public static ColorMatrix HalltoonideMaatriks()
        {
            return new ColorMatrix(new float[][]
            {
                new float[] { 0.30f, 0.30f, 0.30f, 0, 0 },
                new float[] { 0.59f, 0.59f, 0.59f, 0, 0 },
                new float[] { 0.11f, 0.11f, 0.11f, 0, 0 },
                new float[] { 0,     0,     0,     1, 0 },
                new float[] { 0,     0,     0,     0, 1 }
            });
        }
    }
}
