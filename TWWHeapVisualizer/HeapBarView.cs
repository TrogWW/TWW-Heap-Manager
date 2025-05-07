using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TWWHeapVisualizer.Heap.MemoryBlocks;
using Timer = System.Windows.Forms.Timer;
using System.Globalization;
using TWWHeapVisualizer.Extensions;

namespace TWWHeapVisualizer
{
    public class HeapBarForm : Form
    {
        private readonly Timer refreshTimer;
        private readonly SKControl skControl;
        private readonly HScrollBar hScrollBar;
        private MemoryBlockCollection _heap;
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

        public HeapBarForm(MemoryBlockCollection heap, string title)
        {
            _heap = heap;
            Text = title;
            Width = 1000;
            Height = 380; // extra height for arrow, selection, and summary

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
            if (hoveredBlock is FreeMemoryBlock free)
            {
                isSelecting = true;
                selectStartX = selectEndX = e.X;
                selectedFreeBlock = free;
                skControl.Invalidate();
            }
        }

        private void OnMouseMove_Select(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                selectEndX = e.X;
                skControl.Invalidate();
            }
            else
            {
                mouseX = e.X;
            }
        }

        private void OnMouseUp_Select(object sender, MouseEventArgs e)
        {
            if (!isSelecting || selectedFreeBlock == null)
                return;

            isSelecting = false;
            selectEndX = e.X;
            skControl.Invalidate();

            // Compute selection addresses
            float viewW = skControl.Width;
            float scaledW = viewW * zoom;
            float pxb = scaledW / heapSize;

            float sx = Math.Min(selectStartX, selectEndX) + panX;
            float ex = Math.Max(selectStartX, selectEndX) + panX;
            uint addrStart = (uint)(heapStart + (sx / pxb));
            uint addrEnd = (uint)(heapStart + (ex / pxb));

            addrStart = Math.Max(addrStart, selectedFreeBlock.startAddress);
            addrEnd = Math.Min(addrEnd, selectedFreeBlock.endAddress);

            // Show Refine Allocation dialog
            using var dlg = new Form
            {
                Text = "Refine Allocation",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                Width = 380,
                Height = 260,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var lblStart = new Label { Text = "Start Address (hex):", Left = 10, Top = 10, Width = 150 };
            var tbStart = new TextBox { Text = addrStart.ToString("X"), Left = 160, Top = 10, Width = 200 };
            var lblEnd = new Label { Text = "End Address (hex):", Left = 10, Top = 40, Width = 150 };
            var tbEnd = new TextBox { Text = addrEnd.ToString("X"), Left = 160, Top = 40, Width = 200 };
            var lblFrag = new Label { Text = "Fragment Size (bytes):", Left = 10, Top = 70, Width = 150 };
            var tbFrag = new TextBox { Text = "0", Left = 160, Top = 70, Width = 200 };
            var lblSize = new Label { Text = "Region Size (KB):", Left = 10, Top = 100, Width = 150 };
            var tbSize = new TextBox { Left = 160, Top = 100, Width = 200, ReadOnly = true };
            var btnOk = new Button { Text = "OK", Left = 160, Top = 140, Width = 80, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Left = 260, Top = 140, Width = 80, DialogResult = DialogResult.Cancel };

            dlg.Controls.AddRange(new Control[] { lblStart, tbStart, lblEnd, tbEnd, lblFrag, tbFrag, lblSize, tbSize, btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            void UpdateSize()
            {
                if (uint.TryParse(tbStart.Text, NumberStyles.HexNumber, null, out uint s) &&
                    uint.TryParse(tbEnd.Text, NumberStyles.HexNumber, null, out uint e) && e >= s)
                {
                    tbSize.Text = ((e - s) / 1024f).ToString("F2");
                }
                else tbSize.Text = string.Empty;
            }
            UpdateSize();
            tbStart.TextChanged += (s, ev) => UpdateSize();
            tbEnd.TextChanged += (s, ev) => UpdateSize();

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (uint.TryParse(tbStart.Text, NumberStyles.HexNumber, null, out uint rs) &&
                    uint.TryParse(tbEnd.Text, NumberStyles.HexNumber, null, out uint re) && rs < re)
                {
                    int fragSize = 0;
                    if (int.TryParse(tbFrag.Text, out int f) && f > 0)
                        fragSize = f;

                    if (fragSize <= 0)
                    {
                        // Single block allocation
                        HeapHacker.FakeAllocate(_heap.baseAddress, selectedFreeBlock, rs, re);
                        _heap.filledMemoryBlocks.Add(rs);
                    }
                    else
                    {
                        FreeMemoryBlock curBlock = selectedFreeBlock;
                        // Fragmented allocation: max block size = fragSize
                        for (uint addr = rs; addr < re; addr += (uint)fragSize)
                        {
                            uint endAlloc = Math.Min(addr + 32, re);
                            HeapHacker.FakeAllocate(_heap.baseAddress, curBlock, addr, endAlloc);
                            
                            _heap.filledMemoryBlocks.Add(addr);
                            curBlock = new FreeMemoryBlock
                            {
                                size = re - endAlloc,
                                startAddress = endAlloc,
                                endAddress = re
                            };
                        }
                    }
                }
            }

            selectedFreeBlock = null;
            skControl.Invalidate();
        }

        private void SkControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (_heap.blocks.Count == 0)
                return;

            heapStart = _heap.blocks.Min(b => b.startAddress);
            heapEnd = _heap.blocks.Max(b => b.endAddress);
            heapSize = heapEnd - heapStart;

            var canvas = e.Surface.Canvas;
            var info = e.Info;
            float viewW = info.Width;
            float viewH = info.Height;
            float viewBarHeight = viewH - 60; // expanded to make room for summary
            float scaledW = viewW * zoom;
            float pxb = scaledW / heapSize;

            UpdateScrollBar(viewW, scaledW);
            canvas.Clear(SKColors.Black);

            using var usedPaint = new SKPaint { Color = SKColors.Red, IsAntialias = false };
            using var freePaint = new SKPaint { Color = SKColors.Green, IsAntialias = false };
            using var smallPaint = new SKPaint { Color = SKColors.Orange, IsAntialias = false };

            hoveredBlock = null;
            // Draw blocks
            foreach (var block in _heap.blocks)
            {
                float rel = block.startAddress - heapStart;
                float x = rel * pxb - panX;
                float w = block.size * pxb;
                if (x + w < 0 || x > viewW) continue;

                bool isUsed = block is UsedMemoryBlock;
                bool isCorrupted = isUsed && ((UsedMemoryBlock)block).filled;

                SKPaint paint;
                if (w < 1f)
                {
                    paint = smallPaint;
                    w = 1f;
                }
                else if (isUsed)
                {
                    paint = isCorrupted
                        ? new SKPaint { Color = new SKColor(100, 0, 160), IsAntialias = false }
                        : usedPaint;
                }
                else paint = freePaint;

                bool hover = mouseX >= x && mouseX <= x + w;
                if (hover)
                {
                    var c = paint.Color;
                    var hc = c.WithRed((byte)Math.Min(255, c.Red + 80))
                              .WithGreen((byte)Math.Min(255, c.Green + 80))
                              .WithBlue((byte)Math.Min(255, c.Blue + 80));
                    using var hp = new SKPaint { Color = hc, IsAntialias = false };
                    canvas.DrawRect(x, 0, w, viewBarHeight, hp);
                    hoveredBlock = block;
                }
                else
                {
                    canvas.DrawRect(x, 0, w, viewBarHeight, paint);
                }
            }

            // Draw marquee selection with KB overlay
            if (isSelecting && selectedFreeBlock != null)
            {
                float sx = Math.Min(selectStartX, selectEndX);
                float sw = Math.Abs(selectEndX - selectStartX);
                using var selPaint = new SKPaint { Color = new SKColor(0, 120, 255, 100), IsAntialias = false };
                canvas.DrawRect(sx, 0, sw, viewBarHeight, selPaint);

                float worldStartX = sx + panX;
                float worldEndX = sx + sw + panX;
                uint addr0 = (uint)(heapStart + (worldStartX / pxb));
                uint addr1 = (uint)(heapStart + (worldEndX / pxb));
                float sizeKB = (addr1 - addr0) / 1024f;
                string sizeLabel = $"{sizeKB:F2} KB";

                using var font = new SKFont(SKTypeface.Default, 14);
                using var tp = new SKPaint { Color = SKColors.White, IsAntialias = true };
                using var bg = new SKPaint { Color = SKColors.Black.WithAlpha(180), IsAntialias = true };
                font.MeasureText(sizeLabel, out SKRect bounds);
                float pad = 4;
                float bwx = bounds.Width + pad * 2;
                float bhx = bounds.Height + pad * 2;
                float mid = sx + sw / 2;
                float bx2 = mid - bwx / 2;
                float by2 = viewBarHeight / 2 - bhx / 2;
                canvas.DrawRect(bx2, by2, bwx, bhx, bg);
                canvas.DrawText(sizeLabel, bx2 + pad, by2 + pad - bounds.Top, SKTextAlign.Left, font, tp);
            }

            // Arrow to last UsedMemoryBlock by index
            var lastUsed = _heap.usedBlocks
                .OrderByDescending(b => b.index)
                .FirstOrDefault();
            if (lastUsed != null)
            {
                float rel = lastUsed.startAddress - heapStart;
                float x = rel * pxb - panX;
                float w = lastUsed.size * pxb;
                if (x + w >= 0 && x <= viewW)
                {
                    float cx = x + w / 2;
                    float arrowY = viewBarHeight + 4;
                    using var ap = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
                    using var path = new SKPath();
                    path.MoveTo(cx, arrowY);
                    path.LineTo(cx - 6, arrowY + 10);
                    path.LineTo(cx + 6, arrowY + 10);
                    path.Close();
                    canvas.DrawPath(path, ap);
                }
            }

            // Draw axis grid
            using var axisPaint = new SKPaint
            {
                Color = SKColors.White,
                StrokeWidth = 1,
                IsAntialias = false,
                TextSize = 12,
                Typeface = SKTypeface.Default
            };
            float labelStepPx = 100;
            for (float px = 0; px <= viewW; px += labelStepPx)
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
                    bool isCorrupted = usr.filled;
                    lines.Add("Used" + (isCorrupted ? " (Filled)" : ""));
                    if (!string.IsNullOrEmpty(usr.data?.ToString()))
                        lines.Add("DYN: " + usr.data.ToString());
                    lines.Add($"Index: {usr.index}");
                }
                else
                {
                    lines.Add("Free");
                }
                lines.Add($"Start: 0x{hoveredBlock.startAddress:X8}");
                lines.Add($"Size : {hoveredBlock.size}");
                using var font = new SKFont(SKTypeface.Default, 14);
                using var tp = new SKPaint { Color = SKColors.White, IsAntialias = true };
                using var bg = new SKPaint { Color = SKColors.Black.WithAlpha(180), IsAntialias = true };
                float padding = 6;
                float lineHeight = 16;
                float maxW = 0;
                var boundsList = new List<SKRect>();
                foreach (var ln in lines)
                {
                    font.MeasureText(ln, out SKRect r);
                    boundsList.Add(r);
                    maxW = Math.Max(maxW, r.Width);
                }
                float bw = maxW + padding * 2;
                float bh = lineHeight * lines.Count + padding * 2;
                float bx = mouseX + 15;
                if (bx + bw > e.Info.Width) bx = mouseX - bw - 15;
                float by = 30;
                canvas.DrawRect(bx, by, bw, bh, bg);
                for (int i = 0; i < lines.Count; i++)
                {
                    float ty = by + padding + i * lineHeight - boundsList[i].Top;
                    canvas.DrawText(lines[i], bx + padding, ty, SKTextAlign.Left, font, tp);
                }
            }

            // Draw summary data at bottom-left
            using var infoPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 12,
                IsAntialias = true
            };
            float summaryX = 5;
            float summaryY = viewBarHeight + 20;

            long freeSize = _heap.freeBlocks.Sum(b => b.size);
            long usedSize = _heap.usedBlocks.Sum(b => b.size);
            long maxFree = _heap.freeBlocks.Max(b => b.size);
            long totalSize = freeSize + usedSize;

            canvas.DrawText($"Max:  {(float)maxFree / 1000f} KB", summaryX, summaryY, infoPaint);
            canvas.DrawText($"Free:  {(float)freeSize / 1000f} KB", summaryX, summaryY + 15, infoPaint);
            canvas.DrawText($"Total: {(float)totalSize / 1000f} KB", summaryX, summaryY + 30, infoPaint);
        }

        private void SkControl_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZ = zoom;
            zoom *= e.Delta > 0 ? 1.1f : 0.9f;
            zoom = Math.Max(1f, Math.Min(zoom, 50000f));
            float vw = skControl.Width;
            float oldW = vw * oldZ;
            float newW = vw * zoom;
            float mr = (e.X + panX) / oldW;
            panX = Math.Clamp(mr * newW - e.X, 0, newW - vw);
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
                int max = (int)Math.Ceiling(contentWidth - viewWidth);
                hScrollBar.Minimum = 0;
                hScrollBar.LargeChange = (int)viewWidth;
                hScrollBar.SmallChange = (int)(viewWidth * 0.1f);
                hScrollBar.Maximum = max + hScrollBar.LargeChange - 1;
                hScrollBar.Value = Math.Max(hScrollBar.Minimum, Math.Min((int)Math.Round(panX), max));
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