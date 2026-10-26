using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Windows.Forms;

namespace BetterScreenTimePC
{
    public partial class ChallengeForm : Form
    {
        private bool _closingViaButton;
        
        public string HeaderText { get; set; } //1/5

        public ChallengeForm()
        {
            InitializeComponent();
            this.Load += ChallengeForm_Load;
            btnSubmit.Click += BtnSubmit_Click;
            btnGiveUp.Click += BtnGiveUp_Click;
            this.FormClosing += ChallengeForm_FormClosing;

            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;

            // covers every monitor, not just the primary one
        }

        private void ChallengeForm_Load(object sender, EventArgs e)
        {
            switch (MainForm.option)
            {
                case 0:
                    GenerateSystem();
                    textBox1.Visible = false;
                    break;
                case 1:
                    GenerateSequence();
                    btnSubmit.Location = new System.Drawing.Point(916, 652);
                    btnGiveUp.Location = new System.Drawing.Point(916, 712);
                    break;
                default:
                    break;
            }
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
            switch (MainForm.option)
            {
                case 0:
                    if (!int.TryParse(tbX.Text, out int x)) { MessageBox.Show("Invalid x"); return; }
                    if (!int.TryParse(tbY.Text, out int y)) { MessageBox.Show("Invalid y"); return; }
                    if (!int.TryParse(tbZ.Text, out int z)) { MessageBox.Show("Invalid z"); return; }
                    if (x == expectedX && y == expectedY && z == expectedZ)
                    {
                        _closingViaButton = true;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Incorrect solution. Try again.");
                    }
                    break;
                case 1:
                    if(textBox1.Text.Trim()==sequence)
                        _closingViaButton = true;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    break;
                default:
                    break;
            }
            
        }

        string sequence = "";
        private void GenerateSequence()
        {
            label4.Text = HeaderText;
            for (int i = 0;i<300;i++)
                sequence += Convert.ToChar(a.Next(33, 127));
            lblProblem.Text = sequence;
        }

        private int expectedX = 0;
        private int expectedY = 0;
        private int expectedZ = 0;
        private string task;
        Random a = new Random();
        private void GenerateSystem()
        {
            label4.Text = HeaderText;
            task = "";
            int x, y, z;
            int x1, x2, x3;
            int y1, y2, y3;
            int z1, z2, z3;
            x = a.Next(-5, 6);
            y = a.Next(-5, 6);
            z = a.Next(-5, 6);
            expectedX = x;
            expectedY = y;
            expectedZ = z;
            x1 = a.Next(-5, 6);
            if (x1 == 1) task += "x";
            else if (x1 == -1) task += "-x";
            else if (x1 != 0) task += x1 + "x";
            y1 = a.Next(-5, 6);
            if (y1 == 1) task += "+y";
            else if (y1 == -1) task += "-y";
            else if (y1 > 0) task += "+" + y1 + "y";
            else if (y1 < 0) task += y1 + "y";
            z1 = a.Next(-5, 6);
            if (z1 == 1) task += "+z";
            else if (z1 == -1) task += "-z";
            else if (z1 > 0) task += "+" + z1 + "z";
            else if (z1 < 0) task += z1 + "z";
            int rez1 = x1 * x + y1 * y + z1 * z;
            task += "=" + rez1 + "\n";
            x2 = a.Next(-5, 6);
            if (x2 == 1) task += "x";
            else if (x2 == -1) task += "-x";
            else if (x2 != 0) task += x2 + "x";
            y2 = a.Next(-5, 6);
            if (y2 == 1) task += "+y";
            else if (y2 == -1) task += "-y";
            else if (y2 > 0) task += "+" + y2 + "y";
            else if (y2 < 0) task += y2 + "y";
            z2 = a.Next(-5, 6);
            if (z2 == 1) task += "+z";
            else if (z2 == -1) task += "-z";
            else if (z2 > 0) task += "+" + z2 + "z";
            else if (z2 < 0) task += z2 + "z";
            int rez2 = x2 * x + y2 * y + z2 * z;
            task += "=" + rez2 + "\n";
            x3 = a.Next(-5, 6);
            if (x3 == 1) task += "x";
            else if (x3 == -1) task += "-x";
            else if (x3 != 0) task += x3 + "x";
            y3 = a.Next(-5, 6);
            if (y3 == 1) task += "+y";
            else if (y3 == -1) task += "-y";
            else if (y3 > 0) task += "+" + y3 + "y";
            else if (y3 < 0) task += y3 + "y";
            z3 = a.Next(-5, 6);
            if (z3 == 1) task += "+z";
            else if (z3 == -1) task += "-z";
            else if (z3 > 0) task += "+" + z3 + "z";
            else if (z3 < 0) task += z3 + "z";
            int rez3 = x3 * x + y3 * y + z3 * z;
            task += "=" + rez3 + "\n";
            lblProblem.Text = task;
            //if (feedbackText != null) feedbackText.setText("");
        }
    }
}