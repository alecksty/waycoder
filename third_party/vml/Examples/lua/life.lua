-- 生命游戏 —— 用 **Lua** 写的手机程序
-- Game of Life — a phone program written in **Lua**
--
-- 这是 Conway 的 Game of Life：双缓冲数组 + 邻居求和，**每格的状态由它自己与邻居算出**。
-- This is Conway's Game of Life: double-buffered arrays + neighbor summing, **each cell's state computed from itself and its neighbors**.
-- 与前面几份（弹球 / 段表）刻意不同：这里一整帧的开销几乎全在「遍历数组 + 条件求和」上，
-- Deliberately different from the earlier ones (bouncing ball / jump table): here a whole frame's cost is almost all in "walking arrays + conditional summing",
-- 照的是前端「数组当主力」的那条路径。
-- which exercises the frontend's "arrays as the workhorse" path.
--
-- 一个 20×24 的网格、初始随机撒点；每 300ms 推进一代。
-- A 20×24 grid, randomly seeded at startup; one generation advances every 300ms.
-- 操作：回车重新撒点；SELECT 暂停/继续；ESC 或返回箭头退出。
-- Controls: Enter re-seeds; SELECT pauses/resumes; ESC or the back arrow exits.
--
-- ◆ 手机那套 UI
-- ◆ The phone's UI set
--
-- 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
-- Window opening / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` → `vmlui.vml`),
-- 由 `vmltool.config.xml` 的 `<Language Name="lua" Libs="vmlui.vml">` 挂上来。
-- and are hooked in by `<Language Name="lua" Libs="vmlui.vml">` in `vmltool.config.xml`.
--
-- ◆ 为什么全部内联在 main 里
-- ◆ Why everything is inlined into main
--
-- Lua 的 `function` 看不见**别的函数里的局部**，而这份的状态表（A/B 两张 480 格的表）
-- Lua's `function` cannot see **locals of another function**, and this file's state tables (the two 480-cell tables A/B)
-- 正好是要跨帧保留的。写成全局表 + 顶层函数当然也可以，但本前端对全局表的行为没有把握
-- are exactly what must survive across frames. Writing it as global tables + top-level functions is of course possible, but this frontend's behavior around global tables is not something I trust
-- （Kotlin 的文件级 arrayOf 就是坏的），所以沿用 `corpus/lua/skel.lua` 的形状：
-- (Kotlin's file-level arrayOf is broken), so it follows the shape of `corpus/lua/skel.lua`:
-- 一切都在 `main` 内。
-- everything stays inside `main`.
--
-- ◆ 写法要求（前两条是实测出来的）
-- ◆ Writing requirements (the first two were measured in practice)
--
--   · **循环变量必须先 `local` 声明**再进 `for` —— 否则循环体一次都不执行
--   · **The loop variable must be declared with `local`** before entering `for` — otherwise the loop body never runs even once
--     （v0.96.202 修的就是这条；`corpus/lua/skel.lua` 恰好写了 `local i = 0` 所以照不出来）。
--     (this is exactly what v0.96.202 fixed; `corpus/lua/skel.lua` happens to write `local i = 0`, so it never showed up there).
--   · 表是 **1 基**的：扁平下标 `行*20 + 列` 要 `+1` 才能对上。
--   · Tables are **1-based**: the flat index `row*20 + col` needs `+1` to line up.
--   · 颜色写负数十进制。
--   · Colors are written as negative decimal numbers.

function main()
    local W = 20
    local H = 24
    local N = W * H

    -- 两张 480 格的状态表：A 是当前代，B 是下一代（双缓冲）
    -- Two 480-cell state tables: A is the current generation, B is the next (double buffering)
    local A = {}
    local B = {}

    local w = ui_scr_w()
    local h = ui_scr_h()
    if w <= 0 then
        w = 360
    end
    if h <= 0 then
        h = 620
    end
    -- 界面语言：开局问一次宿主要中文还是英文（0=中文 1=英文），之后整局按它分支。
    -- UI language: ask the host once at startup whether it wants Chinese or English (0=Chinese 1=English), then branch on it for the whole game.
    -- ⚠ 别在每帧里调 —— 那是一次 syscall。
    -- ⚠ Do not call it in every frame — it is a syscall.
    -- ⚠ **不能用 `(lang == 0) and "中文" or "English"`** —— 本前端的 `and`/`or` 求的是
    -- ⚠ **Do not use `(lang == 0) and "Chinese" or "English"`** — this frontend's `and`/`or` evaluate
    --   **布尔 0/1**（`ExpressionManager.EmitAnd/EmitOr` 收尾就是 `MOVE R0, 0/1`），
    --   **boolean 0/1** (`ExpressionManager.EmitAnd/EmitOr` finish with exactly `MOVE R0, 0/1`),
    --   不是操作数的值 ⇒ 当字符串用会拿到 0/1，画出来是乱码（实测窗口标题变成 "VML"）。
    --   not the value of the operand ⇒ using it as a string yields 0/1 and draws garbage (measured: the window title became "VML").
    --   只有 `if/else` 赋值这条路能用。
    --   Only the `if/else` assignment route works.
    local lang = ui_get_language()
    local t_title = "Game of Life"
    local t_gen = "Generation"
    local t_paused = "Paused (SELECT)"
    if lang == 0 then t_title = "生命游戏"
    else t_title = "Game of Life" end
    if lang == 0 then t_gen = "世代"
    else t_gen = "Generation" end
    if lang == 0 then t_paused = "已暂停（SELECT 继续）"
    else t_paused = "Paused (SELECT)" end

    ui_win_open(t_title, w, h)

    local cell = (w - 8) / W
    if (h - 90) / H < cell then
        cell = (h - 90) / H
    end
    if cell < 4 then
        cell = 4
    end
    local ox = (w - cell * W) / 2
    local oy = 50

    -- 初始撒点：ui_rand(2) 给 0 或 1
    -- Initial seeding: ui_rand(2) gives 0 or 1
    local i = 0
    i = 1
    while i <= N do
        A[i] = ui_rand(2)
        B[i] = 0
        i = i + 1
    end

    ui_keep_on(1)
    local paused = 0
    local gen = 0
    local tid = ui_timer_set(300, 0)

    while ui_win_closed() == 0 do

      ui_sfx_tick()
        -- ── draw ──
        ui_clear(-15724520)
        ui_text(8, 8, t_gen, -6643536, 13, 0)
        ui_rect(58, 11, gen, 10, -11409298, 1, 0, 0)
        if paused ~= 0 then
            ui_text(w / 2, 8, t_paused, -6643536, 13, 1)
        end

        local row = 0
        local col = 0
        while row < H do
            col = 0
            while col < W do
                local idx = row * W + col + 1
                if A[idx] ~= 0 then
                    ui_rect(ox + col * cell, oy + row * cell, cell - 1, cell - 1, -131246, 1, 0, 0)
                end
                col = col + 1
            end
            row = row + 1
        end
        ui_present()

        local t = ui_wait_msg(0)
        if t == 10 then
            break
        end

        if t == 9 then
            if paused == 0 then
                -- ── 推进一代：数邻居 → 套规则 → 写 B ──
                -- ── Advance one generation: count neighbors → apply the rules → write B ──
                row = 0
                while row < H do
                    col = 0
                    while col < W do
                        local n = 0
                        local dy = 0 - 1
                        while dy <= 1 do
                            local dx = 0 - 1
                            while dx <= 1 do
                                -- 只数八邻域、跳过自己
                                -- Count only the eight-neighborhood, skipping the cell itself
                                if dx ~= 0 or dy ~= 0 then
                                    local rr = row + dy
                                    local cc = col + dx
                                    -- 边界不环绕：出界当死
                                    -- Edges do not wrap: out of bounds counts as dead
                                    if rr >= 0 and rr < H then
                                        if cc >= 0 and cc < W then
                                            local j = rr * W + cc + 1
                                            if A[j] ~= 0 then
                                                n = n + 1
                                            end
                                        end
                                    end
                                end
                                dx = dx + 1
                            end
                            dy = dy + 1
                        end

                        local idx = row * W + col + 1
                        -- 规则：活细胞 2~3 邻居存活；死细胞恰好 3 邻居复活
                        -- Rules: a live cell with 2~3 neighbors survives; a dead cell with exactly 3 neighbors comes back to life
                        if A[idx] ~= 0 then
                            if n == 2 or n == 3 then
                                B[idx] = 1
                            else
                                B[idx] = 0
                            end
                        else
                            if n == 3 then
                                B[idx] = 1
                            else
                                B[idx] = 0
                            end
                        end
                        col = col + 1
                    end
                    row = row + 1
                end

                -- 双缓冲交换：B 拷回 A
                -- Double-buffer swap: copy B back into A
                i = 1
                while i <= N do
                    A[i] = B[i]
                    i = i + 1
                end
                gen = gen + 1
            end
        end

        if t == 1 then
            local k = ui_msg_a()
            if k == 27 then
                break
            end
            if k == 13 then
                i = 1
                while i <= N do
                    A[i] = ui_rand(2)
                    i = i + 1
                end
                gen = 0
                ui_sfx_add(0, 84, 0, 2, 70, 1)   -- 换代：短促
                -- generation change: short and crisp
            end
            if k == 16 then
                if paused == 0 then
                    paused = 1
                else
                    paused = 0
                end
            end
        end
    end

    ui_timer_kill(tid)
    ui_keep_on(0)
    ui_win_close()
end

main()
