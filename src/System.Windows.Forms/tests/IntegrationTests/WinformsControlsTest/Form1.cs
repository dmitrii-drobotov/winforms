using System.Windows.Forms;

namespace WinformsControlsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            listView1.AllowColumnReorder = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Columns.RemoveAt(0);
            //listView1.RecreateHandleInternal();
            //listView1.RedrawItems(0, listView1.Items.Count - 1, true);
        }
    }
}
