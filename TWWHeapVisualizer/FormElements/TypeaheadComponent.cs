using System.ComponentModel;
public class TypeaheadDataGridViewCell : DataGridViewTextBoxCell
{
    public List<string> TypeaheadList { get; set; }

    public override Type EditType => typeof(TypeaheadEditingControl);

    public TypeaheadDataGridViewCell()
    {
    }

    public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
    {

        var ctl = DataGridView.EditingControl as TypeaheadEditingControl;


        if (ctl != null && ctl is TypeaheadEditingControl typeaheadControl)
        {
            typeaheadControl.list = this.TypeaheadList;
        }
        if (Value != null)
            ctl.Text = Value.ToString();
        base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

    }

    public override object ParseFormattedValue(object formattedValue, DataGridViewCellStyle cellStyle, TypeConverter formattedValueTypeConverter, TypeConverter valueTypeConverter)
    {
        return formattedValue; // Do not perform any parsing
    }
}

public class TypeaheadEditingControl : TextBox, IDataGridViewEditingControl
{
    public ListBox TypeaheadList;
    public List<string> list { get; set; } // Add this property to hold the list of items
    public DataGridView EditingControlDataGridView { get; set; }
    public object EditingControlFormattedValue { get; set; }
    public int EditingControlRowIndex { get; set; }
    public bool EditingControlValueChanged { get; set; }

    public Cursor EditingPanelCursor => base.Cursor;

    public bool RepositionEditingControlOnValueChange => false;

    public TypeaheadEditingControl()
    {
        // Initialize and configure the TypeaheadList as needed
        TypeaheadList = new ListBox();
        TypeaheadList.Visible = false;
        TypeaheadList.SelectedIndexChanged += TypeaheadList_SelectedIndexChanged;
    }
    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);

        // Add the TypeaheadList as a control to the parent form
        if (Parent != null)
        {
            Parent.Parent.Controls.Add(TypeaheadList);
        }
    }
    private void TypeaheadList_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Set the selected item from the typeahead list as the text of the editing control
        if (TypeaheadList.SelectedIndex >= 0)
        {
            this.Text = TypeaheadList.SelectedItem.ToString();
            this.Focus(); // Return focus to the textbox after selection
            TypeaheadList.Visible = false;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape || keyData == Keys.Enter)
        {
            // Hide the typeahead list when Escape key is pressed
            TypeaheadList.Visible = false;
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if(this.Parent == null)
        {
            return;
        }
        // Filter the typeahead list based on the current text value
        string searchText = this.Text.ToLower();

        // Update the displayed list based on the search text
        // You can use LINQ to filter your list of strings
        var filteredItems = list.Where(item => item.ToLower().Contains(searchText)).ToList();

        // Populate the typeaheadList with filteredItems
        // Update the typeahead dropdown with filteredItems
        TypeaheadList.Items.Clear();
        foreach (string item in filteredItems)
        {
            TypeaheadList.Items.Add(item);
        }

        // Show or hide the typeahead list based on whether there are filtered items
        TypeaheadList.Visible = filteredItems.Count > 0;
        TypeaheadList.Location = new Point(Parent.Left, Parent.Bottom);
        TypeaheadList.Width = Parent.Width;
        //TypeaheadList.BringToFront();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        // Hide the typeahead list and end editing when the control loses focus
        //TypeaheadList.Visible = false;
    }

    public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
    {
        this.Font = dataGridViewCellStyle.Font;
        this.BackColor = dataGridViewCellStyle.BackColor;
        this.ForeColor = dataGridViewCellStyle.ForeColor;
    }

    public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
    {
        // Let the DataGridView handle the keys listed.
        switch (keyData & Keys.KeyCode)
        {
            case Keys.Left:
            case Keys.Up:
            case Keys.Down:
            case Keys.Right:
            case Keys.Home:
            case Keys.End:
            case Keys.PageDown:
            case Keys.PageUp:
                return true;
            default:
                return !dataGridViewWantsInputKey;
        }
    }

    public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
    {
        return this.Text;
    }

    public void PrepareEditingControlForEdit(bool selectAll)
    {
        // No preparation needs to be done.
    }
}
