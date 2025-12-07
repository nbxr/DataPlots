// FastBitmap.cs
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DataPlots.Wpf.Extensions;

internal static class WriteableBitmapExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ColorToInt(Color c)
    {
        // Shift the bytes into their proper positions:
        //   A = bits 31-24, R = bits 23-16, G = bits 15-8, B = bits 7-0
        return c.A << 24 | c.R << 16 | c.G << 8 | c.B;
    }

    public static void Clear(this WriteableBitmap bmp, Color color)
    {
        int intColor = ColorToInt(color);
        bmp.Lock();
        unsafe
        {
            int* p = (int*)bmp.BackBuffer;
            int count = bmp.BackBufferStride / 4 * bmp.PixelHeight;
            for (int i = 0; i < count; i++) p[i] = intColor;
        }
        bmp.AddDirtyRect(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
        bmp.Unlock();
    }

    public static void DrawLine(this WriteableBitmap bmp, double x0, double y0, double x1, double y1, Color color, double thickness = 1.0)
    {
        int intColor = ColorToInt(color);

        // If thickness > 1.5, fall back to WPF's built-in (still fast enough for lines)
        if (thickness > 1.5)
        {
            bmp.Lock();
            var pen = new Pen(new SolidColorBrush(color), thickness)
            {
                LineJoin = PenLineJoin.Round,
                EndLineCap = PenLineCap.Round,
                StartLineCap = PenLineCap.Round
            };

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawLine(pen, new Point(x0, y0), new Point(x1, y1));
            }

            var rtb = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            // Only copy the dirty area around the line
            int margin = (int)Math.Ceiling(thickness);
            int left = (int)Math.Min(x0, x1) - margin;
            int top = (int)Math.Min(y0, y1) - margin;
            int width = (int)Math.Abs(x1 - x0) + 2 * margin;
            int height = (int)Math.Abs(y1 - y0) + 2 * margin;

            var rect = new Int32Rect(
                Math.Max(0, left),
                Math.Max(0, top),
                Math.Min(width, bmp.PixelWidth - Math.Max(0, left)),
                Math.Min(height, bmp.PixelHeight - Math.Max(0, top)));

            if (rect.Width > 0 && rect.Height > 0)
            {
                rtb.CopyPixels(rect, bmp.BackBuffer + (rect.Y * bmp.BackBufferStride + rect.X * 4),
                    rect.Height * bmp.BackBufferStride, bmp.BackBufferStride);
                bmp.AddDirtyRect(rect);
            }
            bmp.Unlock();
            return;
        }

        // Thin lines — ultra-fast Bresenham

        bmp.Lock();
        int w = bmp.PixelWidth;
        int h = bmp.PixelHeight;
        unsafe
        {
            int* buffer = (int*)bmp.BackBuffer;
            int stride = bmp.BackBufferStride / 4;

            int ix0 = (int)Math.Round(x0);
            int iy0 = (int)Math.Round(y0);
            int ix1 = (int)Math.Round(x1);
            int iy1 = (int)Math.Round(y1);

            int dx = Math.Abs(ix1 - ix0);
            int dy = Math.Abs(iy1 - iy0);
            int sx = ix0 < ix1 ? 1 : -1;
            int sy = iy0 < iy1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (ix0 >= 0 && ix0 < w && iy0 >= 0 && iy0 < h)
                    buffer[iy0 * stride + ix0] = intColor;

                if (ix0 == ix1 && iy0 == iy1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; ix0 += sx; }
                if (e2 < dx) { err += dx; iy0 += sy; }
            }
        }
        bmp.AddDirtyRect(new Int32Rect(0, 0, w, h));
        bmp.Unlock();
    }

    public static void FillEllipse(this WriteableBitmap bmp, double cx, double cy, double r, Color color)
    {
        FillEllipse(bmp, cx, cy, r, r, color);
    }

    public static void FillEllipse(this WriteableBitmap bmp, double cx, double cy, double rx, double ry, Color color)
    {
        int intColor = ColorToInt(color);
        int x0 = (int)Math.Round(cx - rx);
        int x1 = (int)Math.Round(cx + rx);
        int y0 = (int)Math.Round(cy - ry);
        int y1 = (int)Math.Round(cy + ry);

        bmp.Lock();
        unsafe
        {
            int* p = (int*)bmp.BackBuffer;
            int stride = bmp.BackBufferStride / 4;
            for (int y = Math.Max(y0, 0); y < Math.Min(y1, bmp.PixelHeight); y++)
                for (int x = Math.Max(x0, 0); x < Math.Min(x1, bmp.PixelWidth); x++)
                {
                    double dx = (x - cx) / rx;
                    double dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1.0)
                        p[y * stride + x] = intColor;
                }
        }
        bmp.AddDirtyRect(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
        bmp.Unlock();
    }

    public static void FillRectangle(this WriteableBitmap bmp, double x, double y, double x2, double y2, Color color)
    {
        int x0 = (int)Math.Max(x, 0);
        int y0 = (int)Math.Max(y, 0);
        int x1 = (int)Math.Min(x2, bmp.PixelWidth);
        int y1 = (int)Math.Min(y2, bmp.PixelHeight);
        if (x0 >= x1 || y0 >= y1) return;

        int intColor = ColorToInt(color);
        int width = x1 - x0;
        int height = y1 - y0;

        bmp.Lock();
        unsafe
        {
            int* p = (int*)bmp.BackBuffer;
            int stride = bmp.BackBufferStride / 4;
            for (int row = y0; row < y1; row++)
            {
                int offset = row * stride + x0;
                for (int col = 0; col < width; col++)
                    p[offset + col] = intColor;
            }
        }
        bmp.AddDirtyRect(new Int32Rect(x0, y0, width, height));
        bmp.Unlock();
    }
}