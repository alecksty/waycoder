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
  version "0.84.1"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.84.1/waycoder-v0.84.1-osx-arm64.tar.gz"
    sha256 "74255c4d79db2cdbc7485ea19dd3b63a93f464cd2167eb477626b4a482bf7b4b"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.84.1/waycoder-v0.84.1-osx-x64.tar.gz"
    sha256 "7893f6cc695c0a8ae2c611df6276252570dc70431a1ccf2553e712bc1df694ab"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
