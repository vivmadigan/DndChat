// chat.js
(() => {
    const root = document.getElementById('root');
    const joined = (root?.dataset.joined || '').toLowerCase() === 'true';
    const username = root?.dataset.username || '';

    const messages = document.getElementById('messagesList');
    const input = document.getElementById('messageInput');
    const sendBtn = document.getElementById('sendBtn');
    const status = document.getElementById('status');

    // prevent early clicks until connected
    input.disabled = true;
    sendBtn.disabled = true;

    if (!joined || username.length < 3) {
        console.log('Not authenticated; waiting.');
        return;
    }

    const conn = new signalR.HubConnectionBuilder()
        .withUrl('/chathub')
        .withAutomaticReconnect()
        .build();

    conn.on('ReceiveMessage', (u, m) => {
        const li = document.createElement('li');
        li.className = 'list-group-item';
        li.textContent = `${u}: ${m}`;
        messages.appendChild(li);
        messages.scrollTop = messages.scrollHeight;
    });

    // Reconnect UX
    conn.onreconnecting(() => {
        status.innerHTML = `<div class="alert alert-warning py-2">Reconnecting…</div>`;
        sendBtn.disabled = true; input.disabled = true;
    });
    conn.onreconnected(() => {
        status.innerHTML = `<div class="alert alert-success py-2">Connected as <strong>${username}</strong></div>`;
        sendBtn.disabled = false; input.disabled = false;
    });
    conn.onclose(err => {
        status.innerHTML = `<div class="alert alert-danger py-2">Connection closed${err ? ': ' + err : ''}</div>`;
        sendBtn.disabled = true; input.disabled = true;
    });

    conn.start().then(() => {
        input.disabled = false; sendBtn.disabled = false;
        status.innerHTML = `<div class="alert alert-success py-2">Connected as <strong>${username}</strong></div>`;
        console.log('SignalR connected');
    }).catch(err => {
        status.innerHTML = `<div class="alert alert-danger">Connection failed: ${err}</div>`;
        console.error('SignalR connect error', err);
    });

    sendBtn.addEventListener('click', async () => {
        const text = input.value.trim();
        if (!text) return;
        input.value = '';
        try {
            await conn.invoke('SendMessage', text); // one arg ✔
        } catch (err) {
            status.innerHTML = `<div class="alert alert-warning">Send failed: ${err}</div>`;
            console.error('Send failed', err);
        }
    });

    // Enter to send
    input.addEventListener('keydown', e => {
        if (e.key === 'Enter') { sendBtn.click(); e.preventDefault(); }
    });
})();
