using SkiaSharp;
using SkiaSharp.Views.Desktop;
using TWWHeapVisualizer.Extensions;
using TWWHeapVisualizer.Heap.MemoryBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.Globalization;

namespace TWWHeapVisualizer
{
    public class HeapBarForm : Form
    {
        private readonly Timer refreshTimer;
        private readonly SKControl skControl;
        private readonly HScrollBar hScrollBar;
        private readonly HeapListView view;
        private float zoom = 1.0f;
        private float panX = 0f;

        private uint heapStart;
        private uint heapEnd;
        private float heapSize;

        private IMemoryBlock hoveredBlock = null;
        private int mouseX = -1;

        // Selection state
        private bool isSelecting = false;
        private float selectStartX, selectEndX;
        private FreeMemoryBlock selectedFreeBlock = null;

        public HeapBarForm(HeapListView view)
        {
            this.view = view;

            Text = "Heap Bar Visualizer";
            Width = 1000;
            Height = 360; // extra height for arrow and selection

            skControl = new SKControl { Dock = DockStyle.Fill };
            skControl.PaintSurface += SkControl_PaintSurface;
            skControl.MouseWheel += SkControl_MouseWheel;
            skControl.MouseMove += (s, e) => mouseX = e.X;
            skControl.MouseDown += OnMouseDown_Select;
            skControl.MouseMove += OnMouseMove_Select;
            skControl.MouseUp += OnMouseUp_Select;
            Controls.Add(skControl);

            hScrollBar = new HScrollBar
            {
                Dock = DockStyle.Bottom,
                Minimum = 0,
                SmallChange = 10,
                LargeChange = 100,
                Visible = false
            };
            hScrollBar.Scroll += (s, e) => { panX = hScrollBar.Value; skControl.Invalidate(); };
            Controls.Add(hScrollBar);

            refreshTimer = new Timer { Interval = 100 };
            refreshTimer.Tick += (s, e) => skControl.Invalidate();
            refreshTimer.Start();
        }

        private void OnMouseDown_Select(object sender, MouseEventArgs e)
        {
            // Start selection only within a free block
            if (hoveredBlock is FreeMemoryBlock free)
            {
                isSelecting = true;
                selectStartX = e.X;
                selectEndX = e.X;
                selectedFreeBlock = free;
            }
        }

        private void OnMouseMove_Select(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                selectEndX = e.X;
                skControl.Invalidate();
            }
        }

        private void OnMouseUp_Select(object sender, MouseEventArgs e)
        {
            if (!isSelecting || selectedFreeBlock == null)
                return;
            isSelecting = false;
            selectEndX = e.X;
            skControl.Invalidate();

            // Compute raw addresses from pixel selection
            float viewW = skControl.Width;
            float scaledW = viewW * zoom;
            float pxb = scaledW / heapSize;

            float sx = Math.Min(selectStartX, selectEndX) + panX;
            float ex = Math.Max(selectStartX, selectEndX) + panX;
            uint addrStart = (uint)(heapStart + (sx / pxb));
            uint addrEnd = (uint)(heapStart + (ex / pxb));

            // Clamp to the free block range
            addrStart = Math.Max(addrStart, selectedFreeBlock.startAddress);
            addrEnd = Math.Min(addrEnd, selectedFreeBlock.endAddress);

            // Show Refine Allocation dialog
            using var dlg = new Form
            {
                Text = "Refine Allocation",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                Width = 380,
                Height = 220,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // --- Controls ---
            var lblStart = new Label { Text = "Start Address (hex):", Left = 10, Top = 10, Width = 150 };
            var tbStart = new TextBox { Text = addrStart.ToString("X"), Left = 160, Top = 10, Width = 200 };

            var lblEnd = new Label { Text = "End Address (hex):", Left = 10, Top = 40, Width = 150 };
            var tbEnd = new TextBox { Text = addrEnd.ToString("X"), Left = 160, Top = 40, Width = 200 };

            var lblSize = new Label { Text = "Region Size (KB):", Left = 10, Top = 70, Width = 150 };
            var tbSize = new TextBox { Left = 160, Top = 70, Width = 200, ReadOnly = true };

            var btnOk = new Button { Text = "OK", Left = 160, Top = 110, Width = 80, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Left = 260, Top = 110, Width = 80, DialogResult = DialogResult.Cancel };

            dlg.Controls.AddRange(new Control[]
            {
        lblStart, tbStart,
        lblEnd,   tbEnd,
        lblSize,  tbSize,
        btnOk,    btnCancel
            });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            // Helper to recalc size in KB
            void UpdateSize()
            {
                if (uint.TryParse(tbStart.Text, NumberStyles.HexNumber, null, out uint s) &&
                    uint.TryParse(tbEnd.Text, NumberStyles.HexNumber, null, out uint e) && e >= s)
                {
                    float sizeBytes = e - s;
                    float sizeKB = sizeBytes / 1024f;
                    tbSize.Text = sizeKB.ToString("F2");
                }
                else
                {
                    tbSize.Text = string.Empty;
                }
            }

            // Initial and dynamic updates
            UpdateSize();
            tbStart.TextChanged += (s, ev) => UpdateSize();
            tbEnd.TextChanged += (s, ev) => UpdateSize();

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (uint.TryParse(tbStart.Text, NumberStyles.HexNumber, null, out uint refinedStart) &&
                    uint.TryParse(tbEnd.Text, NumberStyles.HexNumber, null, out uint refinedEnd) &&
                    refinedStart < refinedEnd)
                {
                    // Apply the allocation
                    HeapHacker.FakeAllocate(selectedFreeBlock, refinedStart, refinedEnd);
                }
            }

            selectedFreeBlock = null;
            skControl.Invalidate();
        }

        private void SkControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (view.memoryBlocks.Count == 0)
                return;

            heapStart = view.memoryBlocks.Min(b => b.startAddress);
            heapEnd = view.memoryBlocks.Max(b => b.endAddress);
            heapSize = heapEnd - heapStart;

            var canvas = e.Surface.Canvas;
            var info = e.Info;
            float viewW = info.Width;
            float viewH = info.Height;
            float viewBarHeight = viewH - 40;
            float scaledW = viewW * zoom;
            float pxb = scaledW / heapSize;

            UpdateScrollBar(viewW, scaledW);
            canvas.Clear(SKColors.Black);

            using var usedPaint = new SKPaint { Color = SKColors.Red, IsAntialias = false };
            using var freePaint = new SKPaint { Color = SKColors.Green, IsAntialias = false };
            using var smallPaint = new SKPaint { Color = SKColors.Orange, IsAntialias = false };

            hoveredBlock = null;

            // Draw blocks
            foreach (var block in view.memoryBlocks)
            {
                float relStart = block.startAddress - heapStart;
                float x = relStart * pxb - panX;
                float w = block.size * pxb;
                if (x + w < 0 || x > viewW) continue;

                bool isUsed = block is UsedMemoryBlock;
                bool isCorrupted = isUsed && view.filledMemoryBlocks.Contains(block.startAddress);

                SKPaint basePaint;
                if (w < 1.0f)
                {
                    basePaint = smallPaint;
                    w = 1.0f;
                }
                else basePaint = isUsed
                    ? (isCorrupted ? new SKPaint { Color = new SKColor(255, 102, 0), IsAntialias = false } : usedPaint)
                    : freePaint;

                bool isHovered = mouseX >= x && mouseX <= x + w;
                if (isHovered)
                {
                    SKColor bc = basePaint.Color;
                    SKColor hc = bc
                        .WithRed((byte)Math.Min(255, bc.Red + 80))
                        .WithGreen((byte)Math.Min(255, bc.Green + 80))
                        .WithBlue((byte)Math.Min(255, bc.Blue + 80));
                    using var highlightPaint = new SKPaint { Color = hc, IsAntialias = false };
                    canvas.DrawRect(x, 0, w, viewBarHeight, highlightPaint);
                    hoveredBlock = block;
                }
                else canvas.DrawRect(x, 0, w, viewBarHeight, basePaint);
            }

            // Draw marquee selection
            if (isSelecting && selectedFreeBlock != null)
            {
                float sx = Math.Min(selectStartX, selectEndX);
                float sw = Math.Abs(selectEndX - selectStartX);
                using var selPaint = new SKPaint { Color = new SKColor(0, 120, 215, 100), IsAntialias = false };
                canvas.DrawRect(sx, 0, sw, viewBarHeight, selPaint);
            }

            // Draw arrow to last UsedMemoryBlock by index
            var lastUsed = view.memoryBlocks.OfType<UsedMemoryBlock>()
                .OrderByDescending(b => b.index).FirstOrDefault();
            if (lastUsed != null)
            {
                float x = ((lastUsed.startAddress - heapStart) * pxb) - panX;
                float w = lastUsed.size * pxb;
                if (x + w >= 0 && x <= viewW)
                {
                    float cx = x + w / 2;
                    float arrowY = viewBarHeight + 4;
                    using var ap = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
                    var path = new SKPath();
                    path.MoveTo(cx, arrowY);
                    path.LineTo(cx - 6, arrowY + 10);
                    path.LineTo(cx + 6, arrowY + 10);
                    path.Close();
                    canvas.DrawPath(path, ap);
                }
            }

            // Draw axis grid
            using var axisPaint = new SKPaint { Color = SKColors.White, StrokeWidth = 1, IsAntialias = false, TextSize = 12, Typeface = SKTypeface.Default };
            float labelStepPx = 100;
            for (float px = 0; px <= skControl.Width; px += labelStepPx)
            {
                float addrOff = (px + panX) / pxb;
                uint addr = heapStart + (uint)addrOff;
                canvas.DrawLine(px, viewBarHeight - 5, px, viewBarHeight, axisPaint);
                canvas.DrawText($"0x{addr:X8}", px + 2, viewBarHeight - 14, SKTextAlign.Left, new SKFont(), axisPaint);
            }

            // Draw hover tooltip
            if (hoveredBlock != null)
            {
                var lines = new List<string>();
                if (hoveredBlock is UsedMemoryBlock usr)
                {
                    lines.Add("Used");
                    if (!string.IsNullOrEmpty(usr.data?.ToString())) lines.Add(usr.data.ToString());
                    lines.Add($"Index: {usr.index}");
                }
                else lines.Add("Free");
                lines.Add($"Start: 0x{hoveredBlock.startAddress:X8}");
                lines.Add($"Size : {hoveredBlock.size}");

                using var font = new SKFont(SKTypeface.Default, 14);
                using var tp = new SKPaint { Color = SKColors.White, IsAntialias = true };
                using var bg = new SKPaint { Color = SKColors.Black.WithAlpha(180), IsAntialias = true };
                float padding = 6, lh = 16;
                float maxW = 0;
                var boundsList = new List<SKRect>();
                foreach (var ln in lines)
                {
                    font.MeasureText(ln, out SKRect r);
                    boundsList.Add(r);
                    maxW = Math.Max(maxW, r.Width);
                }
                float bw = maxW + padding * 2, bh = lh * lines.Count + padding * 2;
                float bx = mouseX + 15;
                if (bx + bw > e.Info.Width) bx = mouseX - bw - 15;
                float by = 30;
                canvas.DrawRect(bx, by, bw, bh, bg);
                for (int i = 0; i < lines.Count; i++)
                {
                    float ty = by + padding + i * lh - boundsList[i].Top;
                    canvas.DrawText(lines[i], bx + padding, ty, SKTextAlign.Left, font, tp);
                }
            }
        }

        private void SkControl_MouseWheel(object sender, MouseEventArgs e)
        {
            float old = zoom;
            zoom *= e.Delta > 0 ? 1.1f : 0.9f;
            zoom = Math.Max(1f, Math.Min(zoom, 50000f));
            float vw = skControl.Width;
            float oldSW = vw * old;
            float newSW = vw * zoom;
            float mr = (e.X + panX) / oldSW;
            panX = Math.Max(0, Math.Min(mr * newSW - e.X, newSW - vw));
            skControl.Invalidate();
        }

        private void UpdateScrollBar(float viewWidth, float contentWidth)
        {
            if (contentWidth <= viewWidth)
            {
                hScrollBar.Visible = false;
                hScrollBar.Maximum = 0;
            }
            else
            {
                hScrollBar.Visible = true;
                int maxS = (int)Math.Ceiling(contentWidth - viewWidth);
                hScrollBar.Minimum = 0;
                hScrollBar.LargeChange = (int)viewWidth;
                hScrollBar.SmallChange = (int)(viewWidth * 0.1f);
                hScrollBar.Maximum = maxS + hScrollBar.LargeChange - 1;
                hScrollBar.Value = Math.Max(hScrollBar.Minimum, Math.Min((int)Math.Round(panX), maxS));
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            skControl?.Invalidate();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
        }
    }
}
