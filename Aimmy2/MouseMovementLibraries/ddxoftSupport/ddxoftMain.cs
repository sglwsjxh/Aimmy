using Other;
using System.IO;
using System.Net.Http;
using System.Security.Principal;
using System.Windows;

namespace MouseMovementLibraries.ddxoftSupport
{
    internal class DdxoftMain
    {
        public static ddxoftMouse ddxoftInstance = new();
        private static readonly string ddxoftpath = "ddxoft.dll";
        private static readonly string ddxoftUri = "https://gitlab.com/marsqq/extra-files/-/raw/main/ddxoft.dll";

        private static async Task DownloadDdxoft()
        {
            try
            {
                LogManager.Log(LogManager.LogLevel.Info, $"{ddxoftpath} 缺失，正在尝试下载。", true);

                using HttpClient httpClient = new();

                var response = await httpClient.GetAsync(new Uri(ddxoftUri));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(ddxoftpath, content);
                    LogManager.Log(LogManager.LogLevel.Info, $"{ddxoftpath} 下载成功，请重新选择 ddxoft 虚拟输入驱动以加载 DLL。", true);
                }
            }
            catch
            {
                LogManager.Log(LogManager.LogLevel.Error, $"{ddxoftpath} 下载失败，请尝试其他鼠标移动方式。", true);
            }
        }

        public static async Task<bool> DLLLoading()
        {
            try
            {
                if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) == false)
                {
                    MessageBox.Show("ddxoft 虚拟输入驱动需要以管理员权限运行 Aimmy，请关闭 Aimmy 后以管理员身份重新启动再使用此鼠标移动方式。", "Aimmy");
                    return false;
                }

                if (!File.Exists(ddxoftpath))
                {
                    await DownloadDdxoft();
                    return false;
                }

                if (ddxoftInstance.Load(ddxoftpath) != 1 || ddxoftInstance.btn!(0) != 1)
                {
                    MessageBox.Show("ddxoft 虚拟输入驱动与你的电脑不兼容，请尝试其他鼠标移动方式。", "Aimmy");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载 ddxoft 虚拟输入驱动失败。\n\n" + ex.ToString(), "Aimmy");
                return false;
            }
        }

        public static async Task<bool> Load() => await DLLLoading();
    }
}