using System.Drawing;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Väike aken nime küsimiseks (edetabelisse kandmisel)
    public class NimeDialoog : Form
    {
        private readonly TextBox nimiKast;

        private NimeDialoog(string pealkiri, string tekst, string vaikimisi)
        {
            UiStiil.SeadistaSkaleerimine(this);
            Text = pealkiri;
            Font = UiStiil.Põhifont;
            ClientSize = new Size(320, 120);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiStiil.PaneeliTaust;

            var silt = new Label
            {
                Text = tekst,
                AutoSize = false,
                Bounds = new Rectangle(12, 12, 296, 22)
            };

            nimiKast = new TextBox
            {
                Text = vaikimisi,
                MaxLength = 20,
                Bounds = new Rectangle(12, 38, 296, 24)
            };

            Button ok = UiStiil.LooNupp("OK", null, true);
            ok.AutoSize = false;
            ok.Bounds = new Rectangle(132, 76, 84, 32);
            ok.DialogResult = DialogResult.OK;

            Button tuhista = UiStiil.LooNupp("Jäta vahele", null);
            tuhista.AutoSize = false;
            tuhista.Bounds = new Rectangle(224, 76, 84, 32);
            tuhista.DialogResult = DialogResult.Cancel;

            AcceptButton = ok;
            CancelButton = tuhista;

            Controls.Add(silt);
            Controls.Add(nimiKast);
            Controls.Add(ok);
            Controls.Add(tuhista);

            Shown += (s, e) =>
            {
                nimiKast.Focus();
                nimiKast.SelectAll();
            };
        }

        // Tagastab sisestatud nime või null, kui kasutaja jättis vahele
        public static string Kysi(IWin32Window omanik, string pealkiri, string tekst, string vaikimisi)
        {
            using (var dialoog = new NimeDialoog(pealkiri, tekst, vaikimisi))
            {
                if (dialoog.ShowDialog(omanik) != DialogResult.OK)
                {
                    return null;
                }
                string nimi = dialoog.nimiKast.Text.Trim();
                return nimi.Length == 0 ? null : nimi;
            }
        }
    }
}
