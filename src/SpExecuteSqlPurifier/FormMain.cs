using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SpExecuteSqlPurifier
{
    public partial class FormMain : Form
    {
        public FormMain()
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
            fonts.AddMemoryFont(fontPtr, Properties.Resources.UbuntuMono_R.Length);
            WinApi.AddFontMemResourceEx(fontPtr, (uint)Properties.Resources.UbuntuMono_R.Length, IntPtr.Zero, ref dummy);
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

        private void FormMain_Load(object sender, EventArgs e)
        {
            ButtonConvert_Click(null, null);
        }

        private void ButtonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Written by Filip Golewski", "About",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ButtonCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBoxResult.Text);
        }

        private void ButtonPaste_Click(object sender, EventArgs e)
        {
            textBoxSource.Text = Clipboard.GetText();
            ButtonConvert_Click(null, null);
        }
    }
}
