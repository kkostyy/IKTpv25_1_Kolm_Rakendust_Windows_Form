using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    public class MathQuizForm : Form
    {
        private enum LoppuPohjus
        {
            AegLabi,
            Peatatud,
            KoikOiged
        }

        private const int VaikimisiAeg = 30;
        private const int AjaluguMaksimum = 100;

        private readonly Button startButton;
        private readonly Button stopButton;
        private readonly Button closeButton;
        private readonly Button historyButton;
        private readonly Button resetSeriesButton;
        private readonly NumericUpDown timeLimitBox;
        private readonly ComboBox levelBox;
        private readonly ComboBox operationsBox;
        private readonly Label timeLeftLabel;
        private readonly Label resultLabel;
        private readonly Label seriesLabel;
        private readonly Label roundsLabel;
        private readonly ProgressBar timeBar;
        private readonly Timer countdownTimer;
        private readonly Panel sisuPaneel;
        private readonly TableLayoutPanel tabel;

        private readonly Font suurFont = new Font("Segoe UI", 16F);
        private readonly Font vastuseFont = new Font("Segoe UI", 14F);
        private readonly Font tulemuseFont = new Font("Segoe UI", 14F, FontStyle.Bold);

        private MatemaatikaUlesanne[] ulesanded;
        private Label[] arv1Sildid;
        private Label[] arv2Sildid;
        private NumericUpDown[] vastusValjad;
        private Label[] tulemusSildid;

        private int allesJaanudAeg;
        private int kogualeAeg;

        // Seeria: ringid, mis on järjest mängitud, ja nende kogutulemus
        private int seeriaOigeid;
        private int seeriaKokku;
        private int seeriaRingid;

        public MathQuizForm()
        {
            UiStiil.SeadistaSkaleerimine(this);
            Text = "Matemaatikaviktoriin";
            Font = UiStiil.Põhifont;
            ClientSize = new Size(680, 560);
            MinimumSize = new Size(560, 520);
            StartPosition = FormStartPosition.CenterParent;

            // ---------- Ülemine tööriistariba (esimene rida) ----------
            var ribbon = new Ribbon();

            startButton = UiStiil.LooNupp("Alusta viktoriini", StartButton_Click, true);
            stopButton = UiStiil.LooNupp("Peata viktoriin", StopButton_Click);
            stopButton.Enabled = false;

            timeLimitBox = new NumericUpDown
            {
                Minimum = 5,
                Maximum = 600,
                Value = VaikimisiAeg,
                Width = 55,
                Margin = new Padding(3, 6, 3, 3)
            };
            // Raskusaste: igal tasemel on oma vaikimisi aeg (seda saab hiljem käsitsi muuta)
            levelBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 110,
                Margin = new Padding(3, 6, 3, 3)
            };
            levelBox.Items.AddRange(new object[] { "Lihtne", "Keskmine", "Raske" });
            levelBox.SelectedIndex = 1;

            timeLeftLabel = UiStiil.LooNaidikuSilt(UiStiil.FormatAeg(VaikimisiAeg), 90);
            resultLabel = UiStiil.LooNaidikuSilt("–", 70);

            GroupBox gRaskusaste = (UiStiil.LooGrupp("Raskusaste", levelBox));
            GroupBox gViktoriin = (UiStiil.LooGrupp("Viktoriin", startButton, stopButton));
            GroupBox gAeg = (UiStiil.LooGrupp("Aeg",
                UiStiil.LooSilt("Piir (s)"), timeLimitBox, UiStiil.LooSilt("Järel"), timeLeftLabel));
            GroupBox gTulemus = (UiStiil.LooGrupp("Tulemus", resultLabel));

            // ---------- Teine rida: tehted, seeria, ajalugu ----------
            
            operationsBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Margin = new Padding(3, 6, 3, 3)
            };
            operationsBox.Items.AddRange(new object[] { "+  −  ×  ÷", "+  −  ×  ÷  %  ^" });
            operationsBox.SelectedIndex = 0;

            seriesLabel = UiStiil.LooNaidikuSilt("0 / 0", 90);
            roundsLabel = UiStiil.LooNaidikuSilt("0", 45);
            resetSeriesButton = UiStiil.LooNupp("Nulli seeria", (s, e) => NulliSeeria());
            historyButton = UiStiil.LooNupp("Ajalugu", (s, e) => NaitaAjalugu());
            closeButton = UiStiil.LooNupp("Sulge", (s, e) => Close());

            GroupBox gTehted = (UiStiil.LooGrupp("Tehted", operationsBox));
            GroupBox gSeeria = (UiStiil.LooGrupp("Seeria",
                UiStiil.LooSilt("Skoor"), seriesLabel, UiStiil.LooSilt("Ringe"), roundsLabel, resetSeriesButton));
            GroupBox gVaade = (UiStiil.LooGrupp("Vaade", historyButton, closeButton));

            ribbon.LisaSakk("Viktoriin", gViktoriin, gAeg, gTulemus);
            ribbon.LisaSakk("Seaded", gRaskusaste, gTehted);
            ribbon.LisaSakk("Seeria", gSeeria, gVaade);

            // Aja edenemisriba tööriistariba all
            timeBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 6,
                Minimum = 0,
                Maximum = VaikimisiAeg,
                Value = VaikimisiAeg,
                Style = ProgressBarStyle.Continuous
            };

            // ---------- Ülesannete ala ----------
            sisuPaneel = new Panel { Dock = DockStyle.Fill, BackColor = UiStiil.ToaTaust, AutoScroll = true };

            tabel = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 6,
                BackColor = Color.White,
                Padding = new Padding(24, 18, 24, 18)
            };

            sisuPaneel.Controls.Add(tabel);
            sisuPaneel.Resize += (s, e) => KeskendaTabel(sisuPaneel, tabel);
            tabel.SizeChanged += (s, e) => KeskendaTabel(sisuPaneel, tabel);

            countdownTimer = new Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;

            Controls.Add(sisuPaneel);
            Controls.Add(timeBar);
            Controls.Add(UiStiil.LooEraldusjoon());
            Controls.Add(ribbon);

            EhitaUlesanded();
            ribbon.Valmis(this);
            Shown += (s, e) => KeskendaTabel(sisuPaneel, tabel);

            // Sündmused ühendatakse alles pärast algseadistust
            levelBox.SelectedIndexChanged += LevelBox_SelectedIndexChanged;
            operationsBox.SelectedIndexChanged += OperationsBox_SelectedIndexChanged;
        }

        // ---------- Ülesannete tabel ----------

        private char[] ValitudTehted => operationsBox.SelectedIndex == 1
            ? MatemaatikaUlesanne.LaiendatudTehted
            : MatemaatikaUlesanne.PohiTehted;

        // Ehitab tabeli valitud tehete järgi (4 või 6 rida) ja genereerib uued ülesanded
        private void EhitaUlesanded()
        {
            char[] tehted = ValitudTehted;
            int n = tehted.Length;

            tabel.SuspendLayout();
            while (tabel.Controls.Count > 0)
            {
                Control vana = tabel.Controls[0];
                tabel.Controls.RemoveAt(0);
                vana.Dispose();
            }
            tabel.RowStyles.Clear();
            tabel.RowCount = n;

            ulesanded = new MatemaatikaUlesanne[n];
            arv1Sildid = new Label[n];
            arv2Sildid = new Label[n];
            vastusValjad = new NumericUpDown[n];
            tulemusSildid = new Label[n];

            for (int i = 0; i < n; i++)
            {
                ulesanded[i] = new MatemaatikaUlesanne(tehted[i], ValitudAste);

                arv1Sildid[i] = LooUlesandeSilt(suurFont, ContentAlignment.MiddleRight, 60);
                Label teheSilt = LooUlesandeSilt(suurFont, ContentAlignment.MiddleCenter, 40);
                teheSilt.Text = tehted[i].ToString();
                arv2Sildid[i] = LooUlesandeSilt(suurFont, ContentAlignment.MiddleRight, 60);
                Label vordusSilt = LooUlesandeSilt(suurFont, ContentAlignment.MiddleCenter, 40);
                vordusSilt.Text = "=";

                vastusValjad[i] = new NumericUpDown
                {
                    Minimum = -1000,
                    Maximum = 1000,
                    Width = 90,
                    Font = vastuseFont,
                    Enabled = false,
                    Margin = new Padding(6, 10, 6, 10)
                };
                // Iga sisestatud number kontrollitakse: kui kõik neli (kuus) on õiged, lõpeb viktoriin kohe
                vastusValjad[i].TextChanged += (s, e) => KontrolliKasKoikOiged();

                tulemusSildid[i] = LooUlesandeSilt(tulemuseFont, ContentAlignment.MiddleCenter, 40);

                tabel.Controls.Add(arv1Sildid[i], 0, i);
                tabel.Controls.Add(teheSilt, 1, i);
                tabel.Controls.Add(arv2Sildid[i], 2, i);
                tabel.Controls.Add(vordusSilt, 3, i);
                tabel.Controls.Add(vastusValjad[i], 4, i);
                tabel.Controls.Add(tulemusSildid[i], 5, i);
            }

            tabel.ResumeLayout(true);
            NaitaUlesandeid();
            KeskendaTabel(sisuPaneel, tabel);
        }

        private static Label LooUlesandeSilt(Font font, ContentAlignment joondus, int laius)
        {
            return new Label
            {
                AutoSize = false,
                Width = laius,
                Height = 44,
                Font = font,
                TextAlign = joondus,
                Margin = new Padding(3)
            };
        }

        private static void KeskendaTabel(Panel paneel, Control tabel)
        {
            tabel.Location = new Point(
                Math.Max(0, (paneel.ClientSize.Width - tabel.Width) / 2),
                Math.Max(0, (paneel.ClientSize.Height - tabel.Height) / 2));
        }

        // ---------- Seaded ----------

        private int AjaPiirang => (int)timeLimitBox.Value;

        private Raskusaste ValitudAste => (Raskusaste)levelBox.SelectedIndex;

        private static int VaikimisiAegAstmele(Raskusaste aste)
        {
            switch (aste)
            {
                case Raskusaste.Lihtne: return 45;
                case Raskusaste.Raske: return 20;
                default: return 30;
            }
        }

        // Taseme vahetamisel uuendatakse ülesanded ja soovituslik aeg
        private void LevelBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int aeg = VaikimisiAegAstmele(ValitudAste);
            timeLimitBox.Value = aeg;
            timeLeftLabel.Text = UiStiil.FormatAeg(aeg);
            timeLeftLabel.ForeColor = Color.FromArgb(40, 40, 40);
            timeBar.Maximum = aeg;
            timeBar.Value = aeg;

            UusiUlesanded();
        }

        private void OperationsBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            EhitaUlesanded();
            resultLabel.Text = "–";
        }

        private void UusiUlesanded()
        {
            foreach (var ulesanne in ulesanded)
            {
                ulesanne.Aste = ValitudAste;
                ulesanne.GenereeriUuesti();
            }
            NaitaUlesandeid();
            resultLabel.Text = "–";
        }

        private void NaitaUlesandeid()
        {
            for (int i = 0; i < ulesanded.Length; i++)
            {
                arv1Sildid[i].Text = ulesanded[i].Arv1.ToString();
                arv2Sildid[i].Text = ulesanded[i].Arv2.ToString();
                vastusValjad[i].Value = 0;
                tulemusSildid[i].Text = string.Empty;
            }
        }

        private void NaitaAega()
        {
            timeLeftLabel.Text = UiStiil.FormatAeg(allesJaanudAeg);
            timeBar.Value = Math.Max(0, Math.Min(timeBar.Maximum, allesJaanudAeg));
            // Viimased 5 sekundit on loendur punane
            timeLeftLabel.ForeColor = allesJaanudAeg <= 5 ? Color.Firebrick : Color.FromArgb(40, 40, 40);
        }

        // ---------- Viktoriini käik ----------

        private void StartButton_Click(object sender, EventArgs e)
        {
            UusiUlesanded();

            kogualeAeg = AjaPiirang;
            allesJaanudAeg = kogualeAeg;
            timeBar.Maximum = kogualeAeg;
            NaitaAega();

            foreach (var valjund in vastusValjad)
            {
                valjund.Enabled = true;
            }
            MuudaSeadeteLubatavust(false);

            vastusValjad[0].Focus();
            countdownTimer.Start();
        }

        // Käimasoleva viktoriini ajal ei saa taset, tehteid ega aega muuta
        private void MuudaSeadeteLubatavust(bool lubatud)
        {
            startButton.Enabled = lubatud;
            stopButton.Enabled = !lubatud;
            timeLimitBox.Enabled = lubatud;
            levelBox.Enabled = lubatud;
            operationsBox.Enabled = lubatud;
            resetSeriesButton.Enabled = lubatud;
            historyButton.Enabled = lubatud;
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            allesJaanudAeg--;
            NaitaAega();

            if (allesJaanudAeg <= 0)
            {
                countdownTimer.Stop();
                LopetaViktoriin(LoppuPohjus.AegLabi);
            }
        }

        // Viktoriini saab enne aja lõppu peatada; vastused kontrollitakse samamoodi
        private void StopButton_Click(object sender, EventArgs e)
        {
            countdownTimer.Stop();
            LopetaViktoriin(LoppuPohjus.Peatatud);
        }

        // Loeb vastuse välja tekstist, sest NumericUpDown.Value uueneb alles välja kaotamisel fookuse
        private int LuguVastus(int i)
        {
            int vastus;
            return int.TryParse(vastusValjad[i].Text.Trim(), out vastus)
                ? vastus
                : (int)vastusValjad[i].Value;
        }

        // Kui kõik vastused on juba õiged, ei oota viktoriin taimeri lõppu
        private void KontrolliKasKoikOiged()
        {
            if (!countdownTimer.Enabled)
            {
                return;
            }

            for (int i = 0; i < ulesanded.Length; i++)
            {
                if (!ulesanded[i].KontrolliVastust(LuguVastus(i)))
                {
                    return;
                }
            }

            countdownTimer.Stop();
            LopetaViktoriin(LoppuPohjus.KoikOiged);
        }

        private void LopetaViktoriin(LoppuPohjus pohjus)
        {
            int oigeidVastuseid = 0;

            for (int i = 0; i < ulesanded.Length; i++)
            {
                vastusValjad[i].Enabled = false;
                bool oige = ulesanded[i].KontrolliVastust(LuguVastus(i));
                tulemusSildid[i].Text = oige ? "✔" : "✘";
                tulemusSildid[i].ForeColor = oige ? Color.SeaGreen : Color.Firebrick;
                if (oige)
                {
                    oigeidVastuseid++;
                }
            }

            int kulunud = kogualeAeg - Math.Max(0, allesJaanudAeg);
            resultLabel.Text = oigeidVastuseid + " / " + ulesanded.Length;
            MuudaSeadeteLubatavust(true);

            // Seeria ja ajalugu
            seeriaOigeid += oigeidVastuseid;
            seeriaKokku += ulesanded.Length;
            seeriaRingid++;
            UuendaSeeriat();
            SalvestaTulemus(oigeidVastuseid, kulunud);

            string pealkiri;
            switch (pohjus)
            {
                case LoppuPohjus.Peatatud: pealkiri = "Viktoriin peatati!"; break;
                case LoppuPohjus.KoikOiged: pealkiri = "Kõik vastused on õiged!"; break;
                default: pealkiri = "Aeg sai otsa!"; break;
            }

            MessageBox.Show(this,
                $"{pealkiri}\nRaskusaste: {levelBox.Text}\nÕigeid vastuseid: {oigeidVastuseid} / {ulesanded.Length}\n" +
                $"Kulunud aeg: {UiStiil.FormatAeg(kulunud)}\nSeeria: {seeriaOigeid} / {seeriaKokku} ({seeriaRingid}. ring)",
                "Tulemus",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ---------- Seeria ja ajalugu ----------

        private void UuendaSeeriat()
        {
            seriesLabel.Text = seeriaOigeid + " / " + seeriaKokku;
            roundsLabel.Text = seeriaRingid.ToString();
        }

        private void NulliSeeria()
        {
            seeriaOigeid = 0;
            seeriaKokku = 0;
            seeriaRingid = 0;
            UuendaSeeriat();
        }

        private void SalvestaTulemus(int oigeid, int sekundid)
        {
            List<ViktoriiniTulemus> ajalugu = SeadedHoidla.Praegune.ViktoriiniAjalugu;
            ajalugu.Add(new ViktoriiniTulemus
            {
                Kuupaev = DateTime.Now,
                Aste = levelBox.Text,
                Tehted = operationsBox.SelectedIndex == 1 ? "6 tehet" : "4 tehet",
                Oigeid = oigeid,
                Kokku = ulesanded.Length,
                Sekundid = sekundid
            });

            if (ajalugu.Count > AjaluguMaksimum)
            {
                ajalugu.RemoveRange(0, ajalugu.Count - AjaluguMaksimum);
            }
            SeadedHoidla.Salvesta();
        }

        private void NaitaAjalugu()
        {
            // Uuemad tulemused on tabeli ülaosas
            List<string[]> read = SeadedHoidla.Praegune.ViktoriiniAjalugu
                .AsEnumerable()
                .Reverse()
                .Select(t => new[]
                {
                    t.Kuupaev.ToString("dd.MM.yyyy HH:mm"),
                    t.Aste,
                    t.Tehted,
                    t.Oigeid + " / " + t.Kokku,
                    UiStiil.FormatAeg(t.Sekundid)
                })
                .ToList();

            using (var dialoog = new TabeliDialoog(
                "Viktoriini ajalugu",
                new[] { "Kuupäev", "Raskusaste", "Tehted", "Õigeid", "Aeg" },
                read,
                () =>
                {
                    SeadedHoidla.Praegune.ViktoriiniAjalugu.Clear();
                    SeadedHoidla.Salvesta();
                }))
            {
                dialoog.ShowDialog(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                countdownTimer?.Dispose();
            }
            base.Dispose(disposing);
            if (disposing)
            {
                suurFont.Dispose();
                vastuseFont.Dispose();
                tulemuseFont.Dispose();
            }
        }
    }
}
