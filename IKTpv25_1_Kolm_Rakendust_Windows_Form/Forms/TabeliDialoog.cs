using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Ühine tabeliaken: sobitusmängu edetabel ja viktoriini tulemuste ajalugu
    public class TabeliDialoog : Form
    {
        public TabeliDialoog(string pealkiri, string[] veerud, IList<string[]> read, Action tyhjenda)
        {
            UiStiil.SeadistaSkaleerimine(this);
            Text = pealkiri;
            Font = UiStiil.Põhifont;
            ClientSize = new Size(520, 360);
            MinimumSize = new Size(360, 260);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            var vaade = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false
            };

            foreach (string veerg in veerud)
            {
                vaade.Columns.Add(veerg, 80);
            }
            foreach (string[] rida in read)
            {
                vaade.Items.Add(new ListViewItem(rida));
            }

            // Veeru laius = pikim tekst veerus (päis või sisu)
            for (int c = 0; c < veerud.Length; c++)
            {
                int laius = TextRenderer.MeasureText(veerud[c], Font).Width;
                foreach (string[] rida in read)
                {
                    laius = Math.Max(laius, TextRenderer.MeasureText(rida[c], Font).Width);
                }
                vaade.Columns[c].Width = laius + 28;
            }

            var alumine = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(6),
                BackColor = UiStiil.PaneeliTaust
            };

            Button sulge = UiStiil.LooNupp("Sulge", (s, e) => Close());
            alumine.Controls.Add(sulge);

            if (tyhjenda != null)
            {
                Button tuhjenda = UiStiil.LooNupp("Tühjenda", (s, e) =>
                {
                    if (vaade.Items.Count == 0)
                    {
                        return;
                    }
                    DialogResult vastus = MessageBox.Show(this, "Kustutada kõik read?", pealkiri,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (vastus == DialogResult.Yes)
                    {
                        tyhjenda();
                        vaade.Items.Clear();
                    }
                });
                alumine.Controls.Add(tuhjenda);
            }

            CancelButton = sulge;

            // Fill-juhtelement lisatakse esimesena, et ääred (Bottom) jaotataks enne
            Controls.Add(vaade);
            Controls.Add(alumine);
        }
    }
}
