/**
 * 与 waycoder --web 服务的通信中继：POST /chat 发送，GET /events 消费 SSE 流式事件。
 * 协议（已核实）：`GET /events?client=<id>` 事件 token/tool/tool_output/done/failed/interrupted，
 * 帧格式 `event: <type>\ndata: <json>\n\n`。
 */

export type ChatEvent =
  | { type: 'token'; text: string }
  | { type: 'tool'; name: string; args: string }
  | { type: 'tool_output'; text: string }
  | { type: 'done'; answer: string }
  | { type: 'failed'; error: string }
  | { type: 'interrupted' }
  | { type: 'error'; message: string };

export interface ChatRelayOptions {
  baseUrl: string;
  onEvent: (ev: ChatEvent) => void;
  onClose: () => void;
}

export class ChatRelay {
  private clientId = Math.random().toString(36).slice(2, 10);
  private aborter?: AbortController;

  constructor(private opts: ChatRelayOptions) {}

  /** 发送消息并开始监听 SSE 事件流。 */
  async send(text: string): Promise<void> {
    this.aborter?.abort();
    this.aborter = new AbortController();
    const { baseUrl } = this.opts;
    await fetch(`${baseUrl}/chat`, { method: 'POST', body: text, signal: this.aborter.signal });
    void this.listenEvents();
  }

  interrupt(): void {
    void fetch(`${this.opts.baseUrl}/interrupt`, { method: 'POST' }).catch(() => {});
  }

  private async listenEvents(): Promise<void> {
    const { baseUrl, onEvent, onClose } = this.opts;
    try {
      const res = await fetch(`${baseUrl}/events?client=${this.clientId}`, { signal: this.aborter?.signal });
      if (!res.ok || !res.body) {
        onEvent({ type: 'error', message: `SSE 连接失败 (HTTP ${res.status})` });
        onClose();
        return;
      }
      const reader = res.body.getReader();
      const decoder = new TextDecoder();
      let buf = '';
      for (;;) {
        const { done, value } = await reader.read();
        if (done) break;
        buf += decoder.decode(value, { stream: true });
        let idx: number;
        while ((idx = buf.indexOf('\n\n')) >= 0) {
          const frame = buf.slice(0, idx);
          buf = buf.slice(idx + 2);
          this.parseFrame(frame);
        }
      }
    } catch (e) {
      if (e instanceof Error && e.name === 'AbortError') return;
      onEvent({ type: 'error', message: e instanceof Error ? e.message : String(e) });
    } finally {
      onClose();
    }
  }

  private parseFrame(frame: string): void {
    const { onEvent } = this.opts;
    let eventType = 'message';
    let data = '';
    for (const line of frame.split('\n')) {
      const t = line.trim();
      if (t.startsWith('event:')) eventType = t.slice(6).trim();
      else if (t.startsWith('data:')) data += t.slice(5).trim();
    }
    switch (eventType) {
      case 'token':
        onEvent({ type: 'token', text: extractText(data) });
        break;
      case 'tool':
        onEvent({ type: 'tool', name: extractText(data), args: data });
        break;
      case 'tool_output':
        onEvent({ type: 'tool_output', text: extractText(data) });
        break;
      case 'done':
        onEvent({ type: 'done', answer: extractText(data) });
        break;
      case 'failed':
        onEvent({ type: 'failed', error: extractText(data) });
        break;
      case 'interrupted':
        onEvent({ type: 'interrupted' });
        break;
    }
  }
}

/** 从 SSE data（可能为 JSON）提取可读文本。 */
function extractText(data: string): string {
  try {
    const o = JSON.parse(data);
    if (typeof o === 'string') return o;
    if (o && typeof o.text === 'string') return o.text;
    if (o && typeof o.content === 'string') return o.content;
    if (o && typeof o.answer === 'string') return o.answer;
    if (o && typeof o.message === 'string') return o.message;
    return data;
  } catch {
    return data;
  }
}
