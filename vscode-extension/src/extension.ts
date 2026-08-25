import * as vscode from 'vscode';
import { WaycoderServer } from './server';
import { ChatPanel } from './chatPanel';

const server = new WaycoderServer();

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  context.subscriptions.push(
    vscode.commands.registerCommand('waycoder.openChat', () => openChat(context)),
    vscode.commands.registerCommand('waycoder.explainSelection', () => sendSelection(context, '请解释下面这段代码：')),
    vscode.commands.registerCommand('waycoder.fixSelection', () => sendSelection(context, '请修复下面这段代码的问题：')),
    vscode.commands.registerCommand('waycoder.interrupt', () => vscode.window.showInformationMessage('在聊天面板中点击「中断」按钮'))
  );
}

/** 确保 waycoder --web 服务已启动，返回 baseUrl。 */
async function ensureServer(): Promise<string> {
  const cfg = vscode.workspace.getConfiguration('waycoder');
  const exe = (cfg.get<string>('path') || 'waycoder').trim();
  const requestedPort = cfg.get<number>('port') || 0;
  const model = (cfg.get<string>('model') || '').trim();

  // 模型覆盖：通过环境变量传给子进程（WAYCODER_MODEL）
  const cwd = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath ?? process.cwd();
  process.env.WAYCODER_MODEL = model || process.env.WAYCODER_MODEL;

  if (!server.running) {
    try {
      await server.ensureStarted(exe, cwd, requestedPort);
    } catch (e) {
      const err = e instanceof Error ? e.message : String(e);
      const pick = await vscode.window.showErrorMessage(
        `WayCoder 服务启动失败：${err}\n请确认 waycoder 已安装且在 PATH（或用配置 waycoder.path 指定路径）。`,
        '重试',
        '帮助'
      );
      if (pick === '重试') return ensureServer();
      throw new Error(err);
    }
  }
  return server.baseUrl;
}

async function openChat(context: vscode.ExtensionContext): Promise<void> {
  const baseUrl = await ensureServer();
  await ChatPanel.show({ baseUrl }, context.extensionUri);
}

/** 把选中代码作为 prompt 发送到聊天面板。 */
async function sendSelection(context: vscode.ExtensionContext, prefix: string): Promise<void> {
  const editor = vscode.window.activeTextEditor;
  const selection = editor?.selection;
  if (!editor || !selection || selection.isEmpty) {
    vscode.window.showInformationMessage('请先在编辑器中选中代码。');
    return;
  }
  const text = editor.document.getText(selection);
  const fileName = editor.document.fileName.split(/[\\/]/).pop() ?? '';
  const prompt = `${prefix}\n\n文件：${fileName}\n\`\`\`\n${text}\n\`\`\``;

  const baseUrl = await ensureServer();
  const panel = await ChatPanel.show({ baseUrl }, context.extensionUri);
  panel.appendUser(prompt);
  await panel.sendPrompt(prompt);
}

export async function deactivate(): Promise<void> {
  await server.stop();
}
