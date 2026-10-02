using System;
using System.Drawing;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Peamenüü "Kolm rakendust": kolm nuppu avavad vormid.
    // Kiirklahvid: 1, 2, 3 avavad rakendused, Esc sulgeb menüü.
    // Välimus on võetud UiStiil klassist, et kõik aknad näeksid ühtsed välja.
    public class MainMenuForm : Form
    {
        public MainMenuForm()
        {
            UiStiil.SeadistaSkaleerimine(this);
            Text = "Kolm rakendust";
            ClientSize = new Size(320, 320);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = UiStiil.PaneeliTaust;
            Font = UiStiil.Põhifont;

            // KeyPreview: klahvivajutused jõuavad vormini enne nuppe
            KeyPreview = true;
            KeyDown += MainMenuForm_KeyDown;

            var title = new Label
            {
                Text = "Kolm rakendust",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = UiStiil.Sinine,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 15, ClientSize.Width, 40)
            };

            var group = new GroupBox
            {
                Text = "Vali rakendus",
                Bounds = new Rectangle(20, 65, 280, 185),
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            group.Controls.Add(MakeButton("1. Pildivaataja", 30, (s, e) => Open(new PildiVaatajaForm())));
            group.Controls.Add(MakeButton("2. Matemaatikaviktoriin", 80, (s, e) => Open(new MathQuizForm())));
            group.Controls.Add(MakeButton("3. Sobitusmäng", 130, (s, e) => Open(new MatchingGameForm())));

            Button exitButton = UiStiil.LooNupp("Välju (Esc)", (s, e) => Close());
            exitButton.AutoSize = false;
            exitButton.Bounds = new Rectangle(20, 266, 280, 38);

            Controls.Add(title);
            Controls.Add(group);
            Controls.Add(exitButton);
        }

        private static Button MakeButton(string text, int top, EventHandler onClick)
        {
            Button btn = UiStiil.LooNupp(text, onClick);
            btn.AutoSize = false;
            btn.Bounds = new Rectangle(25, top, 230, 38);
            return btn;
        }

        private void MainMenuForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.D1:
                case Keys.NumPad1:
                    Open(new PildiVaatajaForm());
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    Open(new MathQuizForm());
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    Open(new MatchingGameForm());
                    break;
                case Keys.Escape:
                    Close();
                    break;
                default:
                    return;
            }

            // Klahv on töödeldud: ei kosta piiksu ega anna nuppudele edasi
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void Open(Form form)
        {
            using (form)
            {
                form.ShowDialog(this);
            }
        }
    }
}
