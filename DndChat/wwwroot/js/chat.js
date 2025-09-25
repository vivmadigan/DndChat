(function () {
    const root = document.getElementById('root');
    const joined = (root?.dataset.joined || '').toLowerCase() === 'true';
    const username = root?.dataset.username || '';
    console.log('joined?', joined, 'username:', username, 'origin:', window.location.origin);

    const messages = document.getElementById('messagesList');
    const input = document.getElementById('messageInput');
    const sendBtn = document.getElementById('sendBtn');
    const status = document.getElementById('status');

    if (!joined || username.length < 3) {
        console.log('Not joined yet—JS will wait.');
        return; // chat not active yet
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

    conn.start().then(() => {
        input.disabled = false;
        sendBtn.disabled = false;
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
            await conn.invoke('SendMessage', username, text);
        } catch (err) {
            status.innerHTML = `<div class="alert alert-warning">Send failed: ${err}</div>`;
            console.error('Send failed', err);
        }
    });

    input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') { sendBtn.click(); e.preventDefault(); }
    });
})();
