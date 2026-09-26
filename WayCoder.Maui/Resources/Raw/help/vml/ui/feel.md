# 音效与触感

合成音、音频文件、震动、屏幕常亮。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_beep(int freq, int ms)`
**现场合成**一个音（不用带音频文件）：`freq` 赫兹、`ms` 毫秒。
⚠ **单通道** —— 一次只响一个，连着发的只有最后一个听得见。所以用**音高**表达好坏：消一行 880Hz、四行 1568Hz，赢了盖过一切。
```c
ui_beep(880, 80);      /* 消一行 */
ui_beep(1568, 160);    /* 消四行，音更高 */
```
### `ui_audio_play(char* path, int loop)`
播放一个**音频文件**（相对路径按沙箱根解）。`loop` 非 0 = 循环（BGM 用）。
返回 0 成功 / -1 失败（文件不存在 / 路径非法 / 平台不支持）。
```c
ui_audio_play("bgm.mp3", 1);   /* 循环播放 */
```
### `ui_audio_stop(void)`
停掉正在播的音频。
```c
ui_audio_stop();
```
### `ui_audio_volume(int volume)`
整体音量 0–100（超出会被钳）。对**之后**播放的音生效。
```c
ui_audio_volume(60);
```
### `ui_keep_on(int on)`
屏幕常亮开关（玩游戏的都该开）。
```c
ui_keep_on(1);   /* 1 开 0 关 */
```
### `ui_vibrate(int ms, int strength)`
震动：`ms` 毫秒，`strength` 强度（0–255，0 = 用系统默认）。
```c
ui_vibrate(120, 0);
```
### `ui_vibrate_pattern(int* pattern, int count)`
按**节奏**震动：`pattern` 是 int 数组（奇数下标 = 静、偶数下标 = 动，同 Android 语义），
`count` 是段数。返回 0 成功 / -1 失败。
```c
int pat[4] = { 100, 60, 100, 60 };
ui_vibrate_pattern(pat, 4);   /* 震-停-震-停 */
```
