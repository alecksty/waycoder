# Homebrew formula for WayCoder（道码）
#
# 用法（自定义 tap，免提交 homebrew-core 审核）：
#   brew tap aleckstygit/waycoder https://gitee.com/aleckstygit/homebrew-waycoder
#   brew install waycoder
#
# 提交�?homebrew-core 前需：填 sha256（见下方注释）、补 test、过 brew audit
class Waycoder < Formula
  desc "中文版易用编程智能体，C# (.NET) NativeAOT 单文�?CLI 编程 Agent"
  homepage "https://gitee.com/aleckstygit/my-coder"
  license "MIT"
  version "0.84.0"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.84.0/waycoder-v0.84.0-osx-arm64.tar.gz"
    sha256 "790e16ddda73efc6fb1f461fc933e2b3122c9067611311fba5e57a0d95a1bd07"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.84.0/waycoder-v0.84.0-osx-x64.tar.gz"
    sha256 "29f50fb9efbdd71f7486ae820843a683578f56be51aad3e4104933f9baa2b7b2"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
