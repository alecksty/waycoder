# Homebrew formula for WayCoder（道码）
#
# 用法（自定义 tap，免提交 homebrew-core 审核）：
#   brew tap aleckstygit/waycoder https://gitee.com/aleckstygit/homebrew-waycoder
#   brew install waycoder
#
# 提交到 homebrew-core 前需：填 sha256（见下方注释）、补 test、过 brew audit
class Waycoder < Formula
  desc "中文版易用编程智能体，C# (.NET) NativeAOT 单文件 CLI 编程 Agent"
  homepage "https://gitee.com/aleckstygit/my-coder"
  license "MIT"
  version "0.71.4"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.71.4/waycoder-v0.71.4-osx-arm64.tar.gz"
    sha256 "de054a6d20b1e9af6ac6e1f4859265624583a0e69df1620a4831f06fcec7a61b"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.71.4/waycoder-v0.71.4-osx-x64.tar.gz"
    sha256 "8ec9ed9b4a3ee7148ff277f336558e02c0be7c51638b32679d438f8187d70e29"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
