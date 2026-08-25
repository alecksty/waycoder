import * as child from 'child_process';
import * as net from 'net';

/**
 * 管理 waycoder --web 子进程：spawn、解析端口行、kill。
 * 协议：`waycoder --web <port>`（WAYCODER_WEB_NO_OPEN=1）→ stdout 行 `http://127.0.0.1:<port>`。
 */
export class WaycoderServer {
  private proc?: child.ChildProcess;
  private port = 0;

  get baseUrl(): string {
    return this.port > 0 ? `http://127.0.0.1:${this.port}` : '';
  }
  get running(): boolean {
    return this.proc !== undefined && !this.proc.killed;
  }

  /** 确保服务已启动，返回 baseUrl。requestedPort>0 用指定端口，否则选空闲端口。 */
  async ensureStarted(exe: string, cwd: string, requestedPort: number): Promise<string> {
    if (this.running) return this.baseUrl;
    this.port = requestedPort > 0 ? requestedPort : await this.freePort();

    return new Promise<string>((resolve, reject) => {
      const proc = child.spawn(exe, ['--web', String(this.port)], {
        cwd,
        env: { ...process.env, WAYCODER_WEB_NO_OPEN: '1' },
        windowsHide: true,
      });
      this.proc = proc;
      let settled = false;
      const settle = (err?: Error) => {
        if (settled) return;
        settled = true;
        if (err) reject(err);
        else resolve(this.baseUrl);
      };

      proc.stdout?.on('data', (d: Buffer) => {
        const m = d.toString().match(/http:\/\/127\.0\.0\.1:(\d+)/);
        if (m) { this.port = Number(m[1]); settle(); }
      });
      proc.stderr?.on('data', () => { /* 错误信息走 stderr，忽略 */ });
      proc.on('error', (err) => settle(err));
      proc.on('exit', (code) => settle(new Error(`waycoder --web 提前退出 (code=${code})`)));
      setTimeout(() => settle(new Error('waycoder --web 启动超时（5s 内未输出端口行）')), 5000);
    });
  }

  async stop(): Promise<void> {
    const p = this.proc;
    if (!p) return;
    this.proc = undefined;
    try { p.kill(); } catch { /* 已退出 */ }
    await new Promise<void>((res) => {
      p.on('exit', () => res());
      setTimeout(res, 3000);
    });
  }

  private freePort(): Promise<number> {
    return new Promise((resolve, reject) => {
      const srv = net.createServer();
      srv.unref();
      srv.on('error', reject);
      srv.listen(0, '127.0.0.1', () => {
        const port = (srv.address() as net.AddressInfo).port;
        srv.close(() => resolve(port));
      });
    });
  }
}
