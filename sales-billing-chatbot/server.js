const express = require('express');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;

const knowledgeBase = {
  sales: [
    {
      keywords: ['price', 'pricing', 'quote', 'cost', 'budget'],
      response:
        'For pricing, we offer flexible plans based on team size and usage. I can help compare Starter, Growth, and Enterprise options. Which plan are you considering?'
    },
    {
      keywords: ['demo', 'trial', 'book', 'call', 'consultation'],
      response:
        'You can schedule a live demo or request a trial from our sales team. We usually respond within one business day and can tailor the walkthrough to your use case.'
    },
    {
      keywords: ['feature', 'product', 'plan', 'package', 'upgrade'],
      response:
        'Our sales team can help map the right product package to your business needs. Popular upgrades include advanced analytics, premium automation, and additional user seats.'
    },
    {
      keywords: ['discount', 'offer', 'promo', 'special'],
      response:
        'We regularly offer volume discounts and seasonal promotions for new customers and multi-seat deals. I can help you understand which offer fits your size.'
    }
  ],
  billing: [
    {
      keywords: ['invoice', 'billing', 'billed', 'charge', 'charges'],
      response:
        'I can help with invoice questions. Please share the invoice number or the month in question, and I can explain the charges, due date, or any adjustments.'
    },
    {
      keywords: ['refund', 'reimbursement', 'credit', 'money back'],
      response:
        'Refund requests are reviewed by our billing team. If you believe a charge was incorrect, send the invoice ID and transaction date, and we can review the request promptly.'
    },
    {
      keywords: ['payment', 'card', 'credit card', 'debit card', 'subscription'],
      response:
        'Payment issues can be caused by a failed card authorization or an expired card on file. I can help you check your renewal date and review the latest payment status.'
    },
    {
      keywords: ['receipt', 'tax', 'taxes', 'receipt', 'balance'],
      response:
        'I can help with receipt or balance questions. If you need a duplicate invoice or want to verify taxes on a recent order, I can guide you to the right next step.'
    }
  ]
};

function classifyIntent(message) {
  const lower = message.toLowerCase();
  const salesKeywords = ['price', 'pricing', 'quote', 'cost', 'plan', 'package', 'demo', 'trial', 'discount', 'upgrade', 'sales', 'product'];
  const billingKeywords = ['invoice', 'billing', 'payment', 'refund', 'charge', 'card', 'subscription', 'receipt', 'tax', 'balance'];

  if (salesKeywords.some((word) => lower.includes(word))) {
    return 'sales';
  }

  if (billingKeywords.some((word) => lower.includes(word))) {
    return 'billing';
  }

  return 'general';
}

function getReplyForIntent(intent, message) {
  const lower = message.toLowerCase();

  if (intent === 'sales') {
    const match = knowledgeBase.sales.find((entry) =>
      entry.keywords.some((keyword) => lower.includes(keyword))
    );

    if (match) {
      return match.response;
    }

    return 'Our sales team can help with pricing, plans, demos, and enterprise packages. Tell me the size of your team or what product feature you need.';
  }

  if (intent === 'billing') {
    const match = knowledgeBase.billing.find((entry) =>
      entry.keywords.some((keyword) => lower.includes(keyword))
    );

    if (match) {
      return match.response;
    }

    return 'I can help with billing questions such as invoices, refunds, payment failures, and subscription renewals. What seems to be the issue?';
  }

  if (lower.includes('hi') || lower.includes('hello') || lower.includes('hey')) {
    return 'Hello! I can help with sales questions, pricing, and billing support. Ask me about plans, invoices, refunds, or payments.';
  }

  if (lower.includes('thank')) {
    return 'You are welcome. I can help further with pricing, renewals, invoices, or refund questions anytime.';
  }

  return 'I can help with both sales and billing topics. Try asking about pricing, plans, invoices, refunds, or payment issues.';
}

app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

app.get('/api/health', (req, res) => {
  res.json({ status: 'ok', message: 'Sales and billing chatbot is running.' });
});

app.post('/api/chat', (req, res) => {
  const message = typeof req.body?.message === 'string' ? req.body.message.trim() : '';

  if (!message) {
    return res.status(400).json({ error: 'Message is required.' });
  }

  const intent = classifyIntent(message);
  const reply = getReplyForIntent(intent, message);

  return res.json({
    intent,
    reply,
    message
  });
});

app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.listen(PORT, () => {
  console.log(`Sales and billing chatbot is running on http://localhost:${PORT}`);
});
