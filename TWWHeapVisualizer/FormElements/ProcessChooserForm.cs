using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.FormElements
{
    public class ListBoxProcess
    {
        public Process process { get; private set; }
        public ListBoxProcess(Process process)
        {
            this.process = process;
        }
        public override string ToString()
        {
            return $"{process.MainWindowTitle} ({process.Id})";
        }
    }
    public class ProcessChooserForm : Form
    {
        private ListBox processListBox;
        private Button okButton;
        private Button cancelButton;
        private Process[] _processes;
        public Process SelectedProcess { get; private set; }

        public ProcessChooserForm(Process[] processes)
        {
            _processes = processes;
            InitializeComponents();
            LoadProcesses(processes);
        }

        private void InitializeComponents()
        {
            // Set up the form properties
            this.Text = "Choose Dolphin Process";
            this.Size = new Size(300, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create and configure the processListBox
            processListBox = new ListBox();
            processListBox.Dock = DockStyle.Fill;
            processListBox.SelectionMode = SelectionMode.One;

            // Create and configure the OK button
            okButton = new Button();
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Dock = DockStyle.Bottom;
            okButton.Click += OkButton_Click;

            // Create and configure the Cancel button
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Dock = DockStyle.Bottom;
            cancelButton.Click += CancelButton_Click;

            // Add controls to the form
            this.Controls.Add(processListBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
        }


        private void LoadProcesses(Process[] processes)
        {
            foreach (var process in processes)
            {
                //processListBox.Items.Add($"{process.MainWindowTitle} ({process.Id})");
                processListBox.Items.Add(new ListBoxProcess(process));
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (processListBox.SelectedItem != null)
            {
                ListBoxProcess selectedItem = processListBox.SelectedItem as ListBoxProcess;
                int processId = selectedItem.process.Id;
                SelectedProcess = Array.Find(_processes, p => p.Id == processId);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a process.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
