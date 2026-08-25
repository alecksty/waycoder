import * as vscode from 'vscode';
import { ChatRelay, ChatEvent } from './relay';

/**
 * 聊天 Webview 面板：消息列表 + 输入框 + 发送/中断。
 * webview ↔ 扩展 postMessage 双向中继：
 *   webview → {type:'send'|'interrupt'}；扩展 → ChatEvent（token/tool/done/failed/...）。
 */
export class ChatPanel {
  private static current?: ChatPanel;

  private readonly panel: vscode.WebviewPanel;
  private relay?: ChatRelay;

  private constructor(
    private readonly server: { baseUrl: string },
    private readonly extensionUri: vscode.Uri
  ) {
    this.panel = vscode.window.createWebviewPanel(
      'waycoder.chat',
      'WayCoder 对话',
      vscode.ViewColumn.One,
      { enableScripts: true, retainContextWhenHidden: true }
    );
    this.panel.webview.html = this.buildHtml();
    this.panel.webview.onDidReceiveMessage((msg) => {
      if (msg.type === 'send' && typeof msg.text === 'string') void this.send(msg.text);
      else if (msg.type === 'interrupt') this.relay?.interrupt();
    });
    this.panel.onDidDispose(() => {
      if (ChatPanel.current === this) ChatPanel.current = undefined;
      this.relay?.interrupt();
    });
  }

  static async show(server: { baseUrl: string }, extensionUri: vscode.Uri): Promise<ChatPanel> {
    if (ChatPanel.current) {
      ChatPanel.current.panel.reveal();
      return ChatPanel.current;
    }
    ChatPanel.current = new ChatPanel(server, extensionUri);
    return ChatPanel.current;
  }

  /** 显示用户消息（发送选中代码时用）。 */
  appendUser(text: string): void {
    this.post({ type: 'user', text });
  }

  /** 直接发送一条 prompt（选中代码命令用，绕过 webview 输入框）。 */
  async sendPrompt(text: string): Promise<void> {
    await this.send(text);
  }

  /** 发一条消息并挂起 SSE 事件流。 */
  private async send(text: string): Promise<void> {
    const relay = new ChatRelay({
      baseUrl: this.server.baseUrl,
      onEvent: (ev) => this.post(ev),
      onClose: () => {},
    });
    this.relay = relay;
    this.post({ type: 'status', message: '…' });
    try {
      await relay.send(text);
    } catch (e) {
      this.post({ type: 'error', message: e instanceof Error ? e.message : String(e) });
    }
  }

  private post(ev: ChatEvent | { type: 'status'; message: string } | { type: 'user'; text: string }): void {
    void this.panel.webview.postMessage(ev);
  }

  private buildHtml(): string {
    const nonce = Math.random().toString(36).slice(2);
    const scriptUri = this.panel.webview.asWebviewUri(
      vscode.Uri.joinPath(this.extensionUri, 'media', 'chat.js')
    );
    const cssUri = this.panel.webview.asWebviewUri(
      vscode.Uri.joinPath(this.extensionUri, 'media', 'chat.css')
    );
    return `<!DOCTYPE html>
<html lang="zh">
<head>
<meta charset="UTF-8">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src ${this.panel.webview.cspSource}; script-src 'nonce-${nonce}';">
<link rel="stylesheet" href="${cssUri}">
</head>
<body>
  <div id="messages"></div>
  <div id="inputbar">
    <textarea id="input" placeholder="输入任务…（Ctrl+Enter 发送）" rows="3"></textarea>
    <div id="actions">
      <button id="send">发送</button>
      <button id="interrupt" disabled>中断</button>
    </div>
  </div>
  <script nonce="${nonce}" src="${scriptUri}"></script>
</body>
</html>`;
  }
}
