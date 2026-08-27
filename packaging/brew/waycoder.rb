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
  version "0.96.17"

  on_arm do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.17/waycoder-v0.96.17-osx-arm64.tar.gz"
    sha256 "ab2ce0b1593202c38d278b120c24ff12a42f0fbaa00bb3e91caa4877420d69b6"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.17/waycoder-v0.96.17-osx-x64.tar.gz"
    sha256 "a45572f10b9406d68f89d708149b56e3b94d3f39988a0d44c87a82a8c2b09063"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
