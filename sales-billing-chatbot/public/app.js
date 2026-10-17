const chatMessages = document.getElementById('chatMessages');
const chatForm = document.getElementById('chatForm');
const userInput = document.getElementById('userInput');

function addMessage(text, sender) {
  const item = document.createElement('div');
  item.className = `message ${sender}`;

  const bubble = document.createElement('p');
  bubble.textContent = text;
  item.appendChild(bubble);
  chatMessages.appendChild(item);
  chatMessages.scrollTop = chatMessages.scrollHeight;
}

async function sendMessage(message) {
  addMessage(message, 'user');
  userInput.value = '';
  userInput.disabled = true;

  try {
    const response = await fetch('/api/chat', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ message })
    });

    if (!response.ok) {
      throw new Error('Request failed');
    }

    const data = await response.json();
    addMessage(data.reply, 'bot');
  } catch (error) {
    addMessage('I’m having trouble reaching the support service. Please try again in a moment.', 'bot');
  } finally {
    userInput.disabled = false;
    userInput.focus();
  }
}

chatForm.addEventListener('submit', (event) => {
  event.preventDefault();
  const message = userInput.value.trim();

  if (!message) {
    return;
  }

  sendMessage(message);
});
