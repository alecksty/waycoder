using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VMLAssembler
{
    /// <summary>
    /// VML 链接器
    /// </summary>
    public class Linker
    {
        /// <summary>
        /// 链接多个 VML 程序文件为一个可执行程序
        /// </summary>
        /// <param name="inputFiles">输入 VML 文件路径列表</param>
        /// <param name="outputFile">输出 VML 文件路径</param>
        public void Link(List<string> inputFiles, string outputFile)
        {
            // 直接合并源文件内容，而不是解析后再合并
            StringBuilder linkedSource = new StringBuilder();
            linkedSource.Append(".data\n\n");
            linkedSource.Append(".text\n");

            HashSet<string> seenLabels = new HashSet<string>();

            foreach (string inputFile in inputFiles)
            {
                string source = File.ReadAllText(inputFile);
                // 提取.text段的内容
                int textStart = source.IndexOf(".text");
                if (textStart != -1)
                {
                    string textContent = source.Substring(textStart + 5);
                    // 移除.data段的内容
                    int dataEnd = textContent.IndexOf(".data");
                    if (dataEnd != -1)
                    {
                        textContent = textContent.Substring(0, dataEnd);
                    }

                    // 处理标签冲突，只保留第一个出现的标签
                    StringBuilder filteredContent = new StringBuilder();
                    string[] lines = textContent.Split('\n');
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("LABEL "))
                        {
                            string labelName = trimmedLine.Substring(6).Trim();
                            if (!seenLabels.Contains(labelName))
                            {
                                seenLabels.Add(labelName);
                                filteredContent.AppendLine(line);
                            }
                        }
                        else
                        {
                            filteredContent.AppendLine(line);
                        }
                    }

                    linkedSource.Append(filteredContent.ToString());
                }
            }

            // 保存链接后的程序
            File.WriteAllText(outputFile, linkedSource.ToString());
        }
    }
}
