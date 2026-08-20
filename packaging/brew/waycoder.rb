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
  version "0.79.85"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.79.85/waycoder-v0.79.85-osx-arm64.tar.gz"
    sha256 "4906dc4eac1505c5f394a811b9993745aabca3f057f673311d403a6cb53f7b3c"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.79.85/waycoder-v0.79.85-osx-x64.tar.gz"
    sha256 "4bc46d5ce7b5a5fb766dc86f783bc42cd09942f6386ac7b43c5245bbe926a5eb"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
