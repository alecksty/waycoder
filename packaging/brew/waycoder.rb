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
  version "0.87.11"

  on_arm do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.87.11/waycoder-v0.87.11-osx-arm64.tar.gz"
    sha256 "7be07dcbedd82c6ccd4c8e454259b42427d40d3f420e87e390ded92e0b012d22"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.87.11/waycoder-v0.87.11-osx-x64.tar.gz"
    sha256 "69fce101d632c64322f5b886873353703e49feb87010c3031e715070fc010f82"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
