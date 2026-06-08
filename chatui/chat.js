const form = document.getElementById('chat-form');
const promptInput = document.getElementById('prompt');
const chatWindow = document.getElementById('chat-window');

const escapeHtml = (text) => text
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;')
  .replace(/'/g, '&#039;');

const formatText = (rawText) => {
  const safe = escapeHtml(rawText || '');
  const lines = safe.split('\n');

  const transformed = [];
  let inOl = false;
  let inUl = false;

  const closeLists = () => {
    if (inOl) transformed.push('</ol>');
    if (inUl) transformed.push('</ul>');
    inOl = false;
    inUl = false;
  };

  for (const line of lines) {
    const olMatch = line.match(/^\d+\.\s+(.*)$/);
    const ulMatch = line.match(/^[-*]\s+(.*)$/);

    if (olMatch) {
      if (inUl) {
        transformed.push('</ul>');
        inUl = false;
      }
      if (!inOl) {
        transformed.push('<ol>');
        inOl = true;
      }
      transformed.push(`<li>${olMatch[1]}</li>`);
      continue;
    }

    if (ulMatch) {
      if (inOl) {
        transformed.push('</ol>');
        inOl = false;
      }
      if (!inUl) {
        transformed.push('<ul>');
        inUl = true;
      }
      transformed.push(`<li>${ulMatch[1]}</li>`);
      continue;
    }

    closeLists();
    transformed.push(line.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>'));
  }

  closeLists();
  return transformed.join('<br>');
};

const addBubble = (text, type) => {
  const messageDiv = document.createElement('div');
  messageDiv.className = `bubble ${type}`;
  messageDiv.innerHTML = formatText(text);
  chatWindow.appendChild(messageDiv);
  chatWindow.scrollTop = chatWindow.scrollHeight;
};

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  const prompt = promptInput.value.trim();
  if (!prompt) return;

  addBubble(prompt, 'user');
  promptInput.value = '';

  const response = await fetch('/api/chat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt })
  });

  const payload = await response.json();
  addBubble(payload.response || 'No response returned.', 'bot');
});

addBubble('Hi! I can list expenses, create new expenses, and update statuses through backend APIs.', 'bot');
