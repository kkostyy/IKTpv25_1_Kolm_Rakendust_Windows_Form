using System;
using System.Drawing;
using System.Windows.Forms;

namespace KolmRakendust
{
    // Главное меню: три кнопки для запуска приложений
    public class MainMenuForm : Form
    {
        private static readonly Color Accent = Color.FromArgb(0, 120, 215);
        private static readonly Color Border = Color.FromArgb(180, 180, 180);

        public MainMenuForm()
        {
            Text = "Kolm rakendust";
            ClientSize = new Size(320, 270);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 10F);

            var title = new Label
            {
                Text = "Kolm rakendust",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 15, ClientSize.Width, 40)
            };

            var group = new GroupBox
            {
                Text = "Vali rakendus",
                Bounds = new Rectangle(20, 65, 280, 185),
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            group.Controls.Add(MakeButton("1. Pildi vaataja", 30, (s, e) => Open(new PictureViewerForm())));
            group.Controls.Add(MakeButton("2. Math Quiz", 80, (s, e) => Open(new MathQuizForm())));
            group.Controls.Add(MakeButton("3. Matching Game", 130, (s, e) => Open(new MatchingGameForm())));

            Controls.Add(title);
            Controls.Add(group);
        }

        private Button MakeButton(string text, int top, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Bounds = new Rectangle(25, top, 230, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 40, 40),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Border;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 241, 251);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(204, 228, 247);
            btn.MouseEnter += (s, e) => btn.FlatAppearance.BorderColor = Accent;
            btn.MouseLeave += (s, e) => btn.FlatAppearance.BorderColor = Border;
            btn.Click += onClick;
            return btn;
        }

        private void Open(Form form)
        {
            Hide();
            form.ShowDialog();
            form.Dispose();
            Show();
        }
    }
}
