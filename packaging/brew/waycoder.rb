# Homebrew formula for WayCoder（道码）
#
# 用法（自定义 tap，免提交 homebrew-core 审核）：
#   brew tap aleckstygit/waycoder https://gitee.com/aleckstygit/homebrew-waycoder
#   brew install waycoder
#
# 提交�?homebrew-core 前需：填 sha256（见下方注释）、补 test、过 brew audit�?
class Waycoder < Formula
  desc "中文版易用编程智能体，C# (.NET) NativeAOT 单文�?CLI 编程 Agent"
  homepage "https://gitee.com/aleckstygit/my-coder"
  license "MIT"
  version "0.55.0"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.55.0/waycoder-v0.55.0-osx-arm64.tar.gz"
    # �?`curl -L <url> | shasum -a 256` 填充
    sha256 "eb26fdc0497709162a5081eb4a05c5063af7f64b2883a2ffc8c136152bffe528"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.55.0/waycoder-v0.55.0-osx-x64.tar.gz"
    sha256 "eb26fdc0497709162a5081eb4a05c5063af7f64b2883a2ffc8c136152bffe528"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
