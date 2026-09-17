-- 生命游戏 —— 用 **Lua** 写的手机程序
--
-- 这是 Conway 的 Game of Life：双缓冲数组 + 邻居求和，**每格的状态由它自己与邻居算出**。
-- 与前面几份（弹球 / 段表）刻意不同：这里一整帧的开销几乎全在「遍历数组 + 条件求和」上，
-- 照的是前端「数组当主力」的那条路径。
--
-- 一个 20×24 的网格、初始随机撒点；每 300ms 推进一代。
-- 操作：回车重新撒点；SELECT 暂停/继续；ESC 或返回箭头退出。
--
-- ◆ 手机那套 UI
--
-- 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
-- 由 `vmltool.config.xml` 的 `<Language Name="lua" Libs="vmlui.vml">` 挂上来。
--
-- ◆ 为什么全部内联在 main 里
--
-- Lua 的 `function` 看不见**别的函数里的局部**，而这份的状态表（A/B 两张 480 格的表）
-- 正好是要跨帧保留的。写成全局表 + 顶层函数当然也可以，但本前端对全局表的行为没有把握
-- （Kotlin 的文件级 arrayOf 就是坏的），所以沿用 `corpus/lua/skel.lua` 的形状：
-- 一切都在 `main` 内。
--
-- ◆ 写法要求（前两条是实测出来的）
--
--   · **循环变量必须先 `local` 声明**再进 `for` —— 否则循环体一次都不执行
--     （v0.96.202 修的就是这条；`corpus/lua/skel.lua` 恰好写了 `local i = 0` 所以照不出来）。
--   · 表是 **1 基**的：扁平下标 `行*20 + 列` 要 `+1` 才能对上。
--   · 颜色写负数十进制。

function main()
    local W = 20
    local H = 24
    local N = W * H

    -- 两张 480 格的状态表：A 是当前代，B 是下一代（双缓冲）
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
    ui_win_open("生命游戏", w, h)

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
        -- ── draw ──
        ui_clear(-15724520)
        ui_text(8, 8, "世代", -6643536, 13, 0)
        ui_rect(58, 11, gen, 10, -11409298, 1, 0, 0)
        if paused ~= 0 then
            ui_text(w / 2, 8, "已暂停（SELECT 继续）", -6643536, 13, 1)
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
                                if dx ~= 0 or dy ~= 0 then
                                    local rr = row + dy
                                    local cc = col + dx
                                    -- 边界不环绕：出界当死
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
                ui_beep(880, 40)
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
