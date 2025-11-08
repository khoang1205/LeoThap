using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace LeoThap.Auto
{
    public static class AutoBossController
    {
        static int autoHiddenCount = 0;
        static bool firstAuto = true;

        // hard click auto
        static readonly int AUTO_X = 872;
        static readonly int AUTO_Y = 509;
        public static void RunBossFlow(
     IntPtr hwnd,
     string playerImg,
     string suGiaImg,
     string bossImg,
     string bangDichChuyenImg,
     string lenTangImg,
     Action<string> log,
     CancellationToken tk)
        {
            string folder = Path.GetDirectoryName(bossImg)!;

            string khieuChienImg = Path.Combine(folder, "KhieuChien.png");
            string bossPopupImg = Path.Combine(folder, "BossPopup.png");

            var bossList = LoadBossTemplates(folder, log);

            log("⚔️ START — Leo Tháp Trấn Thủ!");

            while (!tk.IsCancellationRequested)
            {
                log("🔄 Bắt đầu 1 vòng Leo Tháp...");

                // ✅ STEP 1 — kiểm tra popup Boss trước
                bool hasBossPopup =
                    ImageHelper.IsPopupVisible(hwnd, bossPopupImg, 0.65) ||
                    ImageHelper.IsPopupVisible(hwnd, khieuChienImg, 0.65);

                if (hasBossPopup)
                {
                    log("✅ Popup Boss đã mở → bỏ qua scan Boss");
                }
                else
                {
                    // ✅ chưa có popup → click Boss
                    if (ClickBossMulti(hwnd, bossList, log))
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        log("⚠️ Không thấy Boss — retry...");
                        Thread.Sleep(1500);
                        continue;
                    }
                }

                // ✅ Dù popup đang mở sẵn hay mới mở → click Khiêu Chiến
                if (File.Exists(khieuChienImg) &&
                    ImageHelper.ClickImage(hwnd, khieuChienImg, 0.70, log))
                {
                    log("🔥 Click Khiêu Chiến!");
                    Thread.Sleep(1500);
                }

                // ✅ STEP 2 — Combat
                HandleCombat(hwnd, playerImg, log, tk);

                // ✅ STEP 3 — lên tầng
                log("🧙 Xử lý lên tầng...");

                var suGiaList = LoadSuGiaTemplates(folder, log);

                bool clickedSuGia = false;
                foreach (var img in suGiaList)
                {
                    if (ImageHelper.ClickImage(hwnd, img, 0.70, log))
                    {
                        clickedSuGia = true;
                        break;
                    }
                }

                if (clickedSuGia)
                {
                    log("✅ Click Sứ Giả!");
                    Thread.Sleep(2000);

                    if (ImageHelper.IsPopupVisible(hwnd, bangDichChuyenImg, 0.70))
                    {
                        log("✅ Popup dịch chuyển → Click Lên Tầng!");
                        ImageHelper.ClickImage(hwnd, lenTangImg, 0.70, log);
                        Thread.Sleep(4500);
                    }
                    else
                    {
                        log("⚠️ Không thấy popup Dịch Chuyển Ma Tháp");
                    }
                }
                else
                {
                    log("⚠️ Không thấy Sứ Giả → bỏ qua lên tầng");
                    Thread.Sleep(1000);
                }
            }

            log("🛑 STOP — Hủy Leo Tháp");
        }


        // ==================================================
        // COMBAT HANDLER
        // ==================================================
        static void HandleCombat(
      IntPtr hwnd,
      string playerImg,
      Action<string> log,
      CancellationToken tk)
        {
            // Player ẩn → tăng count
            if (!PlayerDetector.IsPlayerVisible(hwnd, playerImg))
            {
                autoHiddenCount++;
                log($"⚠️ Player ẩn → count = {autoHiddenCount}");
            }

            // Nếu chưa đạt 100 → chờ player xuất hiện rồi thôi
            if (autoHiddenCount < 100)
            {
                WaitUntilPlayerAppears(hwnd, playerImg, log, tk);
                return;
            }

            // ✅ ĐÃ ĐỦ 100 lần ẩn → giờ CHỜ player HIỆN LẠI
            log("⚠️ Player ẩn >= 100 → chờ hiện lại để double click Auto");

            WaitUntilPlayerAppears(hwnd, playerImg, log, tk);

            if (PlayerDetector.IsPlayerVisible(hwnd, playerImg))
            {
                log("✅ Player xuất hiện lại → DOUBLE CLICK AUTO!");
                ImageHelper.ClickClient(hwnd, AUTO_X, AUTO_Y, log);
                Thread.Sleep(150);
                ImageHelper.ClickClient(hwnd, AUTO_X, AUTO_Y, log);

                autoHiddenCount = 0;
            }
        }


        static void WaitUntilPlayerAppears(
            IntPtr hwnd,
            string playerImg,
            Action<string> log,
            CancellationToken tk)
        {
            for (int i = 0; i < 35 && !tk.IsCancellationRequested; i++)
            {
                if (PlayerDetector.IsPlayerVisible(hwnd, playerImg))
                {
                    log("✅ Nhân vật đã xuất hiện lại!");
                    return;
                }
                Thread.Sleep(2000);
            }

            log("⚠️ Quá lâu không thấy nhân vật → có thể lag/hết trận");
        }

        // ==================================================
        static List<string> LoadSuGiaTemplates(string folder, Action<string> log)
        {
            var list = new List<string>();

            void Add(string f)
            {
                var p = Path.Combine(folder, f);
                if (File.Exists(p)) list.Add(p);
            }

            Add("SuGia.png");
            Add("SuGia1.png");
            Add("SuGia2.png");
            Add("SuGia3.png");   // dư cũng không sao nếu không có file
            Add("SuGia4.png");

            log($"📁 SuGiaTemplate: {list.Count}");
            return list;
        }
        static List<string> LoadBossTemplates(string folder, Action<string> log)
        {
            var list = new List<string>();
            void Add(string f)
            {
                var p = Path.Combine(folder, f);
                if (File.Exists(p)) list.Add(p);
            }
            Add("Boss.png");
            Add("Boss1.png");
            Add("Boss2.png");
            Add("Boss3.png");
            Add("Boss4.png");
            log($"📁 BossTemplate: {list.Count}");
            return list;
        }

        static bool ClickBossMulti(IntPtr hwnd, List<string> imgs, Action<string> log)
        {
            foreach (var img in imgs)
            {
                if (ImageHelper.ClickImage(hwnd, img, 0.55, log))
                {
                    log($"✅ Click Boss → {Path.GetFileName(img)}");
                    return true;
                }
            }
            return false;
        }
    }
}
