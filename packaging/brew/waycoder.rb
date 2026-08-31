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
  version "0.96.36"

  on_arm do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.36/waycoder-v0.96.36-osx-arm64.tar.gz"
    sha256 "3fd51cb13f33c4b42004916e2483dd26404524340ae6ee90976311a9e24b31f6"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.36/waycoder-v0.96.36-osx-x64.tar.gz"
    sha256 "7f02f70c4cc6eecf7f333c08bc8a1499cb783be2399a2c15d772c3f198960721"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
