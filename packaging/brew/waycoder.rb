# Homebrew formula for WayCoder（道码）
#
# 用法（自定义 tap，免提交 homebrew-core 审核）：
#   brew tap aleckstygit/waycoder https://gitee.com/aleckstygit/homebrew-waycoder
#   brew install waycoder
#
# 提交到 homebrew-core 前需：填 sha256（见下方注释）、补 test、过 brew audit。
class Waycoder < Formula
  desc "中文版易用编程智能体，C# (.NET) NativeAOT 单文件 CLI 编程 Agent"
  homepage "https://gitee.com/aleckstygit/my-coder"
  license "MIT"
  version "0.48.7"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.48.7/waycoder-v0.48.7-osx-arm64.tar.gz"
    # 用 `curl -L <url> | shasum -a 256` 填充
    sha256 "REPLACE_WITH_SHA256"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.48.7/waycoder-v0.48.7-osx-x64.tar.gz"
    sha256 "REPLACE_WITH_SHA256"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
