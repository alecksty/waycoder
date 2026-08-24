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
  version "0.87.18"

  on_arm do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.87.18/waycoder-v0.87.18-osx-arm64.tar.gz"
    sha256 "05c2ef192fc95eb6e95129f9d58c0ff19134710efe6a466ed968f4479a654a25"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.87.18/waycoder-v0.87.18-osx-x64.tar.gz"
    sha256 "9ff98e5797db9467c55ac316ccd5e85bf9d41ca22f9120c511f4ea5d68cb1e3d"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
