using System;
using System.Linq;
using System.Windows.Forms;

namespace BetterScreenTimePC
{
    public partial class ChallengeForm : Form
    {
        private double[] solution = new double[3];
        private double[,] A = new double[3, 3];
        private double[] b = new double[3];

        // Tracks whether closing is happening through our own Submit/Give-up
        // buttons (which set DialogResult first) - anything else (Alt+F4,
        // task-switcher close, etc.) gets blocked in FormClosing below.
        private bool _closingViaButton;

        public ChallengeForm()
        {
            InitializeComponent();
            this.Load += ChallengeForm_Load;
            btnSubmit.Click += BtnSubmit_Click;
            btnGiveUp.Click += BtnGiveUp_Click;
            this.FormClosing += ChallengeForm_FormClosing;

            // Borderless + fullscreen across ALL monitors + always on top, so
            // there's no title bar to drag it aside with and nothing visible
            // to click around it. This stops the simple "move the window and
            // keep using the app underneath" bypass. It does NOT block
            // Alt-Tab/Win+D switching focus to another window - doing that
            // reliably needs a low-level global keyboard hook, a bigger step
            // with its own antivirus-suspicion tradeoffs (similar to what we
            // saw with the hosts file earlier), not added here.
            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen; // covers every monitor, not just the primary one
        }

        private void ChallengeForm_Load(object sender, EventArgs e)
        {
            GenerateSystem();
            RenderSystem();
        }

        private void ChallengeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closingViaButton)
                e.Cancel = true; // block Alt+F4 / any close that didn't go through Submit or Give up
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Swallow Alt+F4 explicitly (belt-and-suspenders alongside FormClosing above).
            if (keyData == (Keys.Alt | Keys.F4))
                return true; // true = handled, suppresses the default close behavior
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BtnGiveUp_Click(object sender, EventArgs e)
        {
            _closingViaButton = true;
            this.DialogResult = DialogResult.Abort;
            this.Close();
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(tbX.Text, out var x)) { MessageBox.Show("Invalid x"); return; }
            if (!double.TryParse(tbY.Text, out var y)) { MessageBox.Show("Invalid y"); return; }
            if (!double.TryParse(tbZ.Text, out var z)) { MessageBox.Show("Invalid z"); return; }

            var ok = Math.Abs(x - solution[0]) < 1e-3 && Math.Abs(y - solution[1]) < 1e-3 && Math.Abs(z - solution[2]) < 1e-3;
            if (ok)
            {
                _closingViaButton = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Incorrect solution. Try again.");
            }
        }

        private void RenderSystem()
        {
            var lines = new string[3];
            for (int i = 0; i < 3; i++)
            {
                var line = "";
                for (int j = 0; j < 3; j++)
                {
                    var coef = A[i, j];
                    var varname = j == 0 ? "x" : j == 1 ? "y" : "z";
                    if (j == 0)
                        line += $"{coef} {varname} ";
                    else
                        line += (coef >= 0 ? "+ " : "- ") + Math.Abs(coef) + " " + varname + " ";
                }
                line += "= " + b[i];
                lines[i] = line;
            }

            lblProblem.Text = string.Join("\n", lines);
        }

        private void GenerateSystem()
        {
            var rnd = new Random();
            while (true)
            {
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        A[i, j] = rnd.Next(-5, 6);

                var det = Determinant3(A);
                if (Math.Abs(det) < 1e-6) continue;

                solution[0] = rnd.Next(-10, 11);
                solution[1] = rnd.Next(-10, 11);
                solution[2] = rnd.Next(-10, 11);
                for (int i = 0; i < 3; i++)
                    b[i] = A[i, 0] * solution[0] + A[i, 1] * solution[1] + A[i, 2] * solution[2];
                break;
            }
        }

        private double Determinant3(double[,] m)
        {
            return m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
        }
    }
}