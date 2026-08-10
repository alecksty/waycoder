# 聊天应用 (Chat App)

一个简洁的中文 Web 聊天应用前端。

## 文件结构

| 文件 | 说明 |
|------|------|
| `index.html` | 页面结构（header / 消息容器 / 输入区） |
| `style.css` | 样式（由并行任务生成） |
| `script.js` | 聊天逻辑（由并行任务生成） |

## 接口约定

- `header.chat-header` — 顶部标题栏
- `#chat-messages` — 消息列表容器（消息由 JS 动态渲染）
- `.chat-input-area` — 输入区，包含：
  - `textarea#chat-input` — 多行输入框，支持回车发送
  - `button#send-btn` — 发送按钮（文本「发送」）

## 使用方式

直接用浏览器打开 `index.html` 即可（三个文件需放在同一目录）。
