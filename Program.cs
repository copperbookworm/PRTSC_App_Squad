using System.Windows.Forms;

namespace SquadShot;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        bool captureNow = args.Any(argument => argument is "--capture-now" or "-c" or "--capture");
        bool openSettings = args.Any(argument => argument is "--settings" or "-s");

        if (captureNow)
        {
            return RunCaptureNow();
        }

        try
        {
            using var mutex = new Mutex(true, @"Local\SquadShot.SingleInstance", out bool isFirstInstance);
            if (!isFirstInstance)
            {
                Logger.Info("已有实例在运行，本次启动忽略");
                if (openSettings)
                {
                    MessageBox.Show(
                        "SquadShot 已在运行，请从系统托盘图标打开设置。",
                        "SquadShot",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                return 0;
            }

            AppConfig config = AppConfig.Load();
            Application.Run(new TrayApp(config, openSettings));
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error("程序启动失败", ex);
            MessageBox.Show(
                "SquadShot 启动失败：" + ex.Message,
                "SquadShot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static int RunCaptureNow()
    {
        try
        {
            AppConfig config = AppConfig.Load();
            using CaptureResult result = CaptureService.Capture(config);
            CliReport.Write(result);
            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            Logger.Error("命令行截图失败", ex);
            try
            {
                CliReport.Write(new CaptureResult { Success = false, Error = ex.Message });
            }
            catch
            {
                // ignore secondary failures
            }
            return 1;
        }
    }
}
