using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    public partial class PildiVaatajaForm : Form
    {
        // PictureBox ilma vilkumiseta (topeltpuhverdus), sest pilt joonistatakse Paint sündmuses
        private class Pind : PictureBox
        {
            public Pind()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }

        private const string AknaPealkiri = "Pildivaataja";
        private const int MiniSuurus = 72;
        private const int ViimasteArv = 8;
        private const int KustutiKordaja = 3;

        // Undo ajalugu on piiratud sammude arvuga JA kogumäluga: suurte fotode puhul jääb samme vähem
        private const int UndoMaxSammud = 20;
        private const long UndoMaxBaite = 300L * 1024 * 1024;

        private static readonly string[] PildiLaiendid = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        private readonly PictureBox pictureBox;
        private readonly Ribbon ribbon;
        private readonly FlowLayoutPanel miniPaneel;
        private readonly ToolTip vihje = new ToolTip();

        private readonly Button showPictureButton;
        private readonly Button newCanvasButton;
        private readonly Button savePictureButton;
        private readonly Button rotateButton;
        private readonly Button flipHorizontalButton;
        private readonly Button flipVerticalButton;
        private readonly CheckBox grayCheckBox;
        private readonly Button undoButton;
        private readonly Button originalButton;
        private readonly CheckBox drawCheckBox;
        private readonly CheckBox eraserCheckBox;
        private readonly Button penColorButton;
        private readonly NumericUpDown penWidthBox;
        private readonly CheckBox sketchCheckBox;
        private readonly Button setBackColorButton;
        private readonly Button clearPictureButton;
        private readonly Button closeButton;

        // Suum ja slaidid (teine tööriistariba)
        private readonly TrackBar zoomBar;
        private readonly Label zoomLabel;
        private readonly Button fitButton;
        private readonly Button slideFolderButton;
        private readonly Button slideStartButton;
        private readonly NumericUpDown slideDelayBox;
        private readonly Timer slaidiTimer;

        // Pilt koosneb kahest kihist: taust (foto/valge leht) ja läbipaistev joonistuskiht.
        // Kustutuskumm kustutab ainult joonistuskihist, seega foto ise jääb alles.
        private Bitmap taust;
        private Bitmap joonis;

        // Mustvalge filter on ainult kuvamise seisund: väljalülitamisel tulevad värvid tagasi
        private bool halltoon;
        // true = filtri muutus tehakse programmiliselt (Undo, uus pilt) ega lähe Undo ajalukku
        private bool vaikseMuutus;

        // Algne pilt ja eelmiste olekute ajalugu (Undo)
        private Bitmap originaal;
        private readonly List<PildiOlek> undoList = new List<PildiOlek>();

        private Color penColor = Color.Red;
        private bool joonistab;
        private Point viimanePunkt;

        // Suum: kas pilt sobitatakse aknasse või kasutatakse käsitsi suumi; paremklõpsuga saab pilti lohistada
        private bool sobitaAknasse = true;
        private float suum = 1f;
        private PointF nihe = PointF.Empty;
        private bool liigutab;
        private Point liigutuseAlgus;
        private PointF niheAlgus;
        private bool suumiMuutus;

        // Slaidid ja viimased failid
        private readonly List<string> kaustaFailid = new List<string>();
        private int kaustaIndeks = -1;
        private readonly Dictionary<string, Bitmap> miniatuurid = new Dictionary<string, Bitmap>();
        private readonly List<Bitmap> vanadMiniatuurid = new List<Bitmap>();

        public PildiVaatajaForm()
        {
            UiStiil.SeadistaSkaleerimine(this);
            Text = AknaPealkiri;
            Font = UiStiil.Põhifont;
            Width = 1300;
            Height = 720;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(480, 320);
            StartPosition = FormStartPosition.CenterParent;

            pictureBox = new Pind
            {
                Dock = DockStyle.Fill,
                BackColor = UiStiil.ToaTaust,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            pictureBox.Paint += PictureBox_Paint;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += (s, e) =>
            {
                joonistab = false;
                liigutab = false;
            };

            // Kõik põhinupud on akna ÜLEMISEL paneelil ühel real
            ribbon = new Ribbon();

            showPictureButton = UiStiil.LooNupp("Ava pilt", ShowPictureButton_Click, true);
            newCanvasButton = UiStiil.LooNupp("Uus leht", NewCanvasButton_Click, true);
            savePictureButton = UiStiil.LooNupp("Salvesta", SavePictureButton_Click, true);
            rotateButton = UiStiil.LooNupp("Pööra 90°", (s, e) => PoordaPilt(RotateFlipType.Rotate90FlipNone));
            flipHorizontalButton = UiStiil.LooNupp("Peegelda ↔", (s, e) => PoordaPilt(RotateFlipType.RotateNoneFlipX));
            flipVerticalButton = UiStiil.LooNupp("Peegelda ↕", (s, e) => PoordaPilt(RotateFlipType.RotateNoneFlipY));
            undoButton = UiStiil.LooNupp("Võta tagasi", UndoButton_Click);
            originalButton = UiStiil.LooNupp("Algne", OriginalButton_Click);

            vihje.SetToolTip(showPictureButton, "Ctrl+O");
            vihje.SetToolTip(savePictureButton, "Ctrl+S");
            vihje.SetToolTip(undoButton, "Ctrl+Z");

            // Mustvalge on lüliti: sisse = mustvalge, välja = värviline pilt tagasi.
            // Lüliti muutus läheb Undo ajalukku (ilma pildikihte kopeerimata).
            grayCheckBox = new CheckBox { Text = "Mustvalge", AutoSize = true, Margin = new Padding(6, 8, 6, 3) };
            grayCheckBox.CheckedChanged += GrayCheckBox_CheckedChanged;

            drawCheckBox = new CheckBox { Text = "Joonista", AutoSize = true, Margin = new Padding(6, 8, 6, 3) };
            drawCheckBox.CheckedChanged += (s, e) =>
            {
                if (drawCheckBox.Checked) eraserCheckBox.Checked = false;
                UuendaKursor();
            };

            eraserCheckBox = new CheckBox { Text = "Kustutuskumm", AutoSize = true, Margin = new Padding(6, 8, 6, 3) };
            eraserCheckBox.CheckedChanged += (s, e) =>
            {
                if (eraserCheckBox.Checked) drawCheckBox.Checked = false;
                UuendaKursor();
            };

            penColorButton = UiStiil.LooNupp("Pliiatsi värv", PenColorButton_Click);
            penColorButton.BackColor = penColor;
            penColorButton.ForeColor = Color.White;

            penWidthBox = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 50,
                Value = 4,
                Width = 50,
                Margin = new Padding(3, 6, 3, 3)
            };

            // Venita: venitab pildi üle kogu pildiala (StretchImage); väljas on Zoom
            sketchCheckBox = new CheckBox { Text = "Venita", AutoSize = true, Margin = new Padding(6, 8, 6, 3) };
            sketchCheckBox.CheckedChanged += SketchCheckBox_CheckedChanged;

            setBackColorButton = UiStiil.LooNupp("Taustavärv", SetBackColorButton_Click);
            clearPictureButton = UiStiil.LooNupp("Tühjenda", ClearPictureButton_Click);
            closeButton = UiStiil.LooNupp("Sulge", (s, e) => Close());

            GroupBox gFail = (UiStiil.LooGrupp("Fail",
                showPictureButton, newCanvasButton, savePictureButton));
            GroupBox gMuuda = (UiStiil.LooGrupp("Muuda",
                rotateButton, flipHorizontalButton, flipVerticalButton, grayCheckBox, undoButton, originalButton));
            GroupBox gJoonistus = (UiStiil.LooGrupp("Joonistus",
                drawCheckBox, eraserCheckBox, penColorButton, UiStiil.LooSilt("Paksus"), penWidthBox));
            GroupBox gVaade = (UiStiil.LooGrupp("Vaade",
                sketchCheckBox, setBackColorButton, clearPictureButton, closeButton));

            // ---------- Teine tööriistariba: suum ja slaidid ----------

            zoomBar = new TrackBar
            {
                Minimum = 10,
                Maximum = 400,
                Value = 100,
                TickStyle = TickStyle.None,
                AutoSize = false,
                Width = 150,
                Height = 28,
                SmallChange = 5,
                LargeChange = 25,
                Margin = new Padding(3, 4, 3, 3)
            };
            zoomBar.ValueChanged += ZoomBar_ValueChanged;
            zoomLabel = new Label
            {
                Text = "100%",
                AutoSize = false,
                Width = 48,
                Height = 24,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(3, 6, 3, 3)
            };
            fitButton = UiStiil.LooNupp("Sobita", (s, e) => SobitaAknasse());
            vihje.SetToolTip(zoomBar, "Suum. Pildi liigutamiseks hoia all hiire parem nupp.");

            slideFolderButton = UiStiil.LooNupp("Vali kaust…", (s, e) => ValiSlaidideKaust());
            slideStartButton = UiStiil.LooNupp("▶ Käivita", (s, e) => LylitaSlaidid());
            slideDelayBox = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 60,
                Value = 3,
                Width = 50,
                Margin = new Padding(3, 6, 3, 3)
            };
            slideDelayBox.ValueChanged += (s, e) => slaidiTimer.Interval = (int)slideDelayBox.Value * 1000;
            vihje.SetToolTip(slideStartButton, "Slaidide ajal liigub ←/→ klahvidega eelmise/järgmise pildi peale.");

            slaidiTimer = new Timer { Interval = (int)slideDelayBox.Value * 1000 };
            slaidiTimer.Tick += (s, e) => NaitaSlaid(1);

            GroupBox gSuum = (UiStiil.LooGrupp("Suum", zoomBar, zoomLabel, fitButton));
            GroupBox gSlaidid = (UiStiil.LooGrupp("Slaidid",
                slideFolderButton, slideStartButton, UiStiil.LooSilt("Paus (s)"), slideDelayBox));

            ribbon.LisaSakk("Fail", gFail, gSlaidid);
            ribbon.LisaSakk("Muuda", gMuuda);
            ribbon.LisaSakk("Joonistus", gJoonistus);
            ribbon.LisaSakk("Vaade", gSuum, gVaade);

            // ---------- Alumine riba: viimased failid ----------
            miniPaneel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = MiniSuurus + 14,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(6, 4, 6, 4),
                BackColor = UiStiil.PaneeliTaust
            };

            // Fill-juhtelement (pictureBox) lisatakse esimesena, et ääred jaotataks enne seda
            Controls.Add(pictureBox);
            Controls.Add(miniPaneel);
            Controls.Add(UiStiil.LooEraldusjoon());
            Controls.Add(ribbon);

            // Aken ei saa olla kitsam kui nupuriba, seega nupud jäävad alati ühele reale
            ribbon.Valmis(this);

            pictureBox.Resize += (s, e) =>
            {
                if (sobitaAknasse) UuendaSuumiNaitu();
            };
            Shown += (s, e) => UuendaMiniatuurid();
        }

        private void UuendaKursor()
        {
            pictureBox.Cursor = (drawCheckBox.Checked || eraserCheckBox.Checked)
                ? Cursors.Cross
                : Cursors.Default;
        }

        // ---------- Kiirklahvid ----------

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.Z:
                    UndoButton_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.S:
                    SavePictureButton_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.O:
                    ShowPictureButton_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Left:
                case Keys.Right:
                    // Nooleklahvid liigutavad slaide, kui fookus pole arvuväljal või suumiliuguril
                    if (kaustaFailid.Count > 0 && !(ActiveControl is NumericUpDown) && !(ActiveControl is TrackBar))
                    {
                        bool taaskaivita = slaidiTimer.Enabled;
                        if (taaskaivita) slaidiTimer.Stop();
                        NaitaSlaid(keyData == Keys.Right ? 1 : -1);
                        if (taaskaivita) slaidiTimer.Start();
                        return true;
                    }
                    break;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---------- Kuvamine ja suum ----------

        // Skaala, millega pilt hetkel pildialale joonistatakse
        private float PraeguneSkaala()
        {
            if (taust == null)
            {
                return 1f;
            }
            if (sobitaAknasse)
            {
                float cw = Math.Max(1, pictureBox.ClientSize.Width);
                float ch = Math.Max(1, pictureBox.ClientSize.Height);
                return Math.Min(cw / taust.Width, ch / taust.Height);
            }
            return suum;
        }

        // Pildi asukoht ja suurus pildialal (arvestab sobitamist, käsitsi suumi ja venitamist)
        private RectangleF PildiAla()
        {
            float cw = Math.Max(1, pictureBox.ClientSize.Width);
            float ch = Math.Max(1, pictureBox.ClientSize.Height);

            if (taust == null || pictureBox.SizeMode == PictureBoxSizeMode.StretchImage)
            {
                return new RectangleF(0, 0, cw, ch);
            }

            float skaala = PraeguneSkaala();
            float w = taust.Width * skaala;
            float h = taust.Height * skaala;
            float x = (cw - w) / 2f + (sobitaAknasse ? 0f : nihe.X);
            float y = (ch - h) / 2f + (sobitaAknasse ? 0f : nihe.Y);
            return new RectangleF(x, y, w, h);
        }

        // Paneb liuguri ja sildi näitama pildi tegelikku suumi (ilma uut suumi käivitamata)
        private void UuendaSuumiNaitu()
        {
            int protsent = (int)Math.Round(PraeguneSkaala() * 100f);
            suumiMuutus = true;
            zoomBar.Value = Math.Max(zoomBar.Minimum, Math.Min(zoomBar.Maximum, protsent));
            suumiMuutus = false;
            zoomLabel.Text = protsent + "%";
        }

        private void ZoomBar_ValueChanged(object sender, EventArgs e)
        {
            if (suumiMuutus)
            {
                return;
            }

            if (sobitaAknasse)
            {
                nihe = PointF.Empty;
            }
            sobitaAknasse = false;
            suum = zoomBar.Value / 100f;
            zoomLabel.Text = zoomBar.Value + "%";

            if (sketchCheckBox.Checked)
            {
                sketchCheckBox.Checked = false;
            }
            pictureBox.Invalidate();
        }

        private void SobitaAknasse()
        {
            sobitaAknasse = true;
            nihe = PointF.Empty;
            UuendaSuumiNaitu();
            pictureBox.Invalidate();
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (taust == null)
            {
                return;
            }
            JoonistaKoos(e.Graphics, Rectangle.Round(PildiAla()));
        }

        // Joonistab taustapildi ja selle peale joonistuskihi; mustvalge filter mõjub mõlemale
        private void JoonistaKoos(Graphics g, Rectangle ala)
        {
            using (ImageAttributes atr = new ImageAttributes())
            {
                if (halltoon)
                {
                    atr.SetColorMatrix(PildiTootlus.HalltoonideMaatriks());
                }
                atr.SetWrapMode(WrapMode.TileFlipXY);

                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(taust, ala, 0, 0, taust.Width, taust.Height, GraphicsUnit.Pixel, atr);
                g.DrawImage(joonis, ala, 0, 0, joonis.Width, joonis.Height, GraphicsUnit.Pixel, atr);
            }
        }

        // ---------- Pildi avamine ja salvestamine ----------

        private void ShowPictureButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Vali pilt";
                dialog.Filter = "Pildifailid|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Kõik failid|*.*";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                PeataSlaidid();
                if (LaadiFail(dialog.FileName, true))
                {
                    LisaViimaneFail(dialog.FileName);
                }
            }
        }

        // Avab pildi failist (kasutavad Ava pilt, miniatuurid ja slaidid)
        private bool LaadiFail(string tee, bool naitaViga)
        {
            try
            {
                // Pilt kopeeritakse uude Bitmap'i: fail ei jää lukku
                Bitmap uus;
                using (var fail = Image.FromFile(tee))
                {
                    uus = new Bitmap(fail);
                }
                AsendaPildid(uus, new Bitmap(uus.Width, uus.Height));

                originaal?.Dispose();
                originaal = new Bitmap(uus);
                TyhjendaUndo();
                SeadaHalltoon(false);
                SobitaAknasse();

                Text = AknaPealkiri + " – " + Path.GetFileName(tee);
                return true;
            }
            catch (Exception)
            {
                if (naitaViga)
                {
                    MessageBox.Show(this, "Faili ei õnnestunud pildina avada.", "Viga",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }
        }

        private void NewCanvasButton_Click(object sender, EventArgs e)
        {
            PeataSlaidid();
            LooTuhiLeht();
            drawCheckBox.Checked = true;
        }

        private void LooTuhiLeht()
        {
            int laius = Math.Max(400, pictureBox.ClientSize.Width);
            int korgus = Math.Max(300, pictureBox.ClientSize.Height);

            Bitmap leht = new Bitmap(laius, korgus);
            using (Graphics g = Graphics.FromImage(leht))
            {
                g.Clear(Color.White);
            }

            SalvestaUndo();
            AsendaPildid(leht, new Bitmap(laius, korgus));
            originaal?.Dispose();
            originaal = new Bitmap(leht);
            SobitaAknasse();
            Text = AknaPealkiri;
        }

        private void SavePictureButton_Click(object sender, EventArgs e)
        {
            if (taust == null)
            {
                MessageBox.Show(this, "Salvestamiseks ava pilt või loo tühi leht (Uus leht).", "Pilti pole",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Salvesta pilt";
                dialog.Filter = "PNG|*.png|JPEG|*.jpg|BMP|*.bmp";
                // Failinimi pakutakse kohe kujul: pilt_aasta-kuu-päev_tunnid-minutid-sekundid
                dialog.FileName = "pilt_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                ImageFormat vorming = ImageFormat.Png;
                switch (Path.GetExtension(dialog.FileName).ToLowerInvariant())
                {
                    case ".jpg":
                    case ".jpeg":
                        vorming = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        vorming = ImageFormat.Bmp;
                        break;
                }

                try
                {
                    // Salvestatakse taust + joonistus (ja mustvalge, kui filter on sees)
                    using (Bitmap tulemus = new Bitmap(taust.Width, taust.Height))
                    using (Graphics g = Graphics.FromImage(tulemus))
                    {
                        JoonistaKoos(g, new Rectangle(0, 0, taust.Width, taust.Height));
                        tulemus.Save(dialog.FileName, vorming);
                    }
                    LisaViimaneFail(dialog.FileName);
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Pildi salvestamine ebaõnnestus.", "Viga",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // Paneb uued kihid kasutusele ja vabastab eelmised
        private void AsendaPildid(Bitmap uusTaust, Bitmap uusJoonis)
        {
            Bitmap vanaTaust = taust;
            Bitmap vanaJoonis = joonis;
            taust = uusTaust;
            joonis = uusJoonis;
            vanaTaust?.Dispose();
            vanaJoonis?.Dispose();
            UuendaSuumiNaitu();
            pictureBox.Invalidate();
        }

        // ---------- Pööramine ----------

        private void PoordaPilt(RotateFlipType tuup)
        {
            if (taust == null)
            {
                return;
            }

            SalvestaUndo();
            taust.RotateFlip(tuup);
            joonis.RotateFlip(tuup);
            UuendaSuumiNaitu();
            pictureBox.Invalidate();
        }

        // ---------- Mustvalge filter ----------

        private void GrayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!vaikseMuutus && taust != null)
            {
                // Salvestatakse eelmine filtri seisund; pildikihte ei kopeerita
                LisaUndo(PildiOlek.AinultFilter(halltoon));
            }
            halltoon = grayCheckBox.Checked;
            pictureBox.Invalidate();
        }

        // Muudab filtri seisundit nii, et see ei satu Undo ajalukku
        private void SeadaHalltoon(bool sees)
        {
            vaikseMuutus = true;
            grayCheckBox.Checked = sees;
            halltoon = sees;
            vaikseMuutus = false;
            pictureBox.Invalidate();
        }

        // ---------- Undo ja algse pildi taastamine ----------

        // Mitu sammu mahub mällu: suur foto võtab ühe sammu kohta mitu kümmet megabaiti
        private int UndoSammudeLimiit()
        {
            if (taust == null)
            {
                return UndoMaxSammud;
            }

            long yksSamm = 2L * taust.Width * taust.Height * 4L;
            long mahub = UndoMaxBaite / Math.Max(1L, yksSamm);
            return (int)Math.Max(2L, Math.Min((long)UndoMaxSammud, mahub));
        }

        private void LisaUndo(PildiOlek olek)
        {
            undoList.Add(olek);

            int piir = UndoSammudeLimiit();
            while (undoList.Count > piir)
            {
                undoList[0].Dispose();
                undoList.RemoveAt(0);
            }
        }

        private void SalvestaUndo()
        {
            if (taust == null)
            {
                return;
            }

            LisaUndo(new PildiOlek(taust, joonis, halltoon));
        }

        private void TyhjendaUndo()
        {
            foreach (PildiOlek o in undoList)
            {
                o.Dispose();
            }
            undoList.Clear();
        }

        private void UndoButton_Click(object sender, EventArgs e)
        {
            if (undoList.Count == 0)
            {
                return;
            }

            PildiOlek eelmine = undoList[undoList.Count - 1];
            undoList.RemoveAt(undoList.Count - 1);

            // Täisolek asendab pildikihid; filtri-olek muudab ainult mustvalge lülitit
            if (eelmine.Taust != null)
            {
                AsendaPildid(eelmine.Taust, eelmine.Joonis);
            }
            SeadaHalltoon(eelmine.Halltoon);
        }

        // Taastab algse pildi, kustutab joonistused ja lülitab mustvalge filtri välja
        private void OriginalButton_Click(object sender, EventArgs e)
        {
            if (taust == null || originaal == null)
            {
                return;
            }

            SalvestaUndo();
            AsendaPildid(new Bitmap(originaal), new Bitmap(originaal.Width, originaal.Height));
            SeadaHalltoon(false);
        }

        // ---------- Joonistamine, kustutamine ja lohistamine ----------

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            // Parem hiirenupp lohistab pilti (käsitsi suumi korral)
            if (e.Button == MouseButtons.Right)
            {
                if (taust != null && !sketchCheckBox.Checked)
                {
                    liigutab = true;
                    liigutuseAlgus = e.Location;
                    niheAlgus = sobitaAknasse ? PointF.Empty : nihe;
                }
                return;
            }

            bool joonistamine = drawCheckBox.Checked;
            bool kustutamine = eraserCheckBox.Checked;

            if ((!joonistamine && !kustutamine) || e.Button != MouseButtons.Left)
            {
                return;
            }

            if (taust == null)
            {
                // Kustutada pole midagi; joonistamisel luuakse automaatselt tühi leht
                if (kustutamine)
                {
                    return;
                }
                LooTuhiLeht();
            }

            PeataSlaidid();
            SalvestaUndo();
            joonistab = true;
            viimanePunkt = e.Location;
            JoonistaLoik(e.Location, e.Location);
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (liigutab && taust != null)
            {
                if (sobitaAknasse)
                {
                    // Esimene lohistus: jätkame praegusest suurusest, kuid enam ei sobitata aknasse
                    suum = PraeguneSkaala();
                    sobitaAknasse = false;
                    nihe = PointF.Empty;
                    niheAlgus = PointF.Empty;
                }
                nihe = new PointF(
                    niheAlgus.X + e.X - liigutuseAlgus.X,
                    niheAlgus.Y + e.Y - liigutuseAlgus.Y);
                pictureBox.Invalidate();
                return;
            }

            if (!joonistab || taust == null)
            {
                return;
            }

            JoonistaLoik(viimanePunkt, e.Location);
            viimanePunkt = e.Location;
        }

        // Teisendab hiirekoordinaadi pildikoordinaadiks
        private PointF PildiPunkt(Point p, out float skaala)
        {
            RectangleF ala = PildiAla();
            skaala = (ala.Width / taust.Width + ala.Height / taust.Height) / 2f;
            return new PointF(
                (p.X - ala.X) * taust.Width / ala.Width,
                (p.Y - ala.Y) * taust.Height / ala.Height);
        }

        // Joonistab (või kustutab) lõigu ainult joonistuskihil
        private void JoonistaLoik(Point a, Point b)
        {
            float skaala;
            PointF algus = PildiPunkt(a, out skaala);
            PointF lopp = PildiPunkt(b, out skaala);

            bool kustuta = eraserCheckBox.Checked;

            // Joone paksus jagatakse skaalaga, et see paistaks ekraanil alati valitud paksusena
            float paksus = Math.Max(1f, (float)penWidthBox.Value / skaala);
            if (kustuta)
            {
                paksus *= KustutiKordaja;
            }

            using (Graphics g = Graphics.FromImage(joonis))
            using (Pen pliiats = new Pen(kustuta ? Color.Transparent : penColor, paksus))
            {
                if (kustuta)
                {
                    // SourceCopy + läbipaistev pliiats teeb joonistuskihi läbipaistvaks (kustutuskumm)
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.SmoothingMode = SmoothingMode.None;
                }
                else
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                }

                pliiats.StartCap = LineCap.Round;
                pliiats.EndCap = LineCap.Round;
                pliiats.LineJoin = LineJoin.Round;
                g.DrawLine(pliiats, algus, lopp);
            }

            pictureBox.Invalidate();
        }

        private void PenColorButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new ColorDialog())
            {
                dialog.Color = penColor;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    penColor = dialog.Color;
                    penColorButton.BackColor = penColor;
                    penColorButton.ForeColor = penColor.GetBrightness() > 0.6f ? Color.Black : Color.White;
                }
            }
        }

        // ---------- Taust, venitamine, tühjendamine ----------

        private void SetBackColorButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new ColorDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    pictureBox.BackColor = dialog.Color;
                }
            }
        }

        private void SketchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            pictureBox.SizeMode = sketchCheckBox.Checked
                ? PictureBoxSizeMode.StretchImage
                : PictureBoxSizeMode.Zoom;
            pictureBox.Invalidate();
        }

        private void ClearPictureButton_Click(object sender, EventArgs e)
        {
            PeataSlaidid();
            kaustaFailid.Clear();
            kaustaIndeks = -1;

            AsendaPildid(null, null);
            originaal?.Dispose();
            originaal = null;
            TyhjendaUndo();
            joonistab = false;
            Text = AknaPealkiri;
        }

        // ---------- Slaidid kaustast ----------

        private void ValiSlaidideKaust()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Vali kaust piltidega";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                List<string> failid;
                try
                {
                    failid = Directory.GetFiles(dialog.SelectedPath)
                        .Where(f => PildiLaiendid.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Kausta ei õnnestunud lugeda.", "Viga",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (failid.Count == 0)
                {
                    MessageBox.Show(this, "Valitud kaustas ei ole pilte.", "Slaidid",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                kaustaFailid.Clear();
                kaustaFailid.AddRange(failid);
                kaustaIndeks = -1;

                // Näitab kohe esimest pilti ja käivitab slaidiseansi
                NaitaSlaid(1);
                AlustaSlaidid();
            }
        }

        private void LylitaSlaidid()
        {
            if (slaidiTimer.Enabled)
            {
                PeataSlaidid();
            }
            else if (kaustaFailid.Count == 0)
            {
                ValiSlaidideKaust();
            }
            else
            {
                AlustaSlaidid();
            }
        }

        private void AlustaSlaidid()
        {
            if (kaustaFailid.Count == 0)
            {
                return;
            }
            slaidiTimer.Interval = (int)slideDelayBox.Value * 1000;
            slaidiTimer.Start();
            slideStartButton.Text = "⏸ Peata";
        }

        private void PeataSlaidid()
        {
            slaidiTimer.Stop();
            slideStartButton.Text = "▶ Käivita";
        }

        // Näitab järgmist (+1) või eelmist (-1) pilti kaustast; katkised failid jäetakse vahele
        private void NaitaSlaid(int samm)
        {
            int n = kaustaFailid.Count;
            for (int katse = 0; katse < n; katse++)
            {
                kaustaIndeks = ((kaustaIndeks + samm) % n + n) % n;
                if (LaadiFail(kaustaFailid[kaustaIndeks], false))
                {
                    return;
                }
            }
            PeataSlaidid();
        }

        // ---------- Viimased failid (miniatuurid) ----------

        private void LisaViimaneFail(string tee)
        {
            Seaded seaded = SeadedHoidla.Praegune;
            seaded.ViimasedFailid.RemoveAll(f => string.Equals(f, tee, StringComparison.OrdinalIgnoreCase));
            seaded.ViimasedFailid.Insert(0, tee);
            if (seaded.ViimasedFailid.Count > ViimasteArv)
            {
                seaded.ViimasedFailid.RemoveRange(ViimasteArv, seaded.ViimasedFailid.Count - ViimasteArv);
            }
            SeadedHoidla.Salvesta();

            // Fail võis olla üle kirjutatud: vana miniatuur visatakse ära.
            // Seda EI vabastata siin, sest paneelis olev PictureBox kasutab seda veel
            // (vabastatud pilt põhjustaks ArgumentException "Parameter is not valid").
            // Vabastamine toimub UuendaMiniatuurid sees pärast vanade kontrollide eemaldamist.
            Bitmap vana;
            if (miniatuurid.TryGetValue(tee, out vana))
            {
                miniatuurid.Remove(tee);
                vanadMiniatuurid.Add(vana);
            }
            UuendaMiniatuurid();
        }

        private Bitmap LooMiniatuur(string tee)
        {
            Bitmap olemas;
            if (miniatuurid.TryGetValue(tee, out olemas))
            {
                return olemas;
            }

            try
            {
                using (Image pilt = Image.FromFile(tee))
                {
                    float k = Math.Min((float)MiniSuurus / pilt.Width, (float)MiniSuurus / pilt.Height);
                    int w = Math.Max(1, (int)(pilt.Width * k));
                    int h = Math.Max(1, (int)(pilt.Height * k));

                    Bitmap mini = new Bitmap(w, h);
                    using (Graphics g = Graphics.FromImage(mini))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(pilt, 0, 0, w, h);
                    }
                    miniatuurid[tee] = mini;
                    return mini;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void UuendaMiniatuurid()
        {
            miniPaneel.SuspendLayout();
            while (miniPaneel.Controls.Count > 0)
            {
                Control vana = miniPaneel.Controls[0];
                PictureBox vanaPb = vana as PictureBox;
                if (vanaPb != null)
                {
                    vanaPb.Image = null; // pilt kuulub vahemällu, mitte kontrollile
                }
                miniPaneel.Controls.RemoveAt(0);
                vana.Dispose();
            }

            // Nüüd, kui ükski kontroll vanu pilte enam ei kasuta, saab need vabastada
            foreach (Bitmap b in vanadMiniatuurid)
            {
                b.Dispose();
            }
            vanadMiniatuurid.Clear();

            // Kustutatud ja avamatud failid võetakse nimekirjast välja
            List<string> viimased = SeadedHoidla.Praegune.ViimasedFailid;
            viimased.RemoveAll(f => !File.Exists(f));

            // Kustutatud failide miniatuurid vabastatakse vahemälust
            foreach (string kustutatud in miniatuurid.Keys.Where(k => !viimased.Contains(k)).ToList())
            {
                miniatuurid[kustutatud].Dispose();
                miniatuurid.Remove(kustutatud);
            }

            if (viimased.Count > 0)
            {
                miniPaneel.Controls.Add(UiStiil.LooSilt("Viimased:"));
            }

            foreach (string tee in viimased)
            {
                Bitmap mini = LooMiniatuur(tee);
                if (mini == null)
                {
                    continue;
                }

                var pb = new PictureBox
                {
                    Width = MiniSuurus,
                    Height = MiniSuurus,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = mini,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3),
                    Tag = tee
                };
                pb.Click += Miniatuur_Click;
                vihje.SetToolTip(pb, Path.GetFileName(tee));
                miniPaneel.Controls.Add(pb);
            }
            miniPaneel.ResumeLayout(true);
        }

        private void Miniatuur_Click(object sender, EventArgs e)
        {
            string tee = (string)((Control)sender).Tag;
            PeataSlaidid();
            LaadiFail(tee, true);
        }

        // Kutsutakse välja Designer faili Dispose meetodist
        private void VabastaPildid()
        {
            slaidiTimer?.Dispose();
            vihje?.Dispose();
            taust?.Dispose();
            joonis?.Dispose();
            originaal?.Dispose();
            TyhjendaUndo();
            foreach (Bitmap mini in miniatuurid.Values)
            {
                mini.Dispose();
            }
            miniatuurid.Clear();
            foreach (Bitmap b in vanadMiniatuurid)
            {
                b.Dispose();
            }
            vanadMiniatuurid.Clear();
        }
    }
}