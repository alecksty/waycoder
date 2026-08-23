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
  version "0.87.3"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.87.3/waycoder-v0.87.3-osx-arm64.tar.gz"
    sha256 "c5fc7cafdd76b026211d8a4564dbcc41674e42eaf869d534e26271a3aba82b15"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.87.3/waycoder-v0.87.3-osx-x64.tar.gz"
    sha256 "0f16171b329a46019caa67707a0668338b042120c3c69d05bd4e20dc4fb2b460"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
