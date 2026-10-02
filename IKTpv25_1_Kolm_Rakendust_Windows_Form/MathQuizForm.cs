using System;
using System.Drawing;
using System.Windows.Forms;

namespace KolmRakendust
{
    // Приложение 2: математическая викторина на время (+, -, ×, ÷)
    public class MathQuizForm : Form
    {
        private const int QuizSeconds = 30;
        private static readonly string[] Ops = { "+", "−", "×", "÷" };

        private readonly Random rnd = new Random();
        private readonly Label timeLabel = new Label();
        private readonly Button startButton = new Button();
        private readonly Label[] leftLabels = new Label[4];
        private readonly Label[] rightLabels = new Label[4];
        private readonly NumericUpDown[] answers = new NumericUpDown[4];
        private readonly int[] expected = new int[4];
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 1000 };
        private int timeLeft;

        public MathQuizForm()
        {
            Text = "Math Quiz";
            ClientSize = new Size(440, 350);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 18F);

            timeLabel.Text = "Aeg: " + QuizSeconds + " sek";
            timeLabel.Bounds = new Rectangle(20, 15, 400, 45);
            Controls.Add(timeLabel);

            for (int i = 0; i < 4; i++)
            {
                int y = 80 + i * 55;
                leftLabels[i] = MakeLabel("?", 20, y, 70);
                MakeLabel(Ops[i], 95, y, 35);
                rightLabels[i] = MakeLabel("?", 130, y, 70);
                MakeLabel("=", 205, y, 35);

                answers[i] = new NumericUpDown
                {
                    Bounds = new Rectangle(250, y, 120, 40),
                    Maximum = 1000,
                    Enabled = false
                };
                Controls.Add(answers[i]);
            }

            startButton.Text = "Alusta";
            startButton.Bounds = new Rectangle(20, 300, 400, 40);
            startButton.Click += (s, e) => StartQuiz();
            Controls.Add(startButton);

            timer.Tick += Timer_Tick;
        }

        private Label MakeLabel(string text, int x, int y, int w)
        {
            var l = new Label
            {
                Text = text,
                Bounds = new Rectangle(x, y, w, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(l);
            return l;
        }

        private void StartQuiz()
        {
            // сложение
            int a = rnd.Next(1, 51), b = rnd.Next(1, 51);
            Set(0, a, b, a + b);
            // вычитание
            int x = rnd.Next(1, 101), y = rnd.Next(1, x + 1);
            Set(1, x, y, x - y);
            // умножение
            int m1 = rnd.Next(2, 11), m2 = rnd.Next(2, 11);
            Set(2, m1, m2, m1 * m2);
            // деление
            int divisor = rnd.Next(2, 11), quotient = rnd.Next(2, 11);
            Set(3, divisor * quotient, divisor, quotient);

            foreach (var n in answers) { n.Value = 0; n.Enabled = true; }
            answers[0].Focus();

            timeLeft = QuizSeconds;
            timeLabel.ForeColor = SystemColors.ControlText;
            timeLabel.Text = "Aeg: " + timeLeft + " sek";
            startButton.Enabled = false;
            timer.Start();
        }

        private void Set(int i, int left, int right, int result)
        {
            leftLabels[i].Text = left.ToString();
            rightLabels[i].Text = right.ToString();
            expected[i] = result;
        }

        private bool CheckAnswers()
        {
            for (int i = 0; i < 4; i++)
                if (answers[i].Value != expected[i]) return false;
            return true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (CheckAnswers())
            {
                Finish();
                MessageBox.Show("Kõik vastused on õiged! Tubli!", "Palju õnne!");
                return;
            }

            if (timeLeft > 0)
            {
                timeLeft--;
                timeLabel.Text = "Aeg: " + timeLeft + " sek";
                if (timeLeft <= 5) timeLabel.ForeColor = Color.Red;
            }
            else
            {
                Finish();
                for (int i = 0; i < 4; i++) answers[i].Value = expected[i];
                MessageBox.Show("Aeg sai läbi! Õiged vastused on täidetud.", "Kahjuks...");
            }
        }

        private void Finish()
        {
            timer.Stop();
            foreach (var n in answers) n.Enabled = false;
            startButton.Enabled = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            timer.Stop();
            base.OnFormClosing(e);
        }
    }
}
