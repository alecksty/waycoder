using System;
using System.IO;
using VMLAssembler;

namespace CompilerBase
{
    /// <summary>
    /// 增量编译缓存 — 基于源文件时间戳的 .vmlobj 缓存机制。
    /// 避免重复编译未修改的源文件。
    /// </summary>
    public static class IncrementalCache
    {
        /// <summary>缓存文件扩展名</summary>
        public const string CacheExtension = ".vmlobj";

        /// <summary>
        /// 尝试从缓存加载 VML 程序。如果缓存不存在或过期，返回 null。
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <returns>缓存的 VML 程序，或 null</returns>
        public static VmlProgram? TryLoad(string sourcePath)
        {
            string cachePath = GetCachePath(sourcePath);
            if (!File.Exists(cachePath)) return null;

            DateTime sourceTime = File.GetLastWriteTimeUtc(sourcePath);
            DateTime cacheTime = File.GetLastWriteTimeUtc(cachePath);

            if (cacheTime < sourceTime)
            {
                // 缓存过期 (可以添加依赖文件检查)
                return null;
            }

            try
            {
                string vmlText = File.ReadAllText(cachePath);
                var program = VmlProgram.Load(vmlText);
                if (program.Instructions.Count > 0)
                    return program;
            }
            catch { /* 缓存文件损坏 */ }

            return null;
        }

        /// <summary>
        /// 将 VML 程序保存到缓存。
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="program">编译后的 VML 程序</param>
        public static void Save(string sourcePath, VmlProgram program)
        {
            try
            {
                string cachePath = GetCachePath(sourcePath);
                var dir = Path.GetDirectoryName(cachePath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string vmlText = program.ToString();
                File.WriteAllText(cachePath, vmlText);

                // 对齐时间戳避免不必要的重编译
                File.SetLastWriteTimeUtc(cachePath, File.GetLastWriteTimeUtc(sourcePath));
            }
            catch { /* 缓存写入失败不影响编译 */ }
        }

        /// <summary>清除指定源文件的缓存</summary>
        public static void Invalidate(string sourcePath)
        {
            string cachePath = GetCachePath(sourcePath);
            if (File.Exists(cachePath))
            {
                try { File.Delete(cachePath); } catch { }
            }
        }

        private static string GetCachePath(string sourcePath)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";
            string name = Path.GetFileNameWithoutExtension(sourcePath);
            string cacheDir = Path.Combine(dir, ".vmlcache");
            return Path.Combine(cacheDir, name + CacheExtension);
        }
    }
}
