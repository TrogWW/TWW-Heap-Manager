using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.FormElements
{
    public partial class LoadingForm : Form
    {
        // Constructor
        public LoadingForm(string text)
        {
            InitializeForm(text);
        }

        private void InitializeForm(string text)
        {
            // Set form properties
            this.Text = "Loading...";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Add label to display loading message
            Label labelLoading = new Label();
            labelLoading.Text = text;
            labelLoading.AutoSize = true;
            labelLoading.Location = new System.Drawing.Point(20, 20);
            this.Controls.Add(labelLoading);

            // Adjust form size based on label size
            this.ClientSize = new System.Drawing.Size(
                Math.Max(labelLoading.Right + 20, 150),
                Math.Max(labelLoading.Bottom + 20, 70)
            );
        }
    }
}
