using System;
using System.IO;

namespace VMLPlugins
{
    /// <summary>
    /// 统一版本号 — 从项目根目录 VERSION 文件读取。
    /// 所有组件版本号统一由此文件管理。
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>版本号字符串 (如 "1.65.17")</summary>
        public static readonly string Version;

        /// <summary>带前缀的版本显示 (如 "v1.65.17")</summary>
        public static readonly string Display;

        /// <summary>完整产品名 (如 "VML Toolchain v1.65.17")</summary>
        public static string Product(string name) => $"{name} {Display}";

        static VersionInfo()
        {
            var v = "1.65.36"; // fallback
            try
            {
                // Search from executable directory up to 4 levels
                var dir = AppContext.BaseDirectory;
                for (int i = 0; i < 4; i++)
                {
                    var probe = Path.Combine(dir, "VERSION");
                    if (File.Exists(probe))
                    {
                        v = File.ReadAllText(probe).Trim();
                        break;
                    }
                    var parent = Directory.GetParent(dir);
                    if (parent == null) break;
                    dir = parent.FullName;
                }
            }
            catch (System.IO.IOException) { /* VERSION file not found or unreadable — use fallback */ }
            catch (System.UnauthorizedAccessException) { /* no read permission — use fallback */ }
            Version = v;
            Display = $"v{v}";
        }
    }
}
