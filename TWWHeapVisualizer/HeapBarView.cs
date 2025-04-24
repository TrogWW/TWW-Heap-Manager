using SkiaSharp;
using SkiaSharp.Views.Desktop;
using TWWHeapVisualizer.Heap.MemoryBlocks;
using Timer = System.Windows.Forms.Timer;

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

        public HeapBarForm(HeapListView view)
        {
            this.view = view;

            Text = "Heap Bar Visualizer";
            Width = 1000;
            Height = 320; // extra height for arrow

            skControl = new SKControl
            {
                Dock = DockStyle.Fill
            };
            skControl.PaintSurface += SkControl_PaintSurface;
            skControl.MouseWheel += SkControl_MouseWheel;
            skControl.MouseMove += (s, e) => mouseX = e.X;
            Controls.Add(skControl);

            hScrollBar = new HScrollBar
            {
                Dock = DockStyle.Bottom,
                Minimum = 0,
                SmallChange = 10,
                LargeChange = 100,
                Visible = false
            };
            hScrollBar.Scroll += (s, e) =>
            {
                panX = hScrollBar.Value;
                skControl.Invalidate();
            };
            Controls.Add(hScrollBar);

            refreshTimer = new Timer { Interval = 100 };
            refreshTimer.Tick += (s, e) => skControl.Invalidate();
            refreshTimer.Start();
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
                else
                {
                    basePaint = isUsed
                        ? (isCorrupted ? new SKPaint { Color = new SKColor(255, 102, 0), IsAntialias = false } : usedPaint)
                        : freePaint;
                }

                bool isHovered = mouseX >= x && mouseX <= x + w;

                if (isHovered)
                {
                    SKColor baseColor = basePaint.Color;
                    SKColor highlightColor = baseColor
                        .WithRed((byte)Math.Min(255, baseColor.Red + 80))
                        .WithGreen((byte)Math.Min(255, baseColor.Green + 80))
                        .WithBlue((byte)Math.Min(255, baseColor.Blue + 80))
                        .WithAlpha(255);

                    using var highlightPaint = new SKPaint { Color = highlightColor, IsAntialias = false };
                    canvas.DrawRect(x, 0, w, viewBarHeight, highlightPaint);
                    hoveredBlock = block;
                }
                else
                {
                    canvas.DrawRect(x, 0, w, viewBarHeight, basePaint);
                }
            }

            var lastUsedBlock = view.memoryBlocks
             .OfType<UsedMemoryBlock>()
             .OrderByDescending(b => b.index)
             .FirstOrDefault();
            if (lastUsedBlock != null)
            {
                float relStart = lastUsedBlock.startAddress - heapStart;
                float x = relStart * pxb - panX;
                float w = lastUsedBlock.size * pxb;

                if (x + w >= 0 && x <= viewW)
                {
                    float centerX = x + w / 2f;
                    float arrowTop = viewBarHeight + 4;

                    using var arrowPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
                    SKPoint[] arrowPoints =
                    {
                        new SKPoint(centerX, arrowTop),
                        new SKPoint(centerX - 6, arrowTop + 10),
                        new SKPoint(centerX + 6, arrowTop + 10)
                    };

                    using var path = new SKPath();
                    path.MoveTo(arrowPoints[0]);
                    path.LineTo(arrowPoints[1]);
                    path.LineTo(arrowPoints[2]);
                    path.Close();

                    canvas.DrawPath(path, arrowPaint);

                    using var labelPaint = new SKPaint { Color = SKColors.White, TextSize = 12, IsAntialias = true };
                    canvas.DrawText("Last Used", centerX - 24, arrowTop + 24, SKTextAlign.Left, new SKFont(), labelPaint);
                }
            }

            using var axisPaint = new SKPaint
            {
                Color = SKColors.White,
                StrokeWidth = 1,
                IsAntialias = false,
                TextSize = 12,
                Typeface = SKTypeface.Default
            };

            float labelStepPx = 100;
            float contentWidth = skControl.Width * zoom;
            float pixelsPerByte = contentWidth / heapSize;

            float axisBaseY = viewBarHeight;
            float labelOffsetY = 14;

            for (float px = 0; px <= skControl.Width; px += labelStepPx)
            {
                float heapByteOffset = (px + panX) / pixelsPerByte;
                uint addr = heapStart + (uint)heapByteOffset;
                string label = $"0x{addr:X8}";

                canvas.DrawLine(px, axisBaseY - 5, px, axisBaseY, axisPaint); // small tick mark
                canvas.DrawText(label, px + 2, axisBaseY - labelOffsetY, SKTextAlign.Left, new SKFont(), axisPaint);
            }

            if (hoveredBlock != null)
            {
                var lines = new List<string>
                {
                    hoveredBlock is UsedMemoryBlock used ? "Used" : "Free",
                    hoveredBlock is UsedMemoryBlock u ? (u.data?.ToString() ?? "") : null,
                    hoveredBlock is UsedMemoryBlock u2 ? $"Index    {u2.index}" : null,
                    $"Start     0x{hoveredBlock.startAddress:X8}",
                    $"Size      {hoveredBlock.size}"
                };

                lines.RemoveAll(string.IsNullOrWhiteSpace);

                using var font = new SKFont(SKTypeface.Default, 14);
                using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
                using var backgroundPaint = new SKPaint { Color = SKColors.Black.WithAlpha(180), IsAntialias = true };

                float padding = 6;
                float maxWidth = 0;
                float lineHeight = 16;
                var bounds = new List<SKRect>();

                foreach (var line in lines)
                {
                    font.MeasureText(line, out SKRect rect);
                    bounds.Add(rect);
                    maxWidth = Math.Max(maxWidth, rect.Width);
                    lineHeight = Math.Max(lineHeight, rect.Height);
                }

                float boxWidth = maxWidth + padding * 2;
                float boxHeight = (lineHeight * lines.Count) + padding * 2;
                float boxX = mouseX + 15;
                if (boxX + boxWidth > e.Info.Width)
                    boxX = mouseX - boxWidth - 15;
                float boxY = 30;

                canvas.DrawRect(boxX, boxY, boxWidth, boxHeight, backgroundPaint);

                for (int i = 0; i < lines.Count; i++)
                {
                    float textY = boxY + padding + (i * lineHeight) - bounds[i].Top;
                    canvas.DrawText(lines[i], boxX + padding, textY, SKTextAlign.Left, font, textPaint);
                }
            }
        }

        private void SkControl_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = zoom;
            zoom *= e.Delta > 0 ? 1.1f : 0.9f;
            zoom = Math.Max(1.0f, Math.Min(zoom, 50000f));

            float viewW = skControl.Width;
            float oldScaledW = viewW * oldZoom;
            float newScaledW = viewW * zoom;

            float mouseRatio = (e.X + panX) / oldScaledW;
            float newPanX = mouseRatio * newScaledW - e.X;
            float maxPanX = newScaledW - viewW;

            panX = Math.Max(0, Math.Min(newPanX, maxPanX));
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

                int maxScroll = (int)Math.Ceiling(contentWidth - viewWidth);

                hScrollBar.Minimum = 0;
                hScrollBar.LargeChange = (int)viewWidth;
                hScrollBar.SmallChange = (int)(viewWidth * 0.1f);
                hScrollBar.Maximum = maxScroll + hScrollBar.LargeChange - 1;
                hScrollBar.Value = Math.Max(hScrollBar.Minimum, Math.Min((int)Math.Round(panX), maxScroll));
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (skControl != null)
                skControl.Invalidate();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
        }
    }
}
