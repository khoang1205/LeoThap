using System.Drawing;

namespace LeoThap.Auto
{
    public static class PlayerDetector
    {
        public static bool IsPlayerVisible(IntPtr hwnd, string playerImg, double threshold = 0.85)
        {
            using var frame = ImageHelper.CaptureWindow(hwnd);
            using var tpl = (Bitmap)Image.FromFile(playerImg);
            var (pt, score) = ImageHelper.MatchOnce(frame, tpl, threshold);
            return pt.HasValue && score >= threshold;
        }
        public static string DetectPlayerAvatar(IntPtr hwnd, string assetsDir, double threshold, Action<string> log)
        {
            using var frame = ImageHelper.CaptureWindow(hwnd);

            string bestFile = "";
            double bestScore = 0;

            // Quét tất cả file player_*.png trong thư mục Assets
            foreach (var f in Directory.GetFiles(assetsDir, "player_*.png", SearchOption.TopDirectoryOnly))
            {
                using var tpl = (Bitmap)Image.FromFile(f);
                var (pt, score) = ImageHelper.MatchOnce(frame, tpl, threshold);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestFile = f;
                }
            }

            if (!string.IsNullOrEmpty(bestFile) && bestScore >= threshold)
            {
                log($"🧍 Phát hiện nhân vật: {Path.GetFileNameWithoutExtension(bestFile)} (score={bestScore:F2})");
                return bestFile;
            }

            log($"⚠️ Không phát hiện được nhân vật (best={bestScore:F2})");
            return string.Empty;
        }
    
}
}
