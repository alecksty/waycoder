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
  version "0.96.26"

  on_arm do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.26/waycoder-v0.96.26-osx-arm64.tar.gz"
    sha256 "750fb48dd10aec1f634ec97a334718543f106eaa473aa76b30434676ee4a1ab2"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.26/waycoder-v0.96.26-osx-x64.tar.gz"
    sha256 "21d2b82fe98df05b0795d6854fff2f9f60a5b96f1fe0cc1a37bd7835e83c2742"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
