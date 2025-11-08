using OpenCvSharp;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

// Alias để tránh trùng tên Point giữa System.Drawing và OpenCvSharp
using CvPoint = OpenCvSharp.Point;
using DSize = System.Drawing.Size;

namespace LeoThap.Auto
{
    public static class ImageHelper
    {
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hWnd, out POINT lpPoint);
        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        const uint PW_CLIENTONLY = 0x00000001;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;

        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X; public int Y; }

        public static Bitmap CaptureWindow(IntPtr hwnd)
        {
            if (!GetClientRect(hwnd, out var cr))
                throw new Exception("GetClientRect failed");

            int w = cr.Right - cr.Left, h = cr.Bottom - cr.Top;
            var bmp = new Bitmap(w, h);

            using var g = Graphics.FromImage(bmp);
            var hdc = g.GetHdc();
            bool ok = PrintWindow(hwnd, hdc, PW_CLIENTONLY);
            g.ReleaseHdc(hdc);

            if (!ok)
            {
                if (!ClientToScreen(hwnd, out var tl))
                    throw new Exception("ClientToScreen failed");
                g.CopyFromScreen(tl.X, tl.Y, 0, 0, new DSize(w, h));
            }
            return bmp;
        }

        static Mat ToMat(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
        }

        public static (CvPoint? p, double score) MatchOnce(Bitmap hayBmp, Bitmap tplBmp, double threshold)
        {
            using var hay = ToMat(hayBmp);
            using var tpl = ToMat(tplBmp);
            using var hayGray = new Mat();
            using var tplGray = new Mat();

            Cv2.CvtColor(hay, hayGray, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(tpl, tplGray, ColorConversionCodes.BGR2GRAY);

            using var result = new Mat(hayGray.Rows - tplGray.Rows + 1, hayGray.Cols - tplGray.Cols + 1, MatType.CV_32FC1);
            Cv2.MatchTemplate(hayGray, tplGray, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out CvPoint maxLoc);

            if (maxVal >= threshold)
            {
                var center = new CvPoint(maxLoc.X + tplGray.Cols / 2, maxLoc.Y + tplGray.Rows / 2);
                return (center, maxVal);
            }
            return (null, maxVal);
        }

        public static bool ClickImage(IntPtr hwnd, string imgPath, double threshold, Action<string>? log = null)
        {
            using var frame = CaptureWindow(hwnd);
            using var tpl = (Bitmap)Image.FromFile(imgPath);
            var (pt, score) = MatchOnce(frame, tpl, threshold);

            if (!pt.HasValue)
            {
                log?.Invoke($"🙈 Không thấy {Path.GetFileName(imgPath)} (score={score:F2})");
                return false;
            }

            ClickClient(hwnd, pt.Value.X, pt.Value.Y);
            log?.Invoke($"🖱 Click {Path.GetFileName(imgPath)} tại ({pt.Value.X},{pt.Value.Y}) score={score:F2}");
            return true;
        }

        public static void ClickClient(IntPtr hwnd, int x, int y)
        {
            int lParam = (y << 16) | (x & 0xFFFF);
            PostMessage(hwnd, WM_LBUTTONDOWN, 1, lParam);
            Thread.Sleep(25);
            PostMessage(hwnd, WM_LBUTTONUP, 0, lParam);
        }

        public static bool IsPopupVisible(IntPtr hwnd, string popupImg, double threshold = 0.8)
        {
            using var frame = CaptureWindow(hwnd);
            using var tpl = (Bitmap)Image.FromFile(popupImg);
            var (pt, score) = MatchOnce(frame, tpl, threshold);
            return pt.HasValue && score >= threshold;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const uint INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static void ClickClient(IntPtr hwnd, int x, int y, Action<string>? log = null)
        {
            int lParam = (y << 16) | (x & 0xFFFF);

            PostMessage(hwnd, WM_LBUTTONDOWN, 1, lParam);
            Thread.Sleep(25);
            PostMessage(hwnd, WM_LBUTTONUP, 0, lParam);

            log?.Invoke($"✅ FakeClickClient @client=({x},{y})");
        }


    }
}
