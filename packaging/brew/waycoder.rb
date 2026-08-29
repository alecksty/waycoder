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
  version "0.96.25"

  on_arm do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.25/waycoder-v0.96.25-osx-arm64.tar.gz"
    sha256 "3f474e062f70b42e35672c4160053390c3c92fc39f01586ad270a15d70e36eb6"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.25/waycoder-v0.96.25-osx-x64.tar.gz"
    sha256 "4a21feb030aa37db2f035a1f2c7e4612efde807090f3118f799ef39ca7071798"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
