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
  version "0.96.68"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.68/waycoder-v0.96.68-osx-arm64.tar.gz"
    sha256 "c7f8014945d49994441c64130ef8814cf8e7e0f994c9c8595d7e8a3ce50b85fe"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.68/waycoder-v0.96.68-osx-x64.tar.gz"
    sha256 "9d65162150f16d83d2f609323469d0fad88cbd8c8a774508b340168e45a68155"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
