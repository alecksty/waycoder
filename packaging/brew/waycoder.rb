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
  version "0.79.87"

  on_arm do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.79.87/waycoder-v0.79.87-osx-arm64.tar.gz"
    sha256 "2571fcdce57716803fb0dfd27bc0d73e325bc4a9b4fb56a01b9d15a9cca4cfdc"
  end

  on_intel do
    url "https://gitee.com/aleckstygit/my-coder/releases/download/v0.79.87/waycoder-v0.79.87-osx-x64.tar.gz"
    sha256 "19ec00dd2de3e58a1c643a9592d7eead115edaca2769e7620668538a6b74ad17"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
