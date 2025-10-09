// Immediately-invoked module to avoid leaking variables to global scope
(() => {
    // XSS protection but only needed when we use innerHTML.
    // DOMPurify.sanitize(...) removes dangerous tags/attributes (e.g. <script>, onerror=, javascript: URLs)
    // so we can safely insert user-controlled text into an HTML template.
    const sanitizeForInnerHTML = (value) => DOMPurify.sanitize(String(value));
    // Grab auth + UI state that Razor put on the page
    const root = document.getElementById('root');
    const joined = (root?.dataset.joined || '').toLowerCase() === 'true';
    const username = root?.dataset.username || '';

    // Cache DOM elements we’ll use
    const messages = document.getElementById('messagesList');
    const input = document.getElementById('messageInput');
    const sendBtn = document.getElementById('sendBtn');
    const status = document.getElementById('status');

    // Disable input until SignalR has connected
    input.disabled = true;
    sendBtn.disabled = true;

    // If the user isn’t authenticated, don’t start SignalR
    if (!joined || username.length < 3) {
        console.log('Not authenticated; waiting.');
        return;
    }

    // Ask the server (cookie-protected) to mint a short-lived JWT we can use for SignalR/API
    async function getJwt() {
        try {
            const res = await fetch('/api/auth/token', {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            const { accessToken } = await res.json();
            return accessToken;
        } catch (err) {
            console.warn('JWT fetch failed, falling back to cookie auth', err);
            return null;
        }
    }

    // Bootstrap SignalR using JWT if available; otherwise fall back to cookie auth
    (async () => {
        const token = await getJwt();

        // TEMP: debug — show the raw token (remove in production)
        console.log('JWT:', token);

        // Build the SignalR connection, passing the JWT via accessTokenFactory when we have one
        const builder = new signalR.HubConnectionBuilder();
        const configured = token
            ? builder.withUrl('/chathub', { accessTokenFactory: () => token })
            : builder.withUrl('/chathub');

        // Turn on automatic reconnects and create the connection instance
        const conn = builder.withAutomaticReconnect().build();

        conn.on('ReceiveMessage', (user, text) => {
            const li = document.createElement('li');
            li.className = 'list-group-item';
            li.textContent = `${user}: ${text}`;
            messages.appendChild(li);
            messages.scrollTop = messages.scrollHeight;
        });

        // When a message arrives, render it in the list
        conn.on('SystemNotice', (roomId, text) => {
            const li = document.createElement('li');
            li.className = 'list-group-item text-muted fst-italic';
            li.textContent = text; // safe (no HTML)
            messages.appendChild(li);
            messages.scrollTop = messages.scrollHeight;
        });

        // Show reconnecting/connected/closed status and lock inputs appropriately
        conn.onreconnecting(() => {
            // Static message so no sanitization needed
            status.innerHTML = `<div class="alert alert-warning py-2">Reconnecting…</div>`;
            sendBtn.disabled = true;
            input.disabled = true;
        });

        conn.onreconnected(() => {
            // DOMPurify: sanitize dynamic username before inserting via innerHTML
            const safeUser = sanitizeForInnerHTML(username);
            status.innerHTML = `<div class="alert alert-success py-2">Connected as <strong>${safeUser}</strong></div>`;
            sendBtn.disabled = false;
            input.disabled = false;
        });

        conn.onclose(err => {
            // DOMPurify: err could be anything; sanitize before innerHTML
            const safeErr = err ? `: ${sanitizeForInnerHTML(err)}` : '';
            status.innerHTML = `<div class="alert alert-danger py-2">Connection closed${safeErr}</div>`;
            sendBtn.disabled = true;
            input.disabled = true;
        });

        // Start the connection, then enable inputs
        try {
            await conn.start();
            input.disabled = false;
            sendBtn.disabled = false;

            // DOMPurify: sanitize dynamic username before innerHTML
            const safeUser = sanitizeForInnerHTML(username);
            status.innerHTML = `<div class="alert alert-success py-2">Connected as <strong>${safeUser}</strong></div>`;
        } catch (err) {
            // DOMPurify: sanitize error text before innerHTML
            status.innerHTML = `<div class="alert alert-danger">Connection failed: ${sanitizeForInnerHTML(err)}</div>`;
            console.error('SignalR connect error', err);
            return;
        }

        // Send a chat message to the hub
        sendBtn.addEventListener('click', async () => {
            const text = input.value.trim();
            if (!text) return;

            input.value = '';
            try {
                await conn.invoke('SendMessage', text);
            } catch (err) {
                // DOMPurify: sanitize error text before innerHTML
                status.innerHTML = `<div class="alert alert-warning">Send failed: ${sanitizeForInnerHTML(err)}</div>`;
                console.error('Send failed', err);
            }
        });

        // Allow pressing Enter to send
        input.addEventListener('keydown', e => {
            if (e.key === 'Enter') { sendBtn.click(); e.preventDefault(); }
        });
    })();
})();
