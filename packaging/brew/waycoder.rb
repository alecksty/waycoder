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
  version "0.96.109"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.109/waycoder-v0.96.109-osx-arm64.tar.gz"
    sha256 "11836209695ad08e8e752ce4823598dcb1a7e69dfb3849d96f666592e92a0d0a"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.109/waycoder-v0.96.109-osx-x64.tar.gz"
    sha256 "15c38c1de8111f5eac4380fc51d876927c0a873d744af4ea0a0ad04deab2bd0a"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
