using System.Text;
using TWWHeapVisualizer.DataStructTypes;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Heap;
using TWWHeapVisualizer.Heap.MemoryBlocks;
using Timer = System.Windows.Forms.Timer;
namespace TWWHeapVisualizer.FormElements
{
    internal class MemoryDataForm : Form
    {
        private MemoryDataGridView memoryDataGridView;
        private UsedMemoryBlock _block;
        private Timer _timer;

        public MemoryDataForm(Timer timer, UsedMemoryBlock block)
        {
            this._timer = timer;
            this._block = block;
            InitializeMemoryDataGridView();
        }
        private void InitializeMemoryDataGridView()
        {

            this.Size = new Size(1200, 400);
            var actorAddress = _block.startAddress + 0x10;
            fopAc_ac_c actor = new fopAc_ac_c(actorAddress);
            string ghidraStructName = "";
            if (ActorData.Instance.ProcStructNames.ContainsKey(actor.procName))
            {
                ghidraStructName = ActorData.Instance.ProcStructNames[actor.procName].StructName;
            }
            else
            {
                ghidraStructName = "fopAc_ac_c"; //generic actor type
            }
            if (!ActorData.Instance.DataTypes.ContainsKey(ghidraStructName))
            {
                ghidraStructName = "fopAc_ac_c";
            }

            this.Text = $"Memory Data for {_block.startAddress.ToString("X")} {_block.data?.ToString() ?? ""} | Struct: {ghidraStructName}";

            IDataType datatype = ActorData.Instance.DataTypes[ghidraStructName];
            if(datatype is StructureType structData)
            {
                memoryDataGridView = new MemoryDataGridView(_timer, actorAddress, structData);
            }
            else
            {
                throw new Exception($"Error with parsed ghidra structs. The proc name {ghidraStructName} was found to not be a structure type.");
            }

            // Add memoryDataGridView to the form's controls
            Controls.Add(memoryDataGridView);
        }

        // Override the OnFormClosing method to handle form closing event
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Perform cleanup for memoryDataGridView if needed
            if (memoryDataGridView != null)
            {
                _timer.Tick -= memoryDataGridView.PopulateDataGridView;
            }
        }
    }
}
