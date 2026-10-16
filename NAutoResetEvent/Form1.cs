using NCommonUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NAutoResetEventSample
{
    public partial class Form1 : Form
    {
        int no = 0;
        NAutoResetEvent _event = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void btn_Set_Click(object sender, EventArgs e)
        {
            _event?.Set();
        }

        private async void btn_Await_Click(object sender, EventArgs e)
        {
            if (_event == null)
            {
                _event = new NAutoResetEvent(chk_InitSet.Checked, chk_KeepOrder.Checked);
            }
            int n = ++no;
            this.lbl_AwaitNo.Text = n.ToString();
            if(await _event.Wait())
            {
                this.textBox1.Text = this.textBox1.Text+$"Fire{n}\r\n";
            }
            else
            {
                this.textBox1.Text = this.textBox1.Text+$"Cancel{n}\r\n";
            }
        }

        private void btn_Reset_Click(object sender, EventArgs e)
        {
            if (_event == null)
            {
                _event = new NAutoResetEvent(chk_InitSet.Checked, chk_KeepOrder.Checked);
            }
            else
            {
                _event.Reset(chk_InitSet.Checked, chk_KeepOrder.Checked);
            }
            no = 0;
            this.lbl_AwaitNo.Text = no.ToString();
            this.textBox1.Text = "";
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            _event?.Cancel();
        }
    }
}
