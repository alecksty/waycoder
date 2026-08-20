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
  version "0.79.76"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.79.76/waycoder-v0.79.76-osx-arm64.tar.gz"
    sha256 "1ef7def7631ebbf15e5a31c38735dca1d439ac7a26c11bc612f4381cbe4226aa"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.79.76/waycoder-v0.79.76-osx-x64.tar.gz"
    sha256 "6534cbd8849aed78d5aceb7ac5841c0c360f4b5e62ac4d4ff3cfa51f0ee53c31"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
