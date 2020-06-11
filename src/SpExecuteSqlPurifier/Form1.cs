using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SpExecuteSqlPurifier
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            SetupComponent();
        }

        private void SetupComponent()
        {
            byte[] fontData = Properties.Resources.UbuntuMono_R;
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fonts.AddMemoryFont(fontPtr, fontData.Length);
            WinApi.AddFontMemResourceEx(fontPtr, (uint)fontData.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);

            myFont = new Font(fonts.Families[0], 10.0F);

            this.Font = myFont;
        }

        private PrivateFontCollection fonts = new PrivateFontCollection();

        Font myFont;

        private void ButtonConvert_Click(object sender, EventArgs e)
        {
            textBoxResult.Text = new Converter() { Variables = checkBoxVariables.Checked }.Convert(textBoxSource.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ButtonConvert_Click(null, null);
        }

        private void buttonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Written by Filip Golewski", "About",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBoxResult.Text);
        }

        private void buttonPaste_Click(object sender, EventArgs e)
        {
            textBoxSource.Text = Clipboard.GetText();
            ButtonConvert_Click(null, null);
        }
    }
}
