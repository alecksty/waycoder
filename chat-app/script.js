// chat-app/script.js — 简易中文聊天机器人逻辑（原生 ES6+，无外部库）
(() => {
  'use strict';

  const messagesEl = document.getElementById('chat-messages');
  const inputEl = document.getElementById('chat-input');
  const sendBtn = document.getElementById('send-btn');

  /** 内置中文回复池 */
  const REPLIES = [
    '收到！我已经记下来了，还有别的事要帮忙吗？',
    '这个想法不错，我们具体聊聊怎么落地？',
    '我明白了。需要我帮你查一下相关资料吗？',
    '好的，稍等片刻，我正在处理中～',
    '有意思，继续说说你的看法？',
    '没问题，这个交给我吧！',
    '嗯嗯，我在听，请继续说。'
  ];

  /** 获取当前时间 HH:MM */
  function nowTime() {
    const d = new Date();
    const pad = (n) => String(n).padStart(2, '0');
    return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  /**
   * 追加一条消息
   * @param {'user'|'bot'} who 发送者
   * @param {string} text  消息文本
   * @param {boolean} isTyping 是否仅为"正在输入"占位（bot 专用）
   * @returns {HTMLElement} 消息元素
   */
  function appendMessage(who, text, isTyping = false) {
    const msg = document.createElement('div');
    msg.className = `message ${who}`;

    const avatar = document.createElement('div');
    avatar.className = 'avatar';
    avatar.textContent = who === 'user' ? '我' : '机';

    const bubble = document.createElement('div');
    bubble.className = 'bubble';
    if (isTyping) {
      bubble.id = 'typing';
      bubble.textContent = '正在输入...';
    } else {
      bubble.textContent = text;
    }

    const time = document.createElement('div');
    time.className = 'time';
    time.textContent = nowTime();

    msg.append(avatar, bubble, time);
    messagesEl.appendChild(msg);
    return msg;
  }

  /** 滚动到底部 */
  function scrollToBottom() {
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  /** 从回复池随机取一条 */
  function pickReply() {
    return REPLIES[Math.floor(Math.random() * REPLIES.length)];
  }

  /**
   * 发送用户消息并触发 bot 回复
   * @param {string} text 已 trim 的消息文本
   */
  function send(text) {
    if (!text) return;
    appendMessage('user', text);
    inputEl.value = '';
    scrollToBottom();

    // bot 先显示"正在输入..."占位，延迟 800-1500ms 后替换为正式回复
    const typingMsg = appendMessage('bot', '', true);
    scrollToBottom();

    const delay = 800 + Math.random() * 700; // 800 ~ 1500ms
    setTimeout(() => {
      const reply = pickReply();
      const bubble = typingMsg.querySelector('.bubble');
      bubble.id = '';
      bubble.textContent = reply;
      scrollToBottom();
    }, delay);
  }

  /** 简单防抖：500ms 内只响应最后一次 */
  function debounce(fn, wait = 500) {
    let timer = null;
    return (...args) => {
      clearTimeout(timer);
      timer = setTimeout(() => fn(...args), wait);
    };
  }

  // 发送逻辑（防抖，避免连点重复发送）
  const debouncedSend = debounce(send);

  // 点击发送按钮
  sendBtn.addEventListener('click', () => {
    const text = inputEl.value.trim();
    if (text) debouncedSend(text);
  });

  // Enter 发送，Shift+Enter 换行
  inputEl.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      const text = inputEl.value.trim();
      if (text) debouncedSend(text);
    }
  });

  // 加载时自动追加欢迎消息
  appendMessage('bot', '你好！我是小机，有什么可以帮你的吗？');
  scrollToBottom();

  // 输入框初始聚焦
  inputEl.focus();
})();
