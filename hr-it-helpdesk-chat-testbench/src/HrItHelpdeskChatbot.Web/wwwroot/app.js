const domainLabels = {
  hr: "People & HR",
  it: "IT services",
  helpdesk: "Helpdesk"
};

const state = {
  domain: "hr",
  conversationId: null,
  scenarios: [],
  provider: "Mock",
  foundryConfigured: false
};

const chatLog = document.querySelector("#chat-log");
const messageInput = document.querySelector("#message-input");
const chatError = document.querySelector("#chat-error");
const sendButton = document.querySelector("#send-button");
const runButton = document.querySelector("#run-evaluations");

function addMessage(role, content, metadata = "") {
  document.querySelector("#empty-state")?.remove();
  const message = document.createElement("article");
  message.className = `message message-${role}`;
  const meta = document.createElement("div");
  meta.className = "message-meta";
  meta.textContent = role === "user" ? "TEST INPUT" : `ASSISTANT${metadata ? ` / ${metadata}` : ""}`;
  const bubble = document.createElement("div");
  bubble.className = "message-bubble";
  bubble.textContent = content;
  message.append(meta, bubble);
  chatLog.append(message);
  chatLog.scrollTop = chatLog.scrollHeight;
}

function resetChat() {
  state.conversationId = null;
  chatLog.replaceChildren();
  const emptyState = document.createElement("div");
  emptyState.className = "empty-state";
  emptyState.id = "empty-state";
  const mark = document.createElement("span");
  mark.className = "empty-mark";
  mark.textContent = "01";
  const heading = document.createElement("h3");
  heading.textContent = "Start with a test prompt";
  const description = document.createElement("p");
  description.textContent = "Choose a case file or type a fictional scenario below.";
  emptyState.append(mark, heading, description);
  chatLog.append(emptyState);
  chatError.hidden = true;
  messageInput.value = "";
}

function setDomain(domain) {
  state.domain = domain;
  document.querySelectorAll(".domain-button").forEach((button) => {
    const active = button.dataset.domain === domain;
    button.classList.toggle("is-active", active);
    button.setAttribute("aria-pressed", String(active));
  });
  document.querySelector("#active-domain-label").textContent = `${domainLabels[domain].toUpperCase()} / SANDBOX`;
  document.querySelectorAll(".scenario-item").forEach((item) => {
    item.hidden = item.dataset.domain !== domain;
  });
  resetChat();
}

function renderScenarios(scenarios) {
  const list = document.querySelector("#scenario-list");
  list.replaceChildren();
  document.querySelector("#scenario-count").textContent = String(scenarios.length).padStart(2, "0");
  for (const scenario of scenarios) {
    const button = document.createElement("button");
    button.className = "scenario-item";
    button.type = "button";
    button.dataset.domain = scenario.domain;
    const title = document.createElement("strong");
    title.textContent = scenario.title;
    const id = document.createElement("span");
    id.textContent = scenario.id;
    button.append(title, id);
    button.addEventListener("click", () => {
      if (state.domain !== scenario.domain) {
        setDomain(scenario.domain);
      }
      messageInput.value = scenario.prompt;
      messageInput.focus();
    });
    list.append(button);
  }
  setDomain(state.domain);
}

async function loadConfiguration() {
  const response = await fetch("/api/config");
  if (!response.ok) {
    throw new Error("Could not load provider configuration.");
  }
  const configuration = await response.json();
  state.provider = configuration.provider;
  state.foundryConfigured = configuration.foundryConfigured;
  const providerLabel = state.provider === "Mock" ? "MOCK / DETERMINISTIC" : "FOUNDRY / LIVE MODEL";
  document.querySelector("#provider-label").textContent = providerLabel;
  document.querySelector("#footer-provider").textContent = providerLabel;
  document.querySelector(".provider-state").classList.add("is-ready");
  const warning = document.querySelector("#provider-warning");
  if (state.provider === "Foundry" && !state.foundryConfigured) {
    warning.textContent = "Foundry mode is selected, but the endpoint or deployment name is missing.";
    warning.hidden = false;
  }
}

async function loadScenarios() {
  const response = await fetch("/api/scenarios");
  if (!response.ok) {
    throw new Error("Could not load evaluation scenarios.");
  }
  renderScenarios(await response.json());
}

document.querySelectorAll(".domain-button").forEach((button) => {
  button.setAttribute("aria-pressed", String(button.dataset.domain === state.domain));
  button.addEventListener("click", () => setDomain(button.dataset.domain));
});

document.querySelector("#new-conversation").addEventListener("click", resetChat);

document.querySelector("#chat-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const message = messageInput.value.trim();
  if (!message) {
    return;
  }
  chatError.hidden = true;
  addMessage("user", message);
  messageInput.value = "";
  sendButton.disabled = true;
  try {
    const response = await fetch("/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ domain: state.domain, message, conversationId: state.conversationId })
    });
    const payload = await response.json();
    if (!response.ok) {
      throw new Error(payload.error ?? payload.detail ?? "Chat request failed.");
    }
    state.conversationId = payload.conversationId;
    addMessage("assistant", payload.reply, payload.provider.toUpperCase());
  } catch (error) {
    chatError.textContent = error.message;
    chatError.hidden = false;
  } finally {
    sendButton.disabled = false;
    messageInput.focus();
  }
});

runButton.addEventListener("click", async () => {
  runButton.disabled = true;
  runButton.textContent = "Running cases...";
  document.querySelector("#provider-warning").hidden = true;
  try {
    const response = await fetch("/api/evaluations/run", { method: "POST" });
    const payload = await response.json();
    if (!response.ok) {
      throw new Error(payload.error ?? payload.detail ?? "Evaluation failed.");
    }
    renderEvaluation(payload);
  } catch (error) {
    const warning = document.querySelector("#provider-warning");
    warning.textContent = error.message;
    warning.hidden = false;
  } finally {
    runButton.disabled = false;
    runButton.innerHTML = '<span aria-hidden="true">&#9654;</span> Run evaluation suite';
  }
});

function renderEvaluation(result) {
  const rate = result.total === 0 ? 0 : Math.round((result.passed / result.total) * 100);
  document.querySelector("#pass-rate").textContent = `${rate}%`;
  document.querySelector("#passed-count").textContent = String(result.passed);
  document.querySelector("#failed-count").textContent = String(result.failed);
  document.querySelector("#pass-bar").style.width = `${rate}%`;

  const detail = document.querySelector("#run-detail");
  detail.replaceChildren();
  const summary = document.createElement("p");
  summary.textContent = `${result.total} synthetic cases / ${result.provider} provider`;
  const completed = document.createElement("p");
  completed.className = "muted";
  completed.textContent = `Completed ${new Date(result.startedAt).toLocaleString()}.`;
  detail.append(summary, completed);

  const cases = document.querySelector("#run-cases");
  cases.replaceChildren();
  for (const item of result.cases) {
    const row = document.createElement("div");
    row.className = "run-case";
    const label = document.createElement("div");
    const title = document.createElement("strong");
    title.textContent = item.title;
    const domain = document.createElement("span");
    domain.className = "case-domain";
    domain.textContent = item.domain;
    label.append(title, domain);
    const status = document.createElement("span");
    status.className = `case-result ${item.passed ? "is-pass" : "is-fail"}`;
    status.textContent = item.passed ? "PASS" : "FAIL";
    row.append(label, status);
    cases.append(row);
  }

  const resultsPath = document.querySelector("#results-path");
  resultsPath.textContent = `Saved locally: ${result.resultsFile}`;
  resultsPath.hidden = false;
}

Promise.all([loadConfiguration(), loadScenarios()]).catch((error) => {
  const warning = document.querySelector("#provider-warning");
  warning.textContent = error.message;
  warning.hidden = false;
});