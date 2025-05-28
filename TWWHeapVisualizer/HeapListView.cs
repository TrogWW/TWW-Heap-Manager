﻿using System.Diagnostics;
using System.Windows.Forms.VisualStyles;
using TWWHeapVisualizer.Heap.MemoryBlocks;
using Timer = System.Windows.Forms.Timer;
using TWWHeapVisualizer.FormElements;
using TWWHeapVisualizer.Heap;
using Microsoft.VisualBasic.Devices;
using TWWHeapVisualizer.DataStructTypes;
using TWWHeapVisualizer.Helpers;

namespace TWWHeapVisualizer
{

    // Helper class to compare ListViewItems for sorting
    public class HeapListViewComparer : IComparer<IMemoryBlock>
    {
        private int columnToSort;
        private SortOrder sortOrder;

        public HeapListViewComparer(int columnToSort, SortOrder sortOrder)
        {
            this.columnToSort = columnToSort;
            this.sortOrder = sortOrder;
        }

        public int Compare(IMemoryBlock x, IMemoryBlock y)
        {
            // Get the text values for comparison based on the column to sort
            string textX = GetTextValue(x);
            string textY = GetTextValue(y);
            // Parse numeric values for specific columns
            if (columnToSort == 0 || columnToSort == 3 || columnToSort == 4) // Assuming column 0, 3, and 4 should be sorted numerically
            {
                int valueX, valueY;
                if (!int.TryParse(textX, out valueX))
                {
                    // Handle parsing error if necessary
                }
                if (!int.TryParse(textY, out valueY))
                {
                    // Handle parsing error if necessary
                }

                // Perform numeric comparison
                int result = valueX.CompareTo(valueY);

                // Adjust result based on sort order
                if (sortOrder == SortOrder.Descending)
                {
                    result = -result;
                }

                return result;
            }
            else
            {
                // Perform string comparison for other columns
                int result = String.Compare(textX.ToLower(), textY.ToLower());

                // If the primary comparison is a tie, compare by startAddress
                if (result == 0 && columnToSort != 1 && columnToSort != 2 && columnToSort != 3) // Assuming 1 and 2 are startAddress and endAddress columns
                {
                    // Compare by startAddress if the primary comparison is a tie
                    result = x.startAddress.CompareTo(y.startAddress);
                }

                // Adjust result based on sort order
                if (sortOrder == SortOrder.Descending)
                {
                    result = -result;
                }

                return result;
            }
        }

        private string GetTextValue(IMemoryBlock block)
        {
            switch (block)
            {
                case UsedMemoryBlock usedBlock:
                    return GetUsedMemoryBlockTextValue(usedBlock);
                case FreeMemoryBlock freeBlock:
                    return GetFreeMemoryBlockTextValue(freeBlock);
                // Add more cases for other types of memory blocks as needed
                default:
                    return ""; // Default to empty string if block type is unknown
            }
        }

        private string GetUsedMemoryBlockTextValue(UsedMemoryBlock block)
        {
            switch (columnToSort)
            {
                case 0: // Index column
                    return block.index.ToString();
                case 1: // Start Address column
                    return block.startAddress.ToString("X"); // Format as hexadecimal
                case 2: // End Address column
                    return block.endAddress.ToString("X"); // Format as hexadecimal
                case 3: // BsPcId
                    return block.bsPcId.ToString();
                case 4: // Size column
                    return block.size.ToString();
                case 5: // Status column
                    return "Used";
                case 6: // Data column
                    return block.data?.ToString() ?? "";
                default:
                    return ""; // Default to empty string
            }
        }

        private string GetFreeMemoryBlockTextValue(FreeMemoryBlock block)
        {
            switch (columnToSort)
            {
                case 0: // Index column for FreeMemoryBlock
                    return "0"; // Sort FreeMemoryBlock items at the top
                case 1: // Start Address column
                    return block.startAddress.ToString("X"); // Format as hexadecimal
                case 2: // End Address column
                    return block.endAddress.ToString("X"); // Format as hexadecimal
                case 3:
                    return "";
                case 4: // Size column
                    return block.size.ToString();
                case 5: // Status column
                    return "Free";
                case 6: // Data column
                    return "";
                default:
                    return ""; // Default to empty string
            }
        }
    }



    public class HeapListView : ListView
    {
        public List<IMemoryBlock> filteredMemoryBlocks;
        public uint heapSize;
        public uint dataEnd;
        public string selectedOption = "All";
        public bool filterEmptyNames = false;
        private Timer _timer;
        private int sortColumn = -1;
        private SortOrder sortOrder = SortOrder.None;

        public MemoryBlockCollection heap { get; set; }
        public HeapListView(Timer timer)
        {
            InitializeListView();
            this._timer = timer;
        }

        private void InitializeListView()
        {
            this.Dock = DockStyle.Fill;
            this.HideSelection = true;
            this.DoubleBuffered = true;
            this.View = View.Details;
            this.HeaderStyle = ColumnHeaderStyle.Clickable; // Make column headers clickable
            this.VirtualMode = true;
            this.OwnerDraw = true;
            this.RetrieveVirtualItem += HeapListView_RetrieveVirtualItem;
            this.DrawItem += HeapListView_DrawItem;
            this.DrawColumnHeader += HeapListView_DrawColumnHeader;
            this.DrawSubItem += HeapListView_DrawSubItem;
            this.ColumnClick += HeapListView_ColumnClick;
            this.Resize += HeapListView_Resize; // Subscribe to the Resize event
            // Add columns with appropriate headers and widths
            this.Columns.Add("Index", 50, HorizontalAlignment.Left);
            this.Columns.Add("Start Address", 100, HorizontalAlignment.Left);
            this.Columns.Add("End Address", 100, HorizontalAlignment.Left);
            this.Columns.Add("BsPcId", 150, HorizontalAlignment.Left);
            this.Columns.Add("Size", 80, HorizontalAlignment.Left);
            this.Columns.Add("Status", 80, HorizontalAlignment.Left);

            //this.Columns.Add("Rel Pointer", 150, HorizontalAlignment.Left);
            this.Columns.Add("Data", 150, HorizontalAlignment.Left);

            this.Columns.Add("", 100, HorizontalAlignment.Left);
        }

        private void HeapListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // Let the system draw the rest of the item
            // Set the background color based on the tag of the ListViewItem
            if (e.Item.Tag != null)
            {
                IMemoryBlock block = e.Item.Tag as IMemoryBlock;
               // string tag = e.Item.Tag.ToString();
                if (block is UsedMemoryBlock uBlock)
                {
                    bool isFilled = uBlock.filled;
                    IMemoryBlock hoveredBlock = null;
                    Point localMousePosition = this.PointToClient(ZeldaHeapViewer.MousePosition);
                    var hit = this.HitTest(localMousePosition);
                    if(hit.Item != null)
                    {
                        int index = hit.Item.SubItems.IndexOf(hit.SubItem);

                        if (index + 1 == hit.Item.SubItems.Count)
                        {
                            hoveredBlock = (IMemoryBlock)hit.Item.Tag;
                        }
                    }
                    

                    //IMemoryBlock hoveredBlock = GetMouseHoveredMemoryBlock(localMousePosition);
                    Color backColor;
                    if(hoveredBlock != null && hoveredBlock.Equals(block))
                    {
                        backColor = Color.FromArgb(255, 157, 137);
                    }
                    else
                    {
                        if (isFilled)
                        {
                            backColor = Color.FromArgb(160, 120, 180); // Define the background color for the button
                        }
                        else
                        {
                            backColor = Color.FromArgb(255, 125, 98); // Define the background color for the button
                        }
                    }
                        // Red background for "Used" items
                    using (SolidBrush brush = new SolidBrush(backColor)) // Adjust RGB values as needed
                    {
                        e.Graphics.FillRectangle(brush, e.Bounds);
                    }
                }
                else if (block is FreeMemoryBlock)
                {
                    // Green background for "Free" items
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(164, 255, 173))) // Adjust RGB values as needed
                    {
                        e.Graphics.FillRectangle(brush, e.Bounds);
                    }
                }
            }
            //e.DrawText();
        }
        private void HeapListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // Handle drawing for other columns
            if (e.ColumnIndex != this.Columns.Count - 1)
            {
                //e.DrawDefault = true;
                e.DrawText();
                return;
            }

            // Draw the button in the last column
            // Adjust the button appearance and position as needed
            IMemoryBlock block = e.Item.Tag as IMemoryBlock;
            if (block is UsedMemoryBlock uBlock)
            {
                bool isFilled = uBlock.filled;
                int buttonPadding = 0;
                Rectangle buttonBounds = new Rectangle(e.Bounds.Left + buttonPadding, e.Bounds.Top + buttonPadding,
                                                        e.Bounds.Width - 2 * buttonPadding, e.Bounds.Height - 2 * buttonPadding);

                Point localMouseLocation = this.PointToClient(ZeldaHeapViewer.MousePosition);
                Image icon = null;
                if (buttonBounds.Contains(localMouseLocation))
                {
                    icon = Properties.Resources.green_arrow_hover;
                }
                else
                {
                    icon = Properties.Resources.green_arrow; // Load your icon from resources or file
                }
                //IMemoryBlock hoveredBlock = GetMouseHoveredMemoryBlock(localMouseLocation);
                Point localMousePosition = this.PointToClient(ZeldaHeapViewer.MousePosition);
                var hit = this.HitTest(localMousePosition);
                IMemoryBlock hoveredBlock = null;
                if (hit.Item != null)
                {
                    int index = hit.Item.SubItems.IndexOf(hit.SubItem);
                    
                    if (index + 1 == hit.Item.SubItems.Count)
                    {
                        hoveredBlock = (IMemoryBlock)hit.Item.Tag;
                    }
                }

                Color buttonBackColor;
                if (hoveredBlock != null && hoveredBlock.Equals(block))
                {
                    buttonBackColor = Color.FromArgb(255, 157, 137);
                }
                else
                {
                    if (isFilled)
                    {
                        buttonBackColor = Color.FromArgb(160, 120, 180); // Define the background color for the button
                    }
                    else
                    {
                        buttonBackColor = Color.FromArgb(255, 125, 98); // Define the background color for the button
                    }
                    
                }
                // Draw the button
                ButtonRenderer.DrawButton(e.Graphics, buttonBounds, e.Item.Selected ? PushButtonState.Pressed : PushButtonState.Normal);

                // Calculate the maximum icon size based on the row's height
                int iconSize = Math.Min(buttonBounds.Height, e.Bounds.Height) - 2 * buttonPadding;

                // Calculate the bounds for the icon while centering it within the button area
                int iconX = buttonBounds.Left + (buttonBounds.Width - iconSize) / 2;
                int iconY = buttonBounds.Top + (buttonBounds.Height - iconSize) / 2;
                Rectangle iconBounds = new Rectangle(iconX, iconY, iconSize, iconSize);
                // Draw the background color for the button
                using (SolidBrush brush = new SolidBrush(buttonBackColor))
                {
                    e.Graphics.FillRectangle(brush, buttonBounds);
                }
                // Draw the icon
                e.Graphics.DrawImage(icon, iconBounds);

            }

            e.DrawText();
        }
        // Event handler for button click
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            var hit = this.HitTest(e.Location);
            if(hit.Item == null)
            {
                return;
            }
            int index = hit.Item.SubItems.IndexOf(hit.SubItem);
            IMemoryBlock hoveredBlock = null;
            if (index + 1 == hit.Item.SubItems.Count)
            {
                if(hit.Item.Tag is UsedMemoryBlock)
                {
                    hoveredBlock = (UsedMemoryBlock)hit.Item.Tag;
                }
                
            }
            if(hoveredBlock != null)
            {
                Debug.WriteLine($"Button clicked for memory block: {hoveredBlock.startAddress.ToString("X")}");
                ShowMemoryDataGridViewForm(hoveredBlock);
            }
        }
        private void HeapListView_Resize(object sender, EventArgs e)
        {
            AdjustColumnWidths(); // Call the method to adjust column widths
        }
        private void AdjustColumnWidths()
        {
            if (this.Columns.Count == 0)
                return;

            int totalColumnWidth = this.ClientSize.Width; // Get the total width of the ListView client area
            int buttonColumnWidth = 50;
            // Calculate the total width excluding the button column
            int totalWidthExceptButtonColumn = totalColumnWidth - buttonColumnWidth;

            // Calculate the total number of columns excluding the button column
            int numColumnsExceptButtonColumn = this.Columns.Count - 1;
            int buttonColumnIndex = this.Columns.Count - 1;
            // Calculate the width for each column except the button column
            int columnWidth = totalWidthExceptButtonColumn / numColumnsExceptButtonColumn;

            // Set the width for each column except the button column
            for (int i = 0; i < this.Columns.Count; i++)
            {
                if (i != buttonColumnIndex) // Exclude the button column
                {
                    this.Columns[i].Width = columnWidth;
                }
            }

            // Set the width for the button column
            if (buttonColumnIndex >= 0 && buttonColumnIndex < this.Columns.Count)
            {
                this.Columns[buttonColumnIndex].Width = buttonColumnWidth;
            }
        }




        private void HeapListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            int padding = 4; // Adjust the padding size as needed

            // Calculate the text bounds with padding and adjusted height
            Rectangle textBounds = e.Bounds;
            textBounds.X += padding;
            textBounds.Width -= 2 * padding;
            textBounds.Y += 3; // Adjust the Y-coordinate to move the text down
            // Define the background color for the header

            Point localMouseLocation = this.PointToClient(ZeldaHeapViewer.MousePosition);
            Color headerColor;
            if (textBounds.Contains(localMouseLocation))
            {
                headerColor = Color.DarkGray;
            }
            else
            {
                headerColor = Color.FromArgb(240, 240, 240);
            }
           
            // Fill the header background with the specified color
            using (SolidBrush brush = new SolidBrush(headerColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Define padding for the text


            // Draw the column header text with padding
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Center;

                // Draw the text with the specified font and color
                using (SolidBrush textBrush = new SolidBrush(Color.Black)) // Adjust text color if needed
                {
                    e.Graphics.DrawString(e.Header.Text, this.Font, textBrush, textBounds, sf);
                }
            }
        }
        private void HeapListView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            try
            {
                if(filteredMemoryBlocks.Count == 0)
                {
                    UpdateFilteredList();           //make sure the filtered list has been updated
                }
                int itemCount = filteredMemoryBlocks.Count;
                int validIndex = Math.Min(e.ItemIndex, itemCount - 1); // Ensure index is within the valid range
                IMemoryBlock block = filteredMemoryBlocks[validIndex];
                var display = block.GetDisplayInfo();
                if(display.SubItems.Count != this.Columns.Count)
                {
                    throw new Exception("Attempted to add an item with invalid number of sub items.");
                }
                e.Item = block.GetDisplayInfo();
                e.Item.Tag = block;

            }
            catch (Exception ex)
            {

                throw;
            }

        }
        public void ApplyDataFilter()
        {
            this.filterEmptyNames = true;
        }
        public void DisableDataFilter()
        {
            this.filterEmptyNames = false;
        }
        // Apply filtering based on current filter option
        private List<IMemoryBlock> ApplyFilter(List<IMemoryBlock> blocks)
        {
            switch (selectedOption)
            {
                case "Free":
                    return blocks.Where(b => b is FreeMemoryBlock).ToList();
                case "Used":
                    if (filterEmptyNames)
                    {
                        // Filter UsedMemoryBlock instances based on empty names
                        //var test = blocks.Where(b => b is UsedMemoryBlock && !string.IsNullOrEmpty(((UsedMemoryBlock)b).data?.ToString())).ToList();
                        return blocks.Where(b => b is UsedMemoryBlock && !string.IsNullOrEmpty(((UsedMemoryBlock)b).data?.ToString())).ToList();
                    }
                    else
                    {
                        // Return all UsedMemoryBlock instances
                        return blocks.Where(b => b is UsedMemoryBlock).ToList();
                    }
                default:
                    if (filterEmptyNames)
                    {
                        return blocks.Where(b => b is FreeMemoryBlock || (b is UsedMemoryBlock && !string.IsNullOrEmpty(((UsedMemoryBlock)b).actor?.ToString()))).ToList();
                    }
                    else
                    {
                        return blocks;
                    }
                    
            }
        }
        // Apply sorting based on current sort column and order
        private void SortMemoryBlocks(List<IMemoryBlock> blocks)
        {
            if (sortColumn >= 0 && sortColumn < this.Columns.Count)
            {
                blocks.Sort(new HeapListViewComparer(sortColumn, sortOrder));
            }
        }
        // Event handler for column click to initiate sorting
        private void HeapListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine which column was clicked and toggle the sort order
            if (e.Column == sortColumn)
            {
                sortOrder = sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                sortColumn = e.Column;
                sortOrder = SortOrder.Ascending;
            }

            // Perform sorting based on the clicked column and sort order
            SortMemoryBlocks(heap.blocks);

            // Refresh the ListView to update the display
            this.Refresh();
        }

        public void UpdateFilteredList()
        {
            filteredMemoryBlocks = ApplyFilter(heap.blocks);
            if(filteredMemoryBlocks.Count == 0)
            {
                filteredMemoryBlocks = heap.blocks;
            }

            SortMemoryBlocks(filteredMemoryBlocks);
        }
        public void UpdateList(object sender, EventArgs e)
        {
            if(heap == null)
            {
                return;
            }
            heap.UpdateBlocks();

            //GetFreeBlocks();
            UpdateFilteredList();
            // Update the visualizer information
            DisplayVisualizerInfo();
        }
        public void DisplayVisualizerInfo()
        {
            try
            {
                this.VirtualListSize = heap.blocks.Count;
                
            }
            catch (Exception)
            {

            }
            finally
            {
                this.Refresh();
            }

        }
        private void ShowMemoryDataGridViewForm(IMemoryBlock memoryBlock)
        {
            UsedMemoryBlock usedMemoryBlock = (UsedMemoryBlock)memoryBlock;
            MemoryDataForm memoryDataGridViewForm = new MemoryDataForm(_timer, usedMemoryBlock);
            // Show the form as a dialog
            memoryDataGridViewForm.Show();
        }
    }
}
