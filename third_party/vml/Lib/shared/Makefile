# VML Shared Library Makefile
# 将所有C源文件编译为VML汇编（使用 --no-link 确保纯模块化输出）
# 每次VML运行时更新后, 执行 make 即可重新生成
#
# 也可以用脚本重建:
#   pwsh -File rebuild_shared.ps1       (Windows)
#   bash rebuild_shared.sh              (Linux/macOS)

CC = dotnet run --project ../../VMLPrepares/CCompiler --
CFLAGS = --no-link
SRCDIR = src
OUTDIR = .

# 自动发现 src/ 下所有 .c 文件
SRCS := $(notdir $(wildcard $(SRCDIR)/*.c))
OBJS := $(SRCS:%.c=$(OUTDIR)/%.vml)

.PHONY: all clean

all: $(OBJS)
	@echo "=== $(words $(OBJS)) 个共享库编译完成 ==="
	@ls -la $(OUTDIR)/*.vml

# 编译规则: C源文件 -> VML汇编（--no-link 防止自动链接 Lib/c/）
$(OUTDIR)/%.vml: $(SRCDIR)/%.c
	@echo "编译 $< -> $@"
	$(CC) $< -o $@ $(CFLAGS)
	@echo ""

# 清理
clean:
	rm -f $(OBJS)
	@echo "已清理所有编译输出"
