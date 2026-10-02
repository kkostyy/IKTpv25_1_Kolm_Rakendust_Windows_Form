using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace KolmRakendust
{
    // Приложение 3: игра "найди пару" (16 карточек, иконки из шрифта Webdings)
    public class MatchingGameForm : Form
    {
        private readonly Random random = new Random();
        private readonly List<string> icons = new List<string>
        {
            "!", "!", "N", "N", ",", ",", "k", "k",
            "b", "b", "v", "v", "w", "w", "z", "z"
        };
        private readonly List<Label> cells = new List<Label>();
        private readonly Label info = new Label();
        private readonly System.Windows.Forms.Timer hideTimer = new System.Windows.Forms.Timer { Interval = 750 };
        private readonly Stopwatch watch = new Stopwatch();
        private Label first, second;
        private int moves;

        public MatchingGameForm()
        {
            Text = "Matching Game";
            ClientSize = new Size(480, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.CornflowerBlue;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 4,
                Padding = new Padding(6),
                BackColor = Color.CornflowerBlue
            };
            for (int i = 0; i < 4; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            }

            for (int i = 0; i < 16; i++)
            {
                var cell = new Label
                {
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    Margin = new Padding(4),
                    BackColor = Color.LightSteelBlue,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Webdings", 48F, FontStyle.Bold)
                };
                cell.Click += Cell_Click;
                cells.Add(cell);
                table.Controls.Add(cell);
            }

            info.Dock = DockStyle.Top;
            info.Height = 40;
            info.TextAlign = ContentAlignment.MiddleCenter;
            info.Font = new Font("Segoe UI", 12F);
            info.ForeColor = Color.White;

            // Fill-контрол добавляется первым, чтобы Top-панель не перекрывала его
            Controls.Add(table);
            Controls.Add(info);

            hideTimer.Tick += HideTimer_Tick;
            NewGame();
        }

        private void NewGame()
        {
            // перемешиваем иконки (Fisher–Yates)
            for (int i = icons.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string tmp = icons[i]; icons[i] = icons[j]; icons[j] = tmp;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].Text = icons[i];
                cells[i].ForeColor = cells[i].BackColor; // спрятать иконку
            }

            first = second = null;
            moves = 0;
            UpdateInfo();
            watch.Restart();
        }

        private void UpdateInfo()
        {
            info.Text = "Käike: " + moves;
        }

        private void Cell_Click(object sender, EventArgs e)
        {
            if (hideTimer.Enabled) return;               // ждём, пока скроется неверная пара
            var clicked = (Label)sender;
            if (clicked.ForeColor != clicked.BackColor) return; // уже открыта

            clicked.ForeColor = Color.Black;

            if (first == null)
            {
                first = clicked;
                return;
            }

            second = clicked;
            moves++;
            UpdateInfo();

            if (first.Text == second.Text)
            {
                first = second = null;
                CheckWinner();
                return;
            }

            hideTimer.Start();
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            hideTimer.Stop();
            first.ForeColor = first.BackColor;
            second.ForeColor = second.BackColor;
            first = second = null;
        }

        private void CheckWinner()
        {
            foreach (var cell in cells)
                if (cell.ForeColor == cell.BackColor) return;

            watch.Stop();
            MessageBox.Show(
                "Leidsid kõik paarid!\nKäike: " + moves + "\nAeg: " + (int)watch.Elapsed.TotalSeconds + " sek",
                "Palju õnne!");
            NewGame();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            hideTimer.Stop();
            base.OnFormClosing(e);
        }
    }
}
