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
  version "0.96.67"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.67/waycoder-v0.96.67-osx-arm64.tar.gz"
    sha256 "40142e08cb3a0d068fff2c4cc9eac75013bad1a2427acdd755f61fc076f1aab1"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.67/waycoder-v0.96.67-osx-x64.tar.gz"
    sha256 "64491fc48c2ae6fa0a918fbdd9c5103bd50d459a58ec93f06c386f6d0839b895"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
