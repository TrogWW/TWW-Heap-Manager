using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.DataStructTypes;
using TWWHeapVisualizer.Extensions;
using TWWHeapVisualizer.Heap;
using TWWHeapVisualizer.Heap.MemoryBlocks;
using TWWHeapVisualizer.Helpers;
using Timer = System.Windows.Forms.Timer;

namespace TWWHeapVisualizer
{
    public enum DataType
    {
        Int,
        Float,
        String
    }
    public class MyDataItem : IEquatable<MyDataItem>
    {
        public bool IsExpanded { get; set; }
        public int Offset { get; set; }

        public int Length { get; set; }
        public string Type { get; set; }

        public string Name { get; set; }
        public string Value { get; set; }
        public bool Locked { get; set; }
        [Browsable(false)]
        public int Level { get; set; }
        [Browsable(false)]
        public IMemoryAccessor memoryAccessor { get; set; }
        [Browsable(false)]
        public ulong BaseAddress { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            MyDataItem other = (MyDataItem)obj;
            return IsExpanded == other.IsExpanded &&
                    Offset == other.Offset &&
                    Length == other.Length &&
                    Type == other.Type &&
                    Name == other.Name &&
                    Value == other.Value;
        }

        public bool Equals(MyDataItem other)
        {
            if (other == null)
            {
                return false;
            }

            return IsExpanded == other.IsExpanded &&
                    Offset == other.Offset &&
                    Length == other.Length &&
                    Type == other.Type &&
                    Name == other.Name &&
                    Value == other.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + IsExpanded.GetHashCode();
                hash = hash * 23 + (Offset != null ? Offset.GetHashCode() : 0);
                hash = hash * 23 + (Length != null ? Length.GetHashCode() : 0);
                hash = hash * 23 + Type.GetHashCode();
                hash = hash * 23 + (Name != null ? Name.GetHashCode() : 0);
                hash = hash * 23 + (Value != null ? Value.GetHashCode() : 0);
                return hash;
            }
        }
    }
    public class MemoryDataGridView : DataGridView
    {
        private List<MyDataItem> dataItems;
        private List<Color> levelColors;
        public bool isEditing = false;
        private Timer _timer;
        private UInt64 _actorAddress;
        private StructureType _structData;
        private int MaxLevel
        {
            get
            {
                return dataItems.Max(i => i.Level) + 1;
            }
        }
        public MemoryDataGridView(Timer timer, UInt64 actorAddress, StructureType structData)
        {
            InitializeDataGridView();
            this.levelColors = DrawExtensions.GetFurthestColors(10);
            this._timer = timer;
            this._actorAddress = actorAddress;
            this._structData = structData;


            dataItems = new List<MyDataItem>();
            foreach (var p in _structData.Properties)
            {
                dataItems.Add(new MyDataItem {
                    BaseAddress = _actorAddress, 
                    IsExpanded = false, 
                    Name = p.Name, 
                    Type = p.DataType.DataTypeName, 
                    Offset = p.Offset, 
                    Length = p.Length, 
                    Locked = false,
                    Value = "", 
                    memoryAccessor = p.DataType 
                });
            }

            BindingList<MyDataItem> bindingList = new BindingList<MyDataItem>(dataItems);
            // Bind dataItems to the DataGridView
            DataSource = bindingList;
            
            _timer.Tick += this.PopulateDataGridView;

        }
        private void InitializeDataGridView()
        {
            this.dataItems = new List<MyDataItem>();
            this.Dock = DockStyle.Fill;

            DataGridViewTextBoxColumn iconColumn = new DataGridViewTextBoxColumn();
            iconColumn.HeaderText = "";
            iconColumn.DataPropertyName = "IsExpanded";
            iconColumn.Name = "IsExpanded";
            iconColumn.ReadOnly = true;
            iconColumn.Width = 3;
            // Define columns
            DataGridViewTextBoxColumn offsetColumn = new DataGridViewTextBoxColumn();
            offsetColumn.HeaderText = "Offset";
            offsetColumn.DataPropertyName = "Offset";
            offsetColumn.Name = "Offset";
            offsetColumn.ReadOnly = true;
            offsetColumn.Width = 10;
            offsetColumn.MinimumWidth = 30;

            DataGridViewTextBoxColumn lengthColumn = new DataGridViewTextBoxColumn();
            lengthColumn.HeaderText = "Length";
            lengthColumn.DataPropertyName = "Length";
            lengthColumn.Name = "Length";
            lengthColumn.ReadOnly = true;
            lengthColumn.Width = 10;
            lengthColumn.MinimumWidth = 30;

            DataGridViewTextBoxColumn typeColumn = new DataGridViewTextBoxColumn();
            typeColumn.HeaderText = "Data Type";
            typeColumn.DataPropertyName = "Type";
            typeColumn.Name = "Type";
            typeColumn.ReadOnly = true;
            typeColumn.Width = 10;
            typeColumn.MinimumWidth = 50;


            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.HeaderText = "Field Name";
            nameColumn.DataPropertyName = "Name";
            nameColumn.Name = "Name";
            nameColumn.ReadOnly = true;
            nameColumn.Width = 20;
            nameColumn.MinimumWidth = 60;

            DataGridViewCheckBoxColumn lockedColumn = new DataGridViewCheckBoxColumn();
            lockedColumn.HeaderText = "Lock";
            lockedColumn.DataPropertyName = "Locked";
            lockedColumn.Name = "Locked";
            lockedColumn.Width = 10;
            lockedColumn.ReadOnly = false;
            lockedColumn.ValueType = typeof(bool);
            lockedColumn.DefaultCellStyle = new DataGridViewCellStyle()
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            //typeColumn.MinimumWidth = 20;

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.HeaderText = "Value";
            valueColumn.DataPropertyName = "Value";
            valueColumn.Name = "Value";
            valueColumn.ReadOnly = false;
            valueColumn.Width = 30;
            valueColumn.MinimumWidth = 90;




            //// Add columns to DataGridView
            this.Columns.AddRange(new DataGridViewColumn[] { iconColumn, offsetColumn, lengthColumn, typeColumn, nameColumn, lockedColumn, valueColumn });
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            this.AllowUserToOrderColumns = true;
            this.AllowUserToResizeColumns = true;
            this.AllowUserToResizeRows = false;
            this.RowHeadersVisible = false;

            this.DefaultCellStyle.SelectionBackColor = this.DefaultCellStyle.BackColor; //disable different color on cell selection
            this.DefaultCellStyle.SelectionForeColor = this.DefaultCellStyle.ForeColor; //disable different color on cell selection
            this.AllowUserToAddRows = false;    //disables empty bottom row
            this.DoubleBuffered = true;

            this.EditMode = DataGridViewEditMode.EditOnEnter;
            this.SelectionMode = DataGridViewSelectionMode.CellSelect;

            this.CurrentCell = null;
            this.CellBeginEdit += memoryDataGridView_CellBeginEdit;
            this.CellEndEdit += memoryDataGridView_CellEndEdit;
            this.LostFocus += memoryDataGridView_LostFocus;
            this.CellPainting += memoryDataGridView_CellPainting;
            this.Disposed += memoryDataGridView_Disposed;
            this.DataError += MemoryDataGridView_DataError;
            this.Resize += MemoryDataGridView_Resize;
            this.CellContentClick += MemoryDataGridView_CellContentClick;
            this.RowTemplate.Height = 20;
        }

        private void MemoryDataGridView_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex >= 0)
            {
                string columnName = Columns[e.ColumnIndex].Name;
                if(columnName == "Locked")
                {
                    MyDataItem item = Rows[e.RowIndex].DataBoundItem as MyDataItem;
                    DataGridViewCheckBoxCell cell = Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCheckBoxCell;
                    if (item != null)
                    {
                        bool currentValue = (bool)item.GetType().GetProperty(cell.OwningColumn.DataPropertyName).GetValue(item);
                        item.GetType().GetProperty(cell.OwningColumn.DataPropertyName).SetValue(item, !currentValue);
                    }
                }
            }
            //throw new NotImplementedException();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if(this.Parent != null)
            {
                this.ResizeColumns(this.Parent.Width);
            }

        }
        private void RemoveChildItems(int startIndex, int currentLevel)
        {
            BindingList<MyDataItem> dataItemList = (BindingList<MyDataItem>)this.DataSource;
            int siblingIndex = -1;

            for (int i = startIndex + 1; i < dataItems.Count; i++)
            {
                siblingIndex = i;
                if (dataItems[i].Level <= currentLevel)
                {
                    break;
                }             
            }

            int childrenToRemove = siblingIndex - (startIndex + 1);
            for (int i = 0; i < childrenToRemove; i++)
            {
                dataItemList.RemoveAt(startIndex + 1);
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if(e.Button == MouseButtons.Left)
            {
                HandleLeftClick(e);
            }
            else if(e.Button == MouseButtons.Right)
            {
                HandleRightClick(e);
            }


        }
        public void HandleLeftClick(MouseEventArgs e)
        {
            //var item = GetMouseHoveredDataItem(e.Location);
            HitTestInfo hit = this.HitTest(e.X, e.Y);
            if (hit.ColumnIndex > 0 || hit.RowIndex < 0)
            {
                return;
            }
            var hitRow = this.Rows[hit.RowIndex];
            var item = (MyDataItem)hitRow.DataBoundItem;
            if (item != null)
            {
                int startIndex = hit.RowIndex;
                HandleChildDataItems(item, startIndex);
                this.ResizeColumns(this.Parent.Width);
            }
        }
        public void HandleRightClick(MouseEventArgs e)
        {
            HitTestInfo hit = this.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
            {
                return;
            }
            var hitRow = this.Rows[hit.RowIndex];
            var item = (MyDataItem)hitRow.DataBoundItem;
            if (item != null)
            {
                int startIndex = hit.RowIndex;
                // Create the context menu
                ContextMenuStrip contextMenu = new ContextMenuStrip();

                // Add menu items to the context menu
                ToolStripMenuItem copyAddressItem = new ToolStripMenuItem($"Copy Address of {item.Name}");
                //ToolStripMenuItem menuItem2 = new ToolStripMenuItem("Option 2");

                // Attach event handlers to the menu items if needed
                copyAddressItem.Click += CopyAddressItem_Click;
                copyAddressItem.Tag = item;
                //menuItem2.Click += MenuItem2_Click;

                // Add menu items to the context menu
                contextMenu.Items.Add(copyAddressItem);
                //contextMenu.Items.Add(menuItem2);

                // Show the context menu at the mouse position
                contextMenu.Show(this, e.Location);
            }
        }

        // Event handler for menu item 1
        private void CopyAddressItem_Click(object sender, EventArgs e)
        {
            // CopyAddressItem context click
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            var item = (MyDataItem)menuItem.Tag;
            ulong address = item.BaseAddress + (ulong)item.Offset;
            string clipboardValue = address.ToString("X");

            // Copy the value to the clipboard
            Clipboard.SetText(clipboardValue);
        }

        // Event handler for menu item 2
        private void MenuItem2_Click(object sender, EventArgs e)
        {
            // Handle menu item 2 click
        }
        public void HandleChildDataItems(MyDataItem item, int startIndex)
        {
            MyDataItem dataItem = dataItems[startIndex];
            BindingList<MyDataItem> dataItemList = (BindingList<MyDataItem>)this.DataSource;
            if (dataItem.memoryAccessor is StructureType structure)
            {
                dataItem.IsExpanded = !dataItem.IsExpanded;
                if (dataItem.IsExpanded)
                {
                    List<MyDataItem> childItems = new List<MyDataItem>();
                    foreach (var p in structure.Properties)
                    {
                        var childItem = new MyDataItem { BaseAddress = item.BaseAddress, IsExpanded = false, Name = p.Name, Type = p.DataType.DataTypeName, Offset = dataItem.Offset + p.Offset, Length = p.Length, Value = "", memoryAccessor = p.DataType, Level = dataItem.Level + 1 };
                        childItems.Add(childItem);
                    }
                    for (int i = 0; i < childItems.Count; i++)
                    {
                        int index = startIndex + i + 1;
                        dataItemList.Insert(index, childItems[i]);
                    }
                    //dataItems.InsertRange(startIndex + 1, childItems);
                }
                else
                {
                    RemoveChildItems(startIndex, dataItem.Level);
                }
            }
            else if (dataItem.memoryAccessor is ArrayType array)
            {
                dataItem.IsExpanded = !dataItem.IsExpanded;
                if (dataItem.IsExpanded)
                {
                    List<MyDataItem> childItems = new List<MyDataItem>();
                    int elementSize = array.Size / array.NumberOfElements;
                    for (int i = 0; i < array.NumberOfElements; i++)
                    {
                        string name = $"{item.Name}[{i}]";
                        int offset = dataItem.Offset + (i * elementSize);
                        var childItem = new MyDataItem { BaseAddress = item.BaseAddress, IsExpanded = false, Name = name, Type = array.ComponentDataType.DataTypeName, Offset = offset, Length = elementSize, Value = "", memoryAccessor = array.ComponentDataType, Level = dataItem.Level + 1 };
                        childItems.Add(childItem);
                    }
                    for (int i = 0; i < childItems.Count; i++)
                    {
                        int index = startIndex + i + 1;
                        dataItemList.Insert(index, childItems[i]);
                    }
                }
                else
                {
                    RemoveChildItems(startIndex, dataItem.Level);
                }
            }
            else if (dataItem.memoryAccessor is PointerType pointer)
            {
                dataItem.IsExpanded = !dataItem.IsExpanded;
                if (dataItem.IsExpanded)
                {
                    if (ActorData.Instance.DataTypes.ContainsKey(pointer.TargetDataTypeName))
                    {
                        var targetDataType = ActorData.Instance.DataTypes[pointer.TargetDataTypeName];
                        ulong baseAddress = 0;
                        if(ulong.TryParse(dataItem.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out baseAddress))
                        {
                            if (MemoryHelpers.isValidAddress((uint)baseAddress))
                            {
                                var childItem = new MyDataItem { BaseAddress = baseAddress, IsExpanded = false, Name = targetDataType.DataTypeName, Type = targetDataType.DataTypeName, Offset = 0, Length = targetDataType.Size, Value = "", memoryAccessor = targetDataType, Level = dataItem.Level + 1 };
                                dataItemList.Insert(startIndex + 1, childItem);
                                HandleChildDataItems(childItem, startIndex + 1);
                            }
                        }
                    }
                }
                else
                {
                    RemoveChildItems(startIndex, dataItem.Level);
                }
            }
        }
        public void ResizeColumns(int parentWidth)
        {
            int[] defaultWidths = new int[this.Columns.Count];
            this.Columns[0].MinimumWidth = (this.MaxLevel * 10) + 20;
            int column0MaxWidth = this.Columns[0].MinimumWidth + 40;
            //int minWidth = 0;
            for (int i = 0; i < this.Columns.Count; i++)
            {
                defaultWidths[i] = this.Columns[i].Width;
                //minWidth += this.Columns[i].MinimumWidth;
            }
            //this.MinimumSize = new Size(minWidth, 100);
            // Calculate the total default width
            int totalDefaultWidth = defaultWidths.Sum() - 1;

            // Calculate the total available width (client width of the DataGridView)
            int totalAvailableWidth = parentWidth;

            // Adjust the widths proportionately
            for (int i = 0; i < this.Columns.Count; i++)
            {
                // Calculate the adjusted width for each column
                int adjustedWidth = (int)((double)defaultWidths[i] / totalDefaultWidth * totalAvailableWidth);
                if(i == 0)
                {
                    if(adjustedWidth > column0MaxWidth)
                    {
                        this.Columns[i].Width = column0MaxWidth;
                    }
                }
                else
                {
                    if (adjustedWidth < this.Columns[i].MinimumWidth)
                    {
                        this.Columns[i].Width = this.Columns[i].MinimumWidth + 5;
                    }
                    else
                    {
                        // Set the adjusted width for the column
                        this.Columns[i].Width = adjustedWidth;
                    }
                }
            }
        }


        private void MemoryDataGridView_Resize(object? sender, EventArgs e)
        {
            ResizeColumns(this.Parent.Width);
        }

        private void MemoryDataGridView_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            var test = 5;
            Debug.WriteLine(e.Exception);
            //throw new Exception(e.Exception.ToString());
        }

        private void memoryDataGridView_Disposed(object? sender, EventArgs e)
        {
            this._timer.Tick -= this.PopulateDataGridView;
        }
        private Color GetLevelColor(int level)
        {
            return this.levelColors[level % this.levelColors.Count];
        }
        private void memoryDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if(e.RowIndex < 0)
            {
                if(e.ColumnIndex == 0)
                {
                    e.PaintBackground(e.CellBounds, true);
                }
            }
            if(e.RowIndex >= 0 && e.ColumnIndex < this.ColumnCount)
            {
                string columnName = Columns[e.ColumnIndex].Name;
                e.PaintBackground(e.CellBounds, true);
                using (Brush backBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(backBrush, e.CellBounds);
                }
                DataGridViewRow row = Rows[e.RowIndex];
                MyDataItem item = row.DataBoundItem as MyDataItem;
                if (item != null)
                {
                    Color levelColor = GetLevelColor(item.Level);
                    Color backColor = levelColor;
                    if(e.RowIndex % 2 == 1)
                    {
                        if(item.Level == 0)
                        {
                            backColor = DrawExtensions.Lighten(levelColor, -10);
                        }
                        else
                        {
                            backColor = DrawExtensions.Lighten(levelColor, 10);
                        }
                        
                    }
                    using (Brush backBrush = new SolidBrush(backColor))
                    {
                        e.Graphics.FillRectangle(backBrush, e.CellBounds);
                    }


                    if (item.IsExpanded)
                    {
                        //draw border
                        using (Pen pen = new Pen(DrawExtensions.Lighten(levelColor,-50), 2)) // Change color and width as needed
                        {
                            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                        }
                    }


                    int indentation = item.Level * 10;
                    Rectangle rect = e.CellBounds;
                    Rectangle paddingRect = new Rectangle(rect.X + indentation, rect.Y, rect.Width - indentation, rect.Height);
                    if (e.ColumnIndex == 0)
                    {
                        if(item.memoryAccessor is StructureType || item.memoryAccessor is ArrayType || item.memoryAccessor is PointerType)
                        {
                            bool displayArrow = true;
                            if(item.memoryAccessor is PointerType)
                            {
                                ulong baseAddress = 0;
                                if (ulong.TryParse(item.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out baseAddress))
                                {
                                    if (!MemoryHelpers.isValidAddress((uint)baseAddress))
                                    {
                                        displayArrow = false;
                                    }                                  
                                }
                                else
                                {
                                    displayArrow = false;
                                }
                            }
                            if (displayArrow)
                            {
                                Image icon = item.IsExpanded ? Properties.Resources.DownwardIcon : Properties.Resources.ForwardIcon;
                                // Calculate icon position and size
                                int iconSize = Math.Min(paddingRect.Width, paddingRect.Height);
                                int iconX = 10 + indentation;//paddingRect.X + (paddingRect.Width - iconSize) / 2;
                                int iconY = paddingRect.Y + (paddingRect.Height - iconSize) / 2;

                                // Draw the icon
                                e.Graphics.DrawImage(icon, iconX, iconY, iconSize, iconSize);
                            }

                        }
                    }
                    else if(columnName == "Locked")
                    {
                        if(item.memoryAccessor is EnumType || item.memoryAccessor is UnknownType || item.memoryAccessor is TypeDefType)
                        {
                            e.PaintContent(e.CellBounds);
                        }

                        //e.Handled = true;
                    }
                    else
                    {
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Near;
                            sf.LineAlignment = StringAlignment.Center;

                            // Draw the text with the specified font and color
                            using (SolidBrush textBrush = new SolidBrush(e.CellStyle.ForeColor))
                            {

                                if(columnName == "Value" || columnName == "Locked")
                                {
                                    e.Graphics.DrawString(e.Value?.ToString(), e.CellStyle.Font, textBrush, rect, sf);  //dont add padding for these columns
                                }
                                else
                                {
                                    e.Graphics.DrawString(e.Value?.ToString(), e.CellStyle.Font, textBrush, paddingRect, sf);
                                }                               
                            }
                        }
                    }


                }
                e.Handled = true;
            }

        }
        private void memoryDataGridView_LostFocus(object sender, EventArgs e)
        {
            // Check if the DataGridView is currently in edit mode
            if (this.IsCurrentCellInEditMode)
            {
                // Commit the edit if the DataGridView is in edit mode
                this.EndEdit();
                TriggerCellEndEdit();
            }
        }
        private void TriggerCellEndEdit()
        {
            // Check if there is a current cell
            if (this.CurrentCell != null)
            {
                // Create a DataGridViewCellCancelEventArgs object with appropriate parameters
                DataGridViewCellEventArgs args = new DataGridViewCellEventArgs(this.CurrentCell.ColumnIndex, this.CurrentCell.RowIndex);

                // Call the event handler directly
                memoryDataGridView_CellEndEdit(this, args);
                this.ResizeColumns(this.Parent.Width);
            }
        }
        //update value cells with the appropriate editor when clicked into
        public void memoryDataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Pause the refresh timer only if the edit is not initiated programmatically
            if (!isEditing)
            {
                string columnName = Columns[e.ColumnIndex].Name;
                isEditing = true;
                DataGridView dataGridView = sender as DataGridView;
                MyDataItem item = (MyDataItem)dataGridView.Rows[e.RowIndex].DataBoundItem;
                if(item.memoryAccessor is StructureType)
                {
                    if(e.ColumnIndex == 3)
                    {
                        //TODO: Fix typeahead logic
                        e.Cancel = true;
                        return;
                        //var typeaheadList = ActorData.Instance.DataTypes.Values.Where(dt => dt is StructureType).Select(dt => dt.DataTypeName).ToList();
                        //TypeaheadDataGridViewCell typeaheadCell = new TypeaheadDataGridViewCell();
                        //typeaheadCell.TypeaheadList = typeaheadList;
                        //dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex] = typeaheadCell;
                    }
                    else
                    {
                        e.Cancel = true;
                        return;
                    }

                }
                if(item.memoryAccessor is EnumType enumItem)
                {
                    if(columnName == "Value")
                    {
                        DataGridViewComboBoxCell comboBoxCell = new DataGridViewComboBoxCell();
                        comboBoxCell.Items.Add("");
                        foreach(string key in enumItem.EnumValues.Keys)
                        {
                            comboBoxCell.Items.Add($"{key} - {enumItem.EnumValues[key]}");
                        }
                        comboBoxCell.Value = item.Value;
                        dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex] = comboBoxCell;
                    }
                }
            }
        }

        // Event handler for the DataGridView's CellEndEdit event
        public void memoryDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Resume the refresh timer only if the editing is not initiated programmatically
            DataGridView dataGridView = sender as DataGridView;
            DataGridViewRow row = dataGridView.Rows[e.RowIndex];
            MyDataItem item = (MyDataItem)row.DataBoundItem;
            var cell = row.Cells[e.ColumnIndex];
            string value = cell.Value?.ToString() ?? "";
            ulong writeAddress = item.BaseAddress + (ulong)item.Offset;
            item.memoryAccessor.Write(writeAddress, value, item.Length);
            this.EndEdit();
            isEditing = false;
        }



        public void PopulateDataGridView(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in this.Rows)
            {
                // Get the corresponding data item
                MyDataItem item = (MyDataItem)row.DataBoundItem;

                try
                {
                    ulong address = item.BaseAddress + (ulong)item.Offset;
                    if (item.Locked)
                    {
                        item.memoryAccessor.Write(address, item.Value, item.Length);
                    }
                    //skip reading from memory if row is not visible
                    //need to see if this actually improves performance
                    if (IsRowOffScreen(row))
                        continue;
                    string value = item.memoryAccessor.Read(address, item.Length);
                    item.Value = value;
                }
                catch (Exception ex)
                {
                    // Handle the exception
                    Console.WriteLine($"Error updating row: {ex.Message}");
                }
            }

            // Refresh the DataGridView
            this.Refresh();
        }
        bool IsRowOffScreen(DataGridViewRow row)
        {
            int rowIndex = row.Index;
            int firstVisibleRowIndex = FirstDisplayedScrollingRowIndex;
            int visibleRowCount = DisplayedRowCount(true);

            // Check if the row index is less than the first visible row index
            // or if it's greater than the last visible row index
            // 5 is used to allow a smaller buffer of extra rows
            return rowIndex < firstVisibleRowIndex || rowIndex - 5 >= (firstVisibleRowIndex + visibleRowCount);
        }
    }
}