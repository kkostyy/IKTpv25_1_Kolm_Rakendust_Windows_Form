using System;
using System.Drawing;
using System.Windows.Forms;

namespace KolmRakendust
{
    // Приложение 1: просмотр картинок
    public class PictureViewerForm : Form
    {
        private readonly PictureBox pictureBox = new PictureBox();
        private readonly CheckBox stretchCheck = new CheckBox();

        public PictureViewerForm()
        {
            Text = "Pildi vaataja";
            ClientSize = new Size(640, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F);

            pictureBox.Dock = DockStyle.Fill;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.BackColor = Color.White;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(245, 247, 250)
            };

            var showBtn = MakeButton("Näita pilti");
            showBtn.Click += ShowPicture;
            var clearBtn = MakeButton("Tühjenda");
            clearBtn.Click += (s, e) => { pictureBox.Image?.Dispose(); pictureBox.Image = null; };
            var colorBtn = MakeButton("Taustavärv");
            colorBtn.Click += ChangeBackground;
            var closeBtn = MakeButton("Sulge");
            closeBtn.Click += (s, e) => Close();

            stretchCheck.Text = "Venita pilt";
            stretchCheck.AutoSize = true;
            stretchCheck.Margin = new Padding(10, 10, 3, 3);
            stretchCheck.CheckedChanged += (s, e) =>
                pictureBox.SizeMode = stretchCheck.Checked ? PictureBoxSizeMode.StretchImage : PictureBoxSizeMode.Zoom;

            buttons.Controls.AddRange(new Control[] { showBtn, clearBtn, colorBtn, stretchCheck, closeBtn });

            Controls.Add(pictureBox);
            Controls.Add(buttons);
        }

        private static Button MakeButton(string text)
        {
            return new Button { Text = text, AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
        }

        private void ShowPicture(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Pildid|*.bmp;*.jpg;*.jpeg;*.png;*.gif|Kõik failid|*.*";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    pictureBox.Image?.Dispose();
                    pictureBox.Load(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Pildi laadimine ebaõnnestus:\n" + ex.Message, "Viga",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ChangeBackground(object sender, EventArgs e)
        {
            using (var dialog = new ColorDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    pictureBox.BackColor = dialog.Color;
            }
        }
    }
}
