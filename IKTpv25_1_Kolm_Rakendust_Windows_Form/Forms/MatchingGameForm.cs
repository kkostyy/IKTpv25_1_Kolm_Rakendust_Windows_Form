using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    public class MatchingGameForm : Form
    {
        // Wingdings sümbolid: 4×4 väljale kulub esimesed 8, 6×6 väljale kõik 18
        private static readonly char[] Sumbolid =
        {
            'J', ')', 'Q', 'S', '~', '!', 'C', 'N',
            'A', 'B', 'D', 'G', 'H', 'K', 'M', 'O', 'R', 'T'
        };
        private static readonly int[] Suurused = { 4, 6 };
        private static readonly Random rnd = new Random();

        private const int EdetabeliPikkus = 10;

        private readonly List<Mangukaart> kaardid = new List<Mangukaart>();
        private readonly Timer peitmiseTimer;
        private readonly Timer mangukellaTimer;
        private readonly Stopwatch stopper = new Stopwatch();
        private readonly TableLayoutPanel laud;

        private readonly Button newGameButton;
        private readonly Button leaderboardButton;
        private readonly Button closeButton;
        private readonly ComboBox sizeBox;
        private readonly CheckBox soundBox;
        private readonly Label timeLabel;
        private readonly Label movesLabel;
        private readonly Label pairsLabel;
        private readonly Label bestLabel;

        private List<Bitmap> kasutajaPildid = new List<Bitmap>();
        private readonly Label imagesLabel;
        private Font kaardiFont;
        private int suurus = 4;               // väljal on suurus × suurus kaarti
        private Mangukaart esimeneValik;
        private Mangukaart teineValik;
        private bool ootabPeitmist;
        private int leitudPaare;
        private int kaike;

        private int Paare => suurus * suurus / 2;
        private string SuuruseVoti => suurus + "x" + suurus;
        private string SuuruseNimi => suurus + "×" + suurus;

        public MatchingGameForm()
        {
            UiStiil.SeadistaSkaleerimine(this);
            Text = "Sobitusmäng";
            Font = UiStiil.Põhifont;
            ClientSize = new Size(560, 640);
            MinimumSize = new Size(420, 520);
            StartPosition = FormStartPosition.CenterParent;

            // ---------- Ülemine tööriistariba (esimene rida) ----------
            var ribbon = new Ribbon();

            newGameButton = UiStiil.LooNupp("Uus mäng", (s, e) => UusMang(), true);
            closeButton = UiStiil.LooNupp("Sulge", (s, e) => Close());

            sizeBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 70,
                Margin = new Padding(3, 6, 3, 3)
            };
            sizeBox.Items.AddRange(new object[] { "4×4", "6×6" });
            sizeBox.SelectedIndex = 0;

            timeLabel = UiStiil.LooNaidikuSilt("00:00", 80);
            movesLabel = UiStiil.LooNaidikuSilt("0", 50);
            pairsLabel = UiStiil.LooNaidikuSilt("0 / " + Paare, 70);
            bestLabel = UiStiil.LooNaidikuSilt("–", 80);

            GroupBox gMang = (UiStiil.LooGrupp("Mäng", newGameButton, sizeBox));
            GroupBox gAeg = (UiStiil.LooGrupp("Aeg", UiStiil.LooSilt("Aeg"), timeLabel,
                UiStiil.LooSilt("Parim"), bestLabel));
            GroupBox gTulemus = (UiStiil.LooGrupp("Tulemus", UiStiil.LooSilt("Käike"), movesLabel,
                UiStiil.LooSilt("Paare"), pairsLabel));

            // ---------- Teine rida: heli, edetabel, sulgemine ----------
            
            soundBox = new CheckBox
            {
                Text = "Heli",
                AutoSize = true,
                Checked = SeadedHoidla.Praegune.Heli,
                Margin = new Padding(6, 8, 6, 3)
            };
            soundBox.CheckedChanged += (s, e) =>
            {
                SeadedHoidla.Praegune.Heli = soundBox.Checked;
                SeadedHoidla.Salvesta();
            };

            leaderboardButton = UiStiil.LooNupp("Edetabel", (s, e) => NaitaEdetabelit());

            imagesLabel = UiStiil.LooSilt("Pilte: 0");
            Button pickImagesButton = UiStiil.LooNupp("Vali pildid…", (s, e) => ValiPildid());
            Button symbolsButton = UiStiil.LooNupp("Sümbolid", (s, e) => AsendaPildid(new List<Bitmap>(), true));
            GroupBox gPildid = UiStiil.LooGrupp("Kaartide pildid", pickImagesButton, symbolsButton, imagesLabel);

            GroupBox gSeaded = (UiStiil.LooGrupp("Seaded", soundBox));
            GroupBox gTulemused = (UiStiil.LooGrupp("Tulemused", leaderboardButton));
            GroupBox gVaade = (UiStiil.LooGrupp("Vaade", closeButton));

            ribbon.LisaSakk("Mäng", gMang, gAeg, gTulemus);
            ribbon.LisaSakk("Seaded", gSeaded, gPildid, gTulemused, gVaade);

            // ---------- Mängulaud: venib koos aknaga ----------
            laud = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiStiil.ToaTaust,
                Padding = new Padding(10)
            };

            peitmiseTimer = new Timer { Interval = 700 };
            peitmiseTimer.Tick += PeitmiseTimer_Tick;

            // Mängukell uuendab aega 4 korda sekundis
            mangukellaTimer = new Timer { Interval = 250 };
            mangukellaTimer.Tick += (s, e) => UuendaAega();

            Controls.Add(laud);
            Controls.Add(UiStiil.LooEraldusjoon());
            Controls.Add(ribbon);

            LaadiSalvestatudPildid();
            EhitaLaud();
            UuendaParimat();
            ribbon.Valmis(this);

            // Suuruse vahetus kuulatakse alles pärast algseadistust
            sizeBox.SelectedIndexChanged += SizeBox_SelectedIndexChanged;
        }

        private void UuendaAega()
        {
            timeLabel.Text = UiStiil.FormatAeg((int)stopper.Elapsed.TotalSeconds);
        }

        private void UuendaParimat()
        {
            int parim;
            bestLabel.Text = SeadedHoidla.Praegune.ParimadAjad.TryGetValue(SuuruseVoti, out parim)
                ? UiStiil.FormatAeg(parim)
                : "–";
        }

        // Pakett: iga paar kaks korda, segatud. Kasutaja pildid on ees, puudu jäävad täidetakse sümbolitega.
        private List<KaardiSisu> LooPakett()
        {
            List<KaardiSisu> paarid = new List<KaardiSisu>();

            List<int> jarjekord = Enumerable.Range(0, kasutajaPildid.Count).ToList();
            Segamine(jarjekord);
            foreach (int nr in jarjekord.Take(Paare))
            {
                paarid.Add(new KaardiSisu(kasutajaPildid[nr], nr));
            }

            int sumbolNr = 0;
            while (paarid.Count < Paare)
            {
                paarid.Add(new KaardiSisu(Sumbolid[sumbolNr++]));
            }

            List<KaardiSisu> pakett = new List<KaardiSisu>();
            foreach (KaardiSisu p in paarid)
            {
                pakett.Add(p);
                pakett.Add(p);
            }
            Segamine(pakett);
            return pakett;
        }

        private static void Segamine<T>(List<T> nimekiri)
        {
            for (int n = nimekiri.Count - 1; n > 0; n--)
            {
                int k = rnd.Next(n + 1);
                (nimekiri[n], nimekiri[k]) = (nimekiri[k], nimekiri[n]);
            }
        }

        // ---------- Kaartide pildid ----------

        private void LaadiSalvestatudPildid()
        {
            List<string> teed = SeadedHoidla.Praegune.MangupildiFailid.Where(File.Exists).ToList();
            kasutajaPildid = LaadiPildid(teed);
            UuendaPiltideSilt();
        }

        private static List<Bitmap> LaadiPildid(IEnumerable<string> teed)
        {
            List<Bitmap> tulemus = new List<Bitmap>();
            foreach (string tee in teed)
            {
                try
                {
                    using (Image pilt = Image.FromFile(tee))
                    {
                        // Kaardile piisab 256 px; suurt fotot mälus ei hoita
                        float k = Math.Min(1f, Math.Min(256f / pilt.Width, 256f / pilt.Height));
                        int w = Math.Max(1, (int)(pilt.Width * k));
                        int h = Math.Max(1, (int)(pilt.Height * k));
                        Bitmap vaike = new Bitmap(w, h);
                        using (Graphics gr = Graphics.FromImage(vaike))
                        {
                            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gr.DrawImage(pilt, 0, 0, w, h);
                        }
                        tulemus.Add(vaike);
                    }
                }
                catch (Exception)
                {
                    // Fail, mis pole pilt, jäetakse vahele
                }
            }
            return tulemus;
        }

        private void ValiPildid()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Vali kaartide pildid";
                dialog.Filter = "Pildifailid|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                List<Bitmap> uued = LaadiPildid(dialog.FileNames);
                if (uued.Count == 0)
                {
                    MessageBox.Show(this, "Valitud failidest ei õnnestunud ühtegi pilti avada.", "Viga",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SeadedHoidla.Praegune.MangupildiFailid = dialog.FileNames.ToList();
                AsendaPildid(uued, true);

                if (uued.Count < Paare)
                {
                    MessageBox.Show(this,
                        $"Valitud {uued.Count} pilti, väljale {SuuruseNimi} on vaja {Paare} paari. " +
                        "Puuduvad kaardid täidetakse sümbolitega.", "Sobitusmäng",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // Vahetab piltide komplekti ja alustab uut mängu; vanad pildid vabastatakse alles pärast kaartide uuendamist
        private void AsendaPildid(List<Bitmap> uued, bool alustaUuesti)
        {
            List<Bitmap> vanad = kasutajaPildid;
            kasutajaPildid = uued;

            if (uued.Count == 0)
            {
                SeadedHoidla.Praegune.MangupildiFailid = new List<string>();
            }
            SeadedHoidla.Salvesta();

            if (alustaUuesti)
            {
                UusMang();
            }
            foreach (Bitmap b in vanad)
            {
                b.Dispose();
            }
            UuendaPiltideSilt();
        }

        private void UuendaPiltideSilt()
        {
            imagesLabel.Text = "Pilte: " + kasutajaPildid.Count;
        }

        // Ehitab nupud nullist. Vaja ainult siis, kui välja suurus muutub.
        private void EhitaLaud()
        {
            laud.SuspendLayout();

            foreach (Mangukaart kaart in kaardid)
            {
                laud.Controls.Remove(kaart.Nupp);
                kaart.Nupp.Dispose();
            }
            kaardid.Clear();
            kaardiFont?.Dispose();
            kaardiFont = new Font("Wingdings", suurus == 4 ? 28F : 18F);

            laud.RowStyles.Clear();
            laud.ColumnStyles.Clear();
            laud.RowCount = suurus;
            laud.ColumnCount = suurus;
            for (int i = 0; i < suurus; i++)
            {
                laud.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / suurus));
                laud.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / suurus));
            }

            List<KaardiSisu> pakett = LooPakett();
            int j = 0;
            for (int rida = 0; rida < suurus; rida++)
            {
                for (int veerg = 0; veerg < suurus; veerg++)
                {
                    Button nupp = new Button
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(suurus == 4 ? 4 : 3),
                        BackColor = UiStiil.Sinine,
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        UseVisualStyleBackColor = false,
                        Font = kaardiFont,
                        Text = string.Empty,
                        Cursor = Cursors.Hand,
                        TabStop = false
                    };
                    nupp.FlatAppearance.BorderColor = Color.White;

                    Mangukaart kaart = new Mangukaart(pakett[j], nupp);
                    nupp.Click += (s, e) => Kaart_Click(kaart);

                    kaardid.Add(kaart);
                    laud.Controls.Add(nupp, veerg, rida);
                    j++;
                }
            }

            laud.ResumeLayout(true);
        }

        // Uus mäng samal väljal: nupud jäävad alles, sümbolid segatakse ümber (kiire ja ilma vilkumiseta)
        private void UusMang()
        {
            NulliSeis();

            List<KaardiSisu> pakett = LooPakett();
            for (int i = 0; i < kaardid.Count; i++)
            {
                kaardid[i].Sisu = pakett[i];
                kaardid[i].Lahtesta();
            }
        }

        private void NulliSeis()
        {
            peitmiseTimer.Stop();
            mangukellaTimer.Stop();
            stopper.Reset();

            esimeneValik = null;
            teineValik = null;
            ootabPeitmist = false;
            leitudPaare = 0;
            kaike = 0;

            timeLabel.Text = "00:00";
            movesLabel.Text = "0";
            pairsLabel.Text = "0 / " + Paare;
        }

        private void SizeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            suurus = Suurused[sizeBox.SelectedIndex];
            NulliSeis();
            LaadiSalvestatudPildid();
            EhitaLaud();
            UuendaParimat();
        }

        private void Kaart_Click(Mangukaart kaart)
        {
            if (ootabPeitmist || kaart.OnPaaritud || kaart == esimeneValik)
            {
                return;
            }

            // Kell käivitub esimese kaardi avamisel
            if (!stopper.IsRunning && leitudPaare < Paare)
            {
                stopper.Start();
                mangukellaTimer.Start();
            }

            kaart.NaitaSisu();

            if (esimeneValik == null)
            {
                esimeneValik = kaart;
                return;
            }

            kaike++;
            movesLabel.Text = kaike.ToString();

            if (esimeneValik.Sisu.Voti == kaart.Sisu.Voti)
            {
                esimeneValik.MarkeeriPaaritud();
                kaart.MarkeeriPaaritud();

                esimeneValik = null;
                leitudPaare++;
                pairsLabel.Text = leitudPaare + " / " + Paare;
                MangiHelisid(true);

                if (leitudPaare == Paare)
                {
                    LopetaMang();
                }
            }
            else
            {
                MangiHelisid(false);
                teineValik = kaart;
                ootabPeitmist = true;
                peitmiseTimer.Start();
            }
        }

        private void MangiHelisid(bool oigePaar)
        {
            if (!soundBox.Checked)
            {
                return;
            }

            if (oigePaar)
            {
                SystemSounds.Asterisk.Play();
            }
            else
            {
                SystemSounds.Exclamation.Play();
            }
        }

        private void LopetaMang()
        {
            stopper.Stop();
            mangukellaTimer.Stop();
            int sekundid = (int)stopper.Elapsed.TotalSeconds;
            UuendaAega();

            Seaded seaded = SeadedHoidla.Praegune;
            int varasemParim;
            bool uusRekord = !seaded.ParimadAjad.TryGetValue(SuuruseVoti, out varasemParim) || sekundid < varasemParim;
            if (uusRekord)
            {
                seaded.ParimadAjad[SuuruseVoti] = sekundid;
            }
            UuendaParimat();

            bool edetabelisse = JoudisEdetabelisse(sekundid, kaike);

            MessageBox.Show(this,
                $"Palju õnne, leidsid kõik paarid!\nVäli: {SuuruseNimi}\nAeg: {UiStiil.FormatAeg(sekundid)}\nKäike: {kaike}" +
                (uusRekord ? "\nUus parim aeg!" : string.Empty),
                "Mäng läbi", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (edetabelisse)
            {
                string nimi = NimeDialoog.Kysi(this, "Edetabel",
                    "Said edetabelisse! Sisesta oma nimi:", seaded.ViimaneNimi);
                if (nimi != null)
                {
                    seaded.ViimaneNimi = nimi;
                    LisaEdetabelisse(nimi, sekundid, kaike);
                }
            }

            SeadedHoidla.Salvesta();
        }

        // ---------- Edetabel ----------

        private IEnumerable<MangutulemusKirje> EdetabeliKirjed(string voti)
        {
            return SeadedHoidla.Praegune.Edetabel
                .Where(k => k.Suurus == voti)
                .OrderBy(k => k.Sekundid)
                .ThenBy(k => k.Kaike);
        }

        private bool JoudisEdetabelisse(int sekundid, int kaike)
        {
            List<MangutulemusKirje> parimad = EdetabeliKirjed(SuuruseVoti).ToList();
            if (parimad.Count < EdetabeliPikkus)
            {
                return true;
            }

            MangutulemusKirje halvim = parimad[parimad.Count - 1];
            return sekundid < halvim.Sekundid || (sekundid == halvim.Sekundid && kaike < halvim.Kaike);
        }

        private void LisaEdetabelisse(string nimi, int sekundid, int kaike)
        {
            List<MangutulemusKirje> edetabel = SeadedHoidla.Praegune.Edetabel;
            edetabel.Add(new MangutulemusKirje
            {
                Nimi = nimi,
                Suurus = SuuruseVoti,
                Sekundid = sekundid,
                Kaike = kaike,
                Kuupaev = DateTime.Now
            });

            // Igale väljasuurusele jäävad alles ainult 10 parimat
            List<MangutulemusKirje> parimad = EdetabeliKirjed(SuuruseVoti).Take(EdetabeliPikkus).ToList();
            edetabel.RemoveAll(k => k.Suurus == SuuruseVoti);
            edetabel.AddRange(parimad);
        }

        private void NaitaEdetabelit()
        {
            string voti = SuuruseVoti;
            List<string[]> read = EdetabeliKirjed(voti)
                .Select((k, i) => new[]
                {
                    (i + 1).ToString(),
                    k.Nimi,
                    UiStiil.FormatAeg(k.Sekundid),
                    k.Kaike.ToString(),
                    k.Kuupaev.ToString("dd.MM.yyyy")
                })
                .ToList();

            using (var dialoog = new TabeliDialoog(
                "Edetabel " + SuuruseNimi,
                new[] { "#", "Nimi", "Aeg", "Käike", "Kuupäev" },
                read,
                () =>
                {
                    SeadedHoidla.Praegune.Edetabel.RemoveAll(k => k.Suurus == voti);
                    SeadedHoidla.Salvesta();
                }))
            {
                dialoog.ShowDialog(this);
            }
        }

        private void PeitmiseTimer_Tick(object sender, EventArgs e)
        {
            peitmiseTimer.Stop();

            esimeneValik?.Peida();
            teineValik?.Peida();

            esimeneValik = null;
            teineValik = null;
            ootabPeitmist = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                peitmiseTimer?.Dispose();
                mangukellaTimer?.Dispose();
            }
            base.Dispose(disposing);
            if (disposing)
            {
                kaardiFont?.Dispose();
                foreach (Bitmap b in kasutajaPildid)
                {
                    b.Dispose();
                }
            }
        }
    }
}
