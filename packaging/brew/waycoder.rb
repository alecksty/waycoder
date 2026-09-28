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
  version "0.96.559"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.559/waycoder-v0.96.559-osx-arm64.tar.gz"
    sha256 "06fa9ec67e9beb6c83974195aa7c18d4cf1dcbb8d5fae5f11028ca0a37abc852"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.559/waycoder-v0.96.559-osx-x64.tar.gz"
    sha256 "906933cebe2e24812a76df7e0f38fd329a67feccea777187b210ab9d4a1a44f6"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
