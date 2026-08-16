# Homebrew formula for WayCoder（道码）
#
# 用法（自定义 tap，免提交 homebrew-core 审核）：
#   brew tap aleckstygit/waycoder https://gitee.com/aleckstygit/homebrew-waycoder
#   brew install waycoder
#
# 提交到 homebrew-core 前需：填 sha256（见下方注释）、补 test、过 brew audit
class Waycoder < Formula
  desc "中文版易用编程智能体，C# (.NET) NativeAOT 单文件 CLI 编程 Agent"
  homepage "https://gitee.com/aleckstygit/my-coder"
  license "MIT"
  version "0.69.0"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.69.0/waycoder-v0.69.0-osx-arm64.tar.gz"
    sha256 "445912a1fe29de8636f365fb61786abeed2860a2d1165274b7b0338b4e80311d"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.69.0/waycoder-v0.69.0-osx-x64.tar.gz"
    sha256 "ecae2e73822c3f068ed23e24d8058ab2b3668cac1a0dcc07769a35352ba3cdaa"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
