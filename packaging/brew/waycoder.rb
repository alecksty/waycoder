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
  version "0.87.19"

  on_arm do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.87.19/waycoder-v0.87.19-osx-arm64.tar.gz"
    sha256 "14a5fef31202b3737a0600b6cdc9a93e5cf2102a26f649b847b53a5ee90e1bbc"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.87.19/waycoder-v0.87.19-osx-x64.tar.gz"
    sha256 "a945588b3ddc2356c0699d4a63e6d8d441c238bd091a7fe5796cfdbb4e4857b3"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
