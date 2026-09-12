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
  version "0.96.110"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.110/waycoder-v0.96.110-osx-arm64.tar.gz"
    sha256 "f4e129f5b59b2f7dc19fa3489c6c10c63dc90f1c5b0f37bc957ddf5bfe2fd8a7"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.110/waycoder-v0.96.110-osx-x64.tar.gz"
    sha256 "0f48105a2ce1eb97f71f2c533244c2299e2517caf5e01c137c0616836e82c9c6"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
