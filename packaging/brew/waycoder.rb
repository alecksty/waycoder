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
  version "0.96.105"

  on_arm do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.105/waycoder-v0.96.105-osx-arm64.tar.gz"
    sha256 "5a9207cfa0bea6ec4a518e6448b24605bb9f90013e9c2c98de375b43929fab67"
  end

  on_intel do
    url "https://github.com/alecksty/waycoder/releases/download/v0.96.105/waycoder-v0.96.105-osx-x64.tar.gz"
    sha256 "3cc85478ebe4e375bda7db537a1cfad533a0b72b21e56e301805fdceef581d7f"
  end

  def install
    bin.install "waycoder"
  end

  test do
    assert_match "WayCoder", shell_output("#{bin}/waycoder --version")
  end
end
