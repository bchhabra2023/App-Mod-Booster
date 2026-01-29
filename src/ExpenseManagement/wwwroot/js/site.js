// Expense Management System - JavaScript

// Show/Hide sections
function showSection(sectionId) {
    const sections = document.querySelectorAll('.section');
    sections.forEach(section => {
        section.classList.remove('active');
    });
    document.getElementById(sectionId).classList.add('active');
}

// Filter expenses in main table
function filterExpenses() {
    const input = document.getElementById('filterInput');
    const filter = input.value.toUpperCase();
    const table = document.getElementById('expensesTableBody');
    const tr = table.getElementsByTagName('tr');

    for (let i = 0; i < tr.length; i++) {
        const tds = tr[i].getElementsByTagName('td');
        let found = false;
        
        for (let j = 0; j < tds.length; j++) {
            const td = tds[j];
            if (td) {
                const txtValue = td.textContent || td.innerText;
                if (txtValue.toUpperCase().indexOf(filter) > -1) {
                    found = true;
                    break;
                }
            }
        }
        
        tr[i].style.display = found ? '' : 'none';
    }
}

// Filter by status
function filterByStatus() {
    const select = document.getElementById('statusFilter');
    const status = select.value.toUpperCase();
    const table = document.getElementById('expensesTableBody');
    const tr = table.getElementsByTagName('tr');

    for (let i = 0; i < tr.length; i++) {
        if (status === '') {
            tr[i].style.display = '';
        } else {
            const statusTd = tr[i].getElementsByTagName('td')[3];
            if (statusTd) {
                const txtValue = statusTd.textContent || statusTd.innerText;
                tr[i].style.display = txtValue.toUpperCase().indexOf(status) > -1 ? '' : 'none';
            }
        }
    }
}

// Filter approve expenses table
function filterApproveExpenses() {
    const input = document.getElementById('approveFilterInput');
    const filter = input.value.toUpperCase();
    const table = document.getElementById('approveTableBody');
    const tr = table.getElementsByTagName('tr');

    for (let i = 0; i < tr.length; i++) {
        const tds = tr[i].getElementsByTagName('td');
        let found = false;
        
        for (let j = 0; j < tds.length; j++) {
            const td = tds[j];
            if (td) {
                const txtValue = td.textContent || td.innerText;
                if (txtValue.toUpperCase().indexOf(filter) > -1) {
                    found = true;
                    break;
                }
            }
        }
        
        tr[i].style.display = found ? '' : 'none';
    }
}

// Add expense form submission
document.addEventListener('DOMContentLoaded', function() {
    const addExpenseForm = document.getElementById('addExpenseForm');
    if (addExpenseForm) {
        addExpenseForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const resultDiv = document.getElementById('addExpenseResult');
            resultDiv.className = 'result-message';
            resultDiv.textContent = 'Creating expense...';
            resultDiv.style.display = 'block';
            
            const expenseData = {
                userId: parseInt(document.getElementById('userId').value),
                categoryId: parseInt(document.getElementById('categoryId').value),
                amount: parseFloat(document.getElementById('amount').value),
                expenseDate: document.getElementById('expenseDate').value,
                description: document.getElementById('description').value,
                currency: 'GBP'
            };
            
            try {
                const response = await fetch('/api/expenses', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(expenseData)
                });
                
                if (response.ok) {
                    const result = await response.json();
                    resultDiv.className = 'result-message success';
                    resultDiv.textContent = `✓ Expense created successfully! ID: ${result.expenseId}`;
                    addExpenseForm.reset();
                    
                    // Refresh page after 2 seconds
                    setTimeout(() => {
                        window.location.reload();
                    }, 2000);
                } else {
                    const error = await response.json();
                    resultDiv.className = 'result-message error';
                    resultDiv.textContent = `✗ Error: ${error.error || 'Failed to create expense'}`;
                }
            } catch (error) {
                resultDiv.className = 'result-message error';
                resultDiv.textContent = `✗ Error: ${error.message}`;
            }
        });
    }
    
    // Set default date to today
    const dateInput = document.getElementById('expenseDate');
    if (dateInput) {
        dateInput.value = new Date().toISOString().split('T')[0];
    }
});

// Approve expense
async function approveExpense(expenseId) {
    if (!confirm('Are you sure you want to approve this expense?')) {
        return;
    }
    
    try {
        const response = await fetch(`/api/expenses/${expenseId}/approve`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(2) // Default reviewer ID
        });
        
        if (response.ok) {
            alert('✓ Expense approved successfully!');
            window.location.reload();
        } else {
            const error = await response.json();
            alert(`✗ Error: ${error.error || 'Failed to approve expense'}`);
        }
    } catch (error) {
        alert(`✗ Error: ${error.message}`);
    }
}

// Reject expense
async function rejectExpense(expenseId) {
    if (!confirm('Are you sure you want to reject this expense?')) {
        return;
    }
    
    try {
        const response = await fetch(`/api/expenses/${expenseId}/reject`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(2) // Default reviewer ID
        });
        
        if (response.ok) {
            alert('✓ Expense rejected successfully!');
            window.location.reload();
        } else {
            const error = await response.json();
            alert(`✗ Error: ${error.error || 'Failed to reject expense'}`);
        }
    } catch (error) {
        alert(`✗ Error: ${error.message}`);
    }
}

// Chat functionality
let chatHistory = [];

function handleChatKeyPress(event) {
    if (event.key === 'Enter') {
        sendChatMessage();
    }
}

async function sendChatMessage() {
    const input = document.getElementById('chat-input');
    const message = input.value.trim();
    
    if (!message) return;
    
    // Add user message to UI
    addChatMessage('user', message);
    input.value = '';
    
    // Add to history
    chatHistory.push({ role: 'user', content: message });
    
    // Show loading indicator
    const loadingDiv = document.createElement('div');
    loadingDiv.className = 'chat-message assistant';
    loadingDiv.id = 'loading-message';
    loadingDiv.innerHTML = '<div class="message-content"><div class="loading"></div> Thinking...</div>';
    document.getElementById('chat-messages').appendChild(loadingDiv);
    scrollChatToBottom();
    
    try {
        const response = await fetch('/api/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ messages: chatHistory })
        });
        
        // Remove loading indicator
        loadingDiv.remove();
        
        if (response.ok) {
            const result = await response.json();
            addChatMessage('assistant', result.message);
            chatHistory.push({ role: 'assistant', content: result.message });
        } else {
            const error = await response.json();
            addChatMessage('assistant', `Error: ${error.error || 'Failed to get response'}`);
        }
    } catch (error) {
        loadingDiv.remove();
        addChatMessage('assistant', `Error: ${error.message}`);
    }
}

function addChatMessage(role, content) {
    const messagesDiv = document.getElementById('chat-messages');
    const messageDiv = document.createElement('div');
    messageDiv.className = `chat-message ${role}`;
    
    const contentDiv = document.createElement('div');
    contentDiv.className = 'message-content';
    
    // Format the content
    const formattedContent = formatMessageContent(content);
    contentDiv.innerHTML = `<strong>${role === 'user' ? 'You' : 'Assistant'}:</strong><br>${formattedContent}`;
    
    messageDiv.appendChild(contentDiv);
    messagesDiv.appendChild(messageDiv);
    
    scrollChatToBottom();
}

function formatMessageContent(text) {
    // Escape HTML first to prevent XSS
    let escaped = text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
    
    // Then apply formatting
    let formatted = escaped
        // Bold text
        .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
        // Line breaks
        .replace(/\n/g, '<br>');
    
    // Handle numbered lists
    const lines = formatted.split('<br>');
    let inNumberedList = false;
    let inBulletList = false;
    let result = [];
    
    for (let line of lines) {
        const trimmed = line.trim();
        
        // Check for numbered list
        if (/^\d+\.\s/.test(trimmed)) {
            if (!inNumberedList) {
                if (inBulletList) {
                    result.push('</ul>');
                    inBulletList = false;
                }
                result.push('<ol>');
                inNumberedList = true;
            }
            result.push('<li>' + trimmed.replace(/^\d+\.\s/, '') + '</li>');
        }
        // Check for bullet list
        else if (/^[-*]\s/.test(trimmed)) {
            if (!inBulletList) {
                if (inNumberedList) {
                    result.push('</ol>');
                    inNumberedList = false;
                }
                result.push('<ul>');
                inBulletList = true;
            }
            result.push('<li>' + trimmed.replace(/^[-*]\s/, '') + '</li>');
        }
        // Regular line
        else {
            if (inNumberedList) {
                result.push('</ol>');
                inNumberedList = false;
            }
            if (inBulletList) {
                result.push('</ul>');
                inBulletList = false;
            }
            if (trimmed) {
                result.push(line);
            }
        }
    }
    
    // Close any open lists
    if (inNumberedList) result.push('</ol>');
    if (inBulletList) result.push('</ul>');
    
    return result.join('<br>');
}

function scrollChatToBottom() {
    const messagesDiv = document.getElementById('chat-messages');
    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}
