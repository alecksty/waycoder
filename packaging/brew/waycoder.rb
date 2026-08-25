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
  version "0.96.4"

  on_arm do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.4/waycoder-v0.96.4-osx-arm64.tar.gz"
    sha256 "909c456b26c3d2e95406594c5ccb4d19683c28645389bbcc9f235a60c57c40ed"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.4/waycoder-v0.96.4-osx-x64.tar.gz"
    sha256 "8e417c522b53d2f5d3c2a61cef3899ad7c7435830d264b509215bc7cceb9f54e"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
