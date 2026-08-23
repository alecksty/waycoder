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
  version "0.87.10"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.87.10/waycoder-v0.87.10-osx-arm64.tar.gz"
    sha256 "ec291b30f0c6e884e492d23470da4f3e2df89e5ecb6a052dee74011d5fb68dfa"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.87.10/waycoder-v0.87.10-osx-x64.tar.gz"
    sha256 "31b501ec6312510f7f275d80dd5aae047e7e2c897843ea09ceb8084ec2f2677c"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
