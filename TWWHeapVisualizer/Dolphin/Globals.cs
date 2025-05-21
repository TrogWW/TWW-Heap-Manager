using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.FormElements;
using TWWHeapVisualizer.Heap.MemoryBlocks;
using Timer = System.Windows.Forms.Timer;

namespace TWWHeapVisualizer.Dolphin
{
    public static class Globals
    {
        private static Timer _timer = null;
        [Class("dComIfG_inf_c")]
        public static uint g_dComIfG_gameInfo = 0x803b8108; //0x803c4c08;
        [Class("dStage_roomStatus_c[64]")]
        public static uint dStage_roomControl_c__mStatus = 0x803b1188; //803bdc88
        public static void PopulateGlobalsMenu(ToolStripMenuItem parent, Timer timer)
        {
            _timer = timer; 
            var t = typeof(Globals);

            // OPTION A: static fields
            foreach (var fi in t.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = fi.GetCustomAttribute<ClassAttribute>();
                if (attr == null) continue;

                var item = new ToolStripMenuItem(fi.Name)
                {
                    // stash both the FieldInfo _and_ the attribute instance in Tag
                    Tag = Tuple.Create<MemberInfo, ClassAttribute>(fi, attr)
                };
                item.Click += OnGlobalItemClick;
                parent.DropDownItems.Add(item);
            }

            // OPTION B: if you really were using static properties,
            // you could do the same over GetProperties(...)
            //
            // foreach (var pi in t.GetProperties(...)) { ... }
        }

        private static void OnGlobalItemClick(object? sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            var (member, attr) = (Tuple<MemberInfo, ClassAttribute>)menuItem.Tag;

            uint address;
            if (member is FieldInfo fi)
                address = (uint)fi.GetValue(null);
            else if (member is PropertyInfo pi)
                address = (uint)pi.GetValue(null);
            else
                throw new InvalidOperationException();

            MemoryDataForm memoryDataGridViewForm = new MemoryDataForm(_timer, address,attr.name);
            // Show the form as a dialog
            memoryDataGridViewForm.Show();
        }
    }
}
