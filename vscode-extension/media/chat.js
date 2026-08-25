// WayCoder 聊天面板前端：postMessage 与扩展中继。
// @ts-nocheck
(function () {
  const messages = document.getElementById('messages');
  const input = document.getElementById('input');
  const sendBtn = document.getElementById('send');
  const interruptBtn = document.getElementById('interrupt');

  let busy = false;
  let currentAssistant = null; // 当前增量渲染的 assistant 消息元素

  const vscode = acquireVsCodeApi();

  function addMsg(role, text) {
    const el = document.createElement('div');
    el.className = 'msg ' + role;
    el.textContent = text;
    messages.appendChild(el);
    messages.scrollTop = messages.scrollHeight;
    return el;
  }

  function setBusy(b) {
    busy = b;
    sendBtn.disabled = b;
    interruptBtn.disabled = !b;
    if (b) currentAssistant = addMsg('assistant', '');
    else currentAssistant = null;
  }

  function appendToAssistant(text) {
    if (!currentAssistant) currentAssistant = addMsg('assistant', '');
    currentAssistant.textContent += text;
    messages.scrollTop = messages.scrollHeight;
  }

  function send() {
    const text = input.value.trim();
    if (!text || busy) return;
    addMsg('user', text);
    input.value = '';
    setBusy(true);
    vscode.postMessage({ type: 'send', text });
  }

  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); send(); }
  });
  sendBtn.addEventListener('click', send);
  interruptBtn.addEventListener('click', () => vscode.postMessage({ type: 'interrupt' }));

  window.addEventListener('message', (e) => {
    const msg = e.data;
    switch (msg.type) {
      case 'user':
        addMsg('user', msg.text);
        break;
      case 'token':
        appendToAssistant(msg.text);
        break;
      case 'tool':
        appendToAssistant('\n[工具] ' + msg.name);
        break;
      case 'tool_output':
        appendToAssistant('\n[输出] ' + msg.text.slice(0, 200));
        break;
      case 'done':
        if (msg.answer) appendToAssistant(msg.answer);
        setBusy(false);
        break;
      case 'failed':
        appendToAssistant('\n❌ 失败: ' + msg.error);
        setBusy(false);
        break;
      case 'interrupted':
        appendToAssistant('\n⏹ 已中断');
        setBusy(false);
        break;
      case 'error':
        addMsg('error', '错误: ' + msg.message);
        setBusy(false);
        break;
      case 'status':
        if (msg.message) appendToAssistant(msg.message);
        break;
    }
  });
})();
