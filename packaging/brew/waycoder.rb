# Homebrew formula for WayCoder（道码）
#
# 用法（自定义 tap，免提交 homebrew-core 审核）：
#   brew tap aleckstygit/waycoder https://gitee.com/aleckstygit/homebrew-waycoder
#   brew install waycoder
#
# 提交到 homebrew-core 前需：填 sha256（见下方注释）、补 test、过 brew audit
class Waycoder < Formula
  desc "中文版易用编程智能体，C# (.NET) NativeAOT 单文件 CLI 编程 Agent"
  homepage "https://gitee.com/aleckstygit/way-coder"
  license "MIT"
  version "0.96.105"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.105/waycoder-v0.96.105-osx-arm64.tar.gz"
    sha256 "874e1d865c9108d6ce08e39ffa3f94e3521dec667cfc459350b6dbbcb5b3b64c"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.105/waycoder-v0.96.105-osx-x64.tar.gz"
    sha256 "607b7df241b02cf0c4ff5b937e87ccec95d540de46874cfe7df6de924b8da22b"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
