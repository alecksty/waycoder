# 音效与触感

合成音、音频文件、震动、屏幕常亮。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_beep(int freq, int ms)`
**现场合成**一个音（不用带音频文件）：`freq` 赫兹、`ms` 毫秒。
⚠ **单通道** —— 一次只响一个，连着发的只有最后一个听得见。所以用**音高**表达好坏：消一行 880Hz、四行 1568Hz，赢了盖过一切。
⚠ 这是它的**老语义，没有变**。要**同时**响几个音（和弦 / 旋律）用下面的 `ui_tone_on/off`。
```c
ui_beep(880, 80);      /* 消一行 */
ui_beep(1568, 160);    /* 消四行，音更高 */
```
### `ui_tone_on(int ch, int note, int vel)` / `ui_tone_off(int ch, int note)`
**复音** —— 多个音**同时**响（最多 32 个声部）。
音符号是**真 MIDI 语义**：A4 = 69 = 440Hz、中央 C = 60。通道 0–15、力度 0–127（**0 等同关音**）。

按和弦就是连续起几个音、**中间不关**：
```c
ui_tone_on(0, 60, 100);   /* do  */
ui_tone_on(1, 64, 100);   /* mi  */
ui_tone_on(2, 67, 100);   /* sol —— 三个音同时在响 */
...
ui_tone_off(2, 67); ui_tone_off(1, 64); ui_tone_off(0, 60);
```
⚠ 参数**一律钳、不拒**：越界的音符号会被钳到 0–127 继续响，而不是默默不发声。
⚠ `ui_beep` 与它**互不干扰** —— 蜂鸣走自己的专用声道，按着和弦时照常打音效也没事。

现成例子：`Examples/c/audio_test.c`（音阶/和弦/旋律）与 `Examples/c/piano.c`（钢琴）。
### `ui_tone_all_off(void)` / `ui_tone_voices(void)`
全关（走**淡出**，不是硬切）；查"此刻在响的声部数" —— 调和弦时有用。
### `ui_tone_wave(int ch, int wave)` / `ui_tone_max_voices(int n)`
设**某个通道**的默认波形（`VML_WAVE_SINE`(0) / `SQUARE`(1) / `SAW`(2) / `TRIANGLE`(3)）、
设**同时允许的声部上限**（1–32，默认 `VML_TONE_MAX_VOICES` = 32）。
音色不同是"和弦听起来不糊"的一半：几个音同时响时，**给低音声部换 `TRIANGLE`**（谐波少、
不抢）、旋律声部留 `SQUARE`（亮），比全用同一种波形清楚得多。
```c
ui_tone_wave(0, VML_WAVE_TRIANGLE);   /* 通道 0 当低音铺底 */
ui_tone_wave(1, VML_WAVE_SQUARE);     /* 通道 1 走旋律 */
ui_tone_max_voices(16);               /* 声部够用就行，太多会互相掩盖 */
```
### `ui_tone_panic(void)`
**立刻**全停（**不进淡出**，声音当场断掉）。用于「用户按了强制停止」这种场合 ——
淡出才安静下来的那几十毫秒，在急停的语义下就是"没停"。
```c
ui_tone_panic();   /* 一键静音 */
```
⚠ 急停只是**把声部停掉**，程序自己的记账要一并清 —— 否则"还按着的手指"抬手时
会去减一本已经清空的账，那个键以后就起不来音了。`Examples/c/piano.c` 的「清音」键
就是三件事一起做的：`ui_tone_panic()` + 清引用计数 + 清槽位记账。
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
