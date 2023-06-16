using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VectorGrab
{
    public partial class ResponseForm : Form
    {
        public ResponseForm()
        {
            InitializeComponent();
        }

        internal void showCompletion(string completion)
        {
            textBox1.Text = completion;
        }
    }
}
