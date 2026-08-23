# Homebrew formula for WayCoder（道码）
#
# 用法（自定义 tap，免提交 homebrew-core 审核）：
#   brew tap aleckstygit/waycoder https://gitee.com/aleckstygit/homebrew-waycoder
#   brew install waycoder
#
# 提交�?homebrew-core 前需：填 sha256（见下方注释）、补 test、过 brew audit
class Waycoder < Formula
  desc "中文版易用编程智能体，C# (.NET) NativeAOT 单文�?CLI 编程 Agent"
  homepage "https://gitee.com/aleckstygit/my-coder"
  license "MIT"
  version "0.86.1"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.86.1/waycoder-v0.86.1-osx-arm64.tar.gz"
    sha256 "1caa4bfe9edc70dc73a08045889d153717136e4deb02b17b3c66cf6bbdb0509e"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.86.1/waycoder-v0.86.1-osx-x64.tar.gz"
    sha256 "028321c628cca1a94b0978ad4c8007a2476c991e0040580ce6555a496e86ba37"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
