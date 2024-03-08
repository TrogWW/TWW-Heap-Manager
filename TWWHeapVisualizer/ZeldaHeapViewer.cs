using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Extensions;
using TWWHeapVisualizer.FormElements;
using TWWHeapVisualizer.Heap;
using TWWHeapVisualizer.Heap.DataStructTypes.GhidraParsing;
using Timer = System.Windows.Forms.Timer;

namespace TWWHeapVisualizer
{
    public partial class ZeldaHeapViewer : Form
    {
        // Declare form controls and variables
        private HeapListView heapListView;
        private MenuStrip menuStrip;
        private Panel mainPanel;
        private LoadingForm loadingForm;
        private ComboBox comboBoxMemoryBlockType;
        private string ghidraCsvDirectoryPath = "";
        private Timer addressLoopTimer = new Timer();

        // Constructor
        public ZeldaHeapViewer()
        {
            InitializeComponent();
            InitializeComponents();
        }

        // Method to initialize form components
        private void InitializeComponents()
        {
            // Initialize the top menu, main panel, heap list view, and other necessary components
            InitializeTopMenu();
            InitializeMainPanel();
            InitializeHeapListView();
            InitializeComboBoxMemoryBlockType();

            addressLoopTimer.Interval = 1000 / 10; // Set the interval for the timer
            addressLoopTimer.Tick += heapListView.UpdateList; // Add event handler for the timer tick
            this.ResizeBegin += ZeldaHeapViewer_ResizeBegin; // Add event handler for form resize begin
            this.ResizeEnd += ZeldaHeapViewer_ResizeEnd; // Add event handler for form resize end
        }

        // Method to initialize the main panel of the form
        private void InitializeMainPanel()
        {
            mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(0, menuStrip.Height, 0, 0); // Add padding to accommodate the menu strip
            mainPanel.Controls.Add(heapListView);
            Controls.Add(mainPanel);
        }

        // Method to initialize the heap list view
        private void InitializeHeapListView()
        {
            // Create and configure the heap list view control
            heapListView = new HeapListView(addressLoopTimer);
            heapListView.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(heapListView);
        }

        private void InitializeTopMenu()
        {
            // Create MenuStrip control
            this.menuStrip = new MenuStrip();

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");

            // File menu items
            ToolStripMenuItem connectToDolphinItem = new ToolStripMenuItem("Connect To Dolphin");
            ToolStripMenuItem importGhidraCsvsItem = new ToolStripMenuItem("Import Ghidra CSVs");
            ToolStripMenuItem twwVersionItem = new ToolStripMenuItem("TWW Version");
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");

            // Help menu items
            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("Github");

            // Add file menu items
            fileMenu.DropDownItems.Add(connectToDolphinItem);
            fileMenu.DropDownItems.Add(importGhidraCsvsItem);
            fileMenu.DropDownItems.Add(twwVersionItem);
            fileMenu.DropDownItems.Add(exitMenuItem);

            // Add help menu items
            helpMenu.DropDownItems.Add(aboutMenuItem);

            // Add event handlers for menu items
            connectToDolphinItem.Click += ConnectToDolphinItem_Click;
            importGhidraCsvsItem.Click += ImportGhidraCsvs_Click;
            exitMenuItem.Click += ExitMenuItem_Click;
            aboutMenuItem.Click += AboutMenuItem_Click;

            // Create a secondary menu with NTSC-U and JP options
            ContextMenuStrip secondaryMenu = new ContextMenuStrip();

            // Add menu items for each version
            ToolStripMenuItem ntscuMenuItem = new ToolStripMenuItem("NTSC-U");
            ntscuMenuItem.Click += (s, args) => SwitchVersionClicked("NTSC-U");

            ToolStripMenuItem jpMenuItem = new ToolStripMenuItem("JP");
            jpMenuItem.Click += (s, args) => SwitchVersionClicked("JP");

            secondaryMenu.Items.Add(ntscuMenuItem);
            secondaryMenu.Items.Add(jpMenuItem);

            // Set the secondary menu as the drop-down for the "TWW Version" menu item
            twwVersionItem.DropDown = secondaryMenu;

            // Add menus to MenuStrip
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(helpMenu);

            // Set MenuStrip as the menu of the form
            this.MainMenuStrip = menuStrip;

            // Add MenuStrip to the form's controls
            this.Controls.Add(menuStrip);
        }

        // Method to initialize the ComboBox for memory block type selection
        private void InitializeComboBoxMemoryBlockType()
        {
            // Create ComboBox for memory block type selection
            comboBoxMemoryBlockType = new ComboBox();
            comboBoxMemoryBlockType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxMemoryBlockType.Items.AddRange(new string[] { "Free", "Used", "All" }); // Add memory block type options
            comboBoxMemoryBlockType.SelectedIndex = 2; // Set default selection to "All"
            comboBoxMemoryBlockType.SelectedIndexChanged += ComboBoxMemoryBlockType_SelectedIndexChanged; // Handle selection change event

            // Create ToolStripControlHost to host the ComboBox
            ToolStripControlHost hostComboBox = new ToolStripControlHost(comboBoxMemoryBlockType);
            hostComboBox.AutoSize = false; // Set AutoSize to false to allow manual sizing
            hostComboBox.Size = new Size(250, menuStrip.Height); // Set the size of the host

            // Create CheckBox for filtering
            CheckBox checkBoxFilter = new CheckBox();
            checkBoxFilter.Text = "Hide Empty Name Data";
            checkBoxFilter.CheckedChanged += CheckBoxFilter_CheckedChanged; // Handle checkbox change event

            // Create ToolStripControlHost to host the CheckBox
            ToolStripControlHost hostCheckBox = new ToolStripControlHost(checkBoxFilter);
            hostCheckBox.AutoSize = false; // Set AutoSize to false to allow manual sizing
            hostCheckBox.Size = new Size(200, menuStrip.Height); // Set the size of the host

            // Add padding to the hosts
            hostComboBox.Padding = new Padding(30, 0, 0, 0); // Left padding for ComboBox
            hostCheckBox.Padding = new Padding(0, 0, 30, 0); // Right padding for CheckBox
            hostComboBox.Alignment = ToolStripItemAlignment.Right;
            hostCheckBox.Alignment = ToolStripItemAlignment.Right;

            // Add the ComboBox host to the MenuStrip
            menuStrip.Items.Add(hostComboBox); // Add ComboBox to the menu strip

            // Add the CheckBox host to the MenuStrip
            menuStrip.Items.Add(hostCheckBox); // Add CheckBox to the menu strip
        }
        // Event handler for ComboBox memory block type selection change
        private void ComboBoxMemoryBlockType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Handle memory block type selection change
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                string selectedOption = comboBox.SelectedItem.ToString(); // Assuming the selected item is a string
                heapListView.selectedOption = selectedOption;
                //heapListView.ApplyFilter(selectedOption); // Pass the selected option to the HeapListView class
            }
        }
        // Event handler for the "TWW Version" menu item
        private void TWWVersionMenuItem_Click(object sender, EventArgs e)
        {
            // Show a secondary menu with options to choose between NTSC-U and JP
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;

            // Create a secondary menu with NTSC-U and JP options
            ContextMenuStrip secondaryMenu = new ContextMenuStrip();

            // Add menu items for each version
            ToolStripMenuItem ntscuMenuItem = new ToolStripMenuItem("NTSC-U");
            ntscuMenuItem.Click += (s, args) => SwitchVersionClicked("NTSC-U");

            ToolStripMenuItem jpMenuItem = new ToolStripMenuItem("JP");
            jpMenuItem.Click += (s, args) => SwitchVersionClicked("JP");

            // Add menu items to the secondary menu
            secondaryMenu.Items.Add(ntscuMenuItem);
            secondaryMenu.Items.Add(jpMenuItem);

            // Set the secondary menu as the drop-down for the primary menu item
            menuItem.DropDown = secondaryMenu;
        }
        private void SwitchVersionClicked(string version)
        {
            // Handle the version switch (e.g., perform actions based on the selected version)
            switch (version)
            {
                case "NTSC-U":
                    ActorData.fopActQueueHead = 0x80372028;
                    ActorData.zeldaHeapPtr = 0x803F6928;
                    ActorData.objectNameTableAddress = 0x80372818;
                    ActorData.Instance.InitializeData();
                    break;
                case "JP":
                    ActorData.fopActQueueHead = 0x803654CC;
                    ActorData.zeldaHeapPtr = 0x803E9E00;
                    ActorData.objectNameTableAddress = 0x80365CB8;
                    ActorData.Instance.InitializeData();
                    break;
                default:
                    break;
            }
        }

        // Event handler for CheckBox filter change
        private void CheckBoxFilter_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                // Apply filter based on the checked state of the checkbox
                if (checkBox.Checked)
                {
                    // Filter logic when checkbox is checked
                    // For example, filter all rows where the data column is empty
                    heapListView.ApplyDataFilter();
                }
                else
                {
                    // Clear the filter when checkbox is unchecked
                    heapListView.DisableDataFilter();
                }
            }
        }

        // Event handler for "Connect To Dolphin" menu item
        private void ConnectToDolphinItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            bool attached = Memory.m_hDolphin != IntPtr.Zero;

            if (!attached)
            {
                AttachToDolphin(menuItem);
            }
            else
            {
                DetachFromDolphin(menuItem);
            }
        }

        // Method to handle attachment to Dolphin
        private void AttachToDolphin(ToolStripMenuItem menuItem)
        {
            // Get Dolphin processes
            var processes = Process.GetProcessesByName("Dolphin");

            if (processes.Length == 0)
            {
                MessageBox.Show("No Dolphin process found");
                return;
            }
            else if (processes.Length == 1)
            {
                AttachAndStartTimer(menuItem, processes[0]);
            }
            else
            {
                Process chosenProcess = ChooseProcess(processes);
                if (chosenProcess != null)
                {
                    AttachAndStartTimer(menuItem, chosenProcess);
                }
            }
        }

        // Method to attach to Dolphin and start the timer
        private void AttachAndStartTimer(ToolStripMenuItem menuItem, Process process)
        {
            if (Memory.Attach(process))
            {
                Memory.ReadMemory((ulong)0x80000000, 8); //something weird is happening, I need to read memory once before it actually pulls data
                string gameVersion = Memory.ReadMemoryString((ulong)0x80000000);
                if(gameVersion == "GZLJ01")
                {
                    SwitchVersionClicked("JP");
                }
                else if(gameVersion == "GZLE01")
                {
                    SwitchVersionClicked("NTSC-U");
                }
                else
                {
                    MessageBox.Show($"Game version {gameVersion} is not supported.");
                }
                addressLoopTimer.Start(); // Start the timer for address loop
                menuItem.Text = "Disconnect from Dolphin"; // Update menu item text
            }
            else
            {
                MessageBox.Show("Unable to locate Dolphin process");
            }
        }

        // Method to detach from Dolphin
        private void DetachFromDolphin(ToolStripMenuItem menuItem)
        {
            addressLoopTimer.Stop(); // Stop the timer
            Memory.m_hDolphin = IntPtr.Zero;
            menuItem.Text = "Connect to Dolphin"; // Update menu item text
        }

        // Method to choose a process from multiple Dolphin processes
        private Process ChooseProcess(Process[] processes)
        {
            var processChooserForm = new ProcessChooserForm(processes);
            var result = processChooserForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                return processChooserForm.SelectedProcess;
            }
            return null;
        }

        // Event handler for "Import Ghidra CSVs" menu item
        private void ImportGhidraCsvs_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select the folder containing the Ghidra CSV files";

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolderPath = folderBrowserDialog.SelectedPath;
                    ghidraCsvDirectoryPath = selectedFolderPath;
                    loadingForm = new LoadingForm("Importing Ghidra Structs...");
                    loadingForm.Show();
                    Thread loadDataThread = new Thread(LoadGhidraData);
                    loadDataThread.Start();
                }
            }
        }

        // Method to load Ghidra data
        private void LoadGhidraData()
        {
            try
            {
                // Load Ghidra CSV files and parse data
                ActorData.Instance.DataTypes = GhidraStructParser.ParseDataTypes(ghidraCsvDirectoryPath);

                // Close the loading form
                if (loadingForm != null && !loadingForm.IsDisposed)
                {
                    loadingForm.Invoke(new Action(() =>
                    {
                        loadingForm.Close();
                    }));
                }
            }
            catch (Exception ex)
            {
                // Display error message
                MessageBox.Show("An error occurred while loading data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Close the loading form
                if (loadingForm != null && !loadingForm.IsDisposed)
                {
                    loadingForm.Invoke(new Action(() =>
                    {
                        loadingForm.Close();
                    }));
                }
            }
        }

        // Event handler for "About" menu item
        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            // GitHub repository URL
            string githubUrl = "https://github.com/dolphin-emu/dolphin/wiki/GameCube-ActionReplay-Code-Types-(Simple-version)";

            try
            {
                // Open the GitHub URL in the default web browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = githubUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open web browser: " + ex.Message);
            }
        }

        // Event handler for form resize end
        private void ZeldaHeapViewer_ResizeEnd(object sender, EventArgs e)
        {
            addressLoopTimer.Tick += heapListView.UpdateList;
        }

        // Event handler for form resize begin
        private void ZeldaHeapViewer_ResizeBegin(object sender, EventArgs e)
        {
            addressLoopTimer.Tick -= heapListView.UpdateList;
        }
        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
