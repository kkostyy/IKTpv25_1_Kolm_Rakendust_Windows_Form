using System;
using System.Drawing;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Ühtne kasutajaliidese stiil (sama mis PildiVaataja vormis):
    // ülemine tööriistariba, rühmad (GroupBox), siniste ja valgete nuppudega.
    public static class UiStiil
    {
        public static readonly Color Sinine = Color.FromArgb(0, 120, 215);
        public static readonly Color PaneeliTaust = Color.FromArgb(245, 246, 248);
        public static readonly Color ToaTaust = Color.FromArgb(235, 237, 240);
        public static readonly Color Joon = Color.FromArgb(210, 214, 220);

        public static readonly Font Põhifont = new Font("Segoe UI", 9F);

        public static FlowLayoutPanel LooTopPaneel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(6),
                BackColor = PaneeliTaust
            };
        }

        // Paneeli nupud on alati ühel real: aken laiendatakse nii, et kõik rühmad mahuksid ära
        public static void NupudUhelReal(Form vorm, FlowLayoutPanel paneel)
        {
            paneel.WrapContents = false;
            int raam = vorm.Width - vorm.ClientSize.Width;
            // Ei lasta aknal ekraanist laiemaks minna
            int vajalik = Math.Min(paneel.GetPreferredSize(Size.Empty).Width,
                Screen.PrimaryScreen.WorkingArea.Width - raam);

            vorm.MinimumSize = new Size(vajalik + raam, vorm.MinimumSize.Height);
            if (vorm.ClientSize.Width < vajalik)
            {
                vorm.ClientSize = new Size(vajalik, vorm.ClientSize.Height);
            }
        }

        public static Panel LooEraldusjoon()
        {
            return new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Joon };
        }

        public static GroupBox LooGrupp(string pealkiri, params Control[] elemendid)
        {
            var sisu = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(4, 16),
                Margin = new Padding(0)
            };
            sisu.Controls.AddRange(elemendid);

            var grupp = new GroupBox
            {
                Text = pealkiri,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(4, 0, 4, 2),
                Padding = new Padding(4, 2, 4, 4)
            };
            grupp.Controls.Add(sisu);
            return grupp;
        }

        public static Button LooNupp(string tekst, EventHandler kasitleja, bool rohutatud = false)
        {
            var nupp = new Button
            {
                Text = tekst,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 30),
                Margin = new Padding(3),
                Padding = new Padding(6, 0, 6, 0),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = rohutatud ? Sinine : Color.White,
                ForeColor = rohutatud ? Color.White : Color.FromArgb(40, 40, 40),
                Cursor = Cursors.Hand
            };
            nupp.FlatAppearance.BorderColor = rohutatud ? Sinine : Color.FromArgb(200, 204, 210);
            nupp.Click += kasitleja;
            return nupp;
        }

        // Aja/tulemuse kuvamise silt (loenduri stiil)
        public static Label LooNaidikuSilt(string algtekst, int laius)
        {
            return new Label
            {
                Text = algtekst,
                AutoSize = false,
                Width = laius,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3)
            };
        }

        public static Label LooSilt(string tekst)
        {
            return new Label { Text = tekst, AutoSize = true, Margin = new Padding(3, 8, 0, 3) };
        }

        // Aja kuvamine kujul mm:ss (kasutavad nii viktoriin kui sobitusmäng)
        public static string FormatAeg(int sekundid)
        {
            sekundid = Math.Max(0, sekundid);
            return $"{sekundid / 60:00}:{sekundid % 60:00}";
        }

        // Skaleerib akna elemendid ekraani DPI järgi (125-150 % skaala korral ei jää liides pisikeseks)
        public static void SeadistaSkaleerimine(Form vorm)
        {
            vorm.AutoScaleDimensions = new SizeF(96F, 96F);
            vorm.AutoScaleMode = AutoScaleMode.Dpi;
        }

        // Nupu seisundi muutmisel sobiv välimus (Enabled=false -> hall)
        public static void SeadistaNupuOlek(Button nupp, bool lubatud)
        {
            nupp.Enabled = lubatud;
            nupp.Cursor = lubatud ? Cursors.Hand : Cursors.Default;
        }
    }
}