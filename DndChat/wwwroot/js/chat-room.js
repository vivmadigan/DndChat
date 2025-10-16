// Immediately-invoked module so variables don't leak to window.*
(() => {
    // Used only when writing innerHTML (status banners). Safe to omit if you already include it globally.
    const sanitizeForInnerHTML = (value) => DOMPurify.sanitize(String(value));

    // === Grab page data the controller put into data-* attributes ===
    // WHY: the controller passes room id/name/code to the view; JS reads them here.
    const root = document.getElementById('roomRoot');
    const roomId = root?.dataset.roomId || '';
    const roomName = root?.dataset.roomName || 'Private Room';
    const joinCode = root?.dataset.joinCode || '';

    // === Cache UI elements we update frequently ===
    // WHY: avoids repeated DOM lookups in handlers.
    const messages = document.getElementById('messagesList');
    const input = document.getElementById('messageInput');
    const sendBtn = document.getElementById('sendBtn');
    const status = document.getElementById('status');
    const copyBtn = document.getElementById('copyJoinCodeBtn');
    const leaveBtn = document.getElementById('leaveBtn');

    // === Disable input until SignalR is connected ===
    input.disabled = true;
    sendBtn.disabled = true;

    // === Helper to mint a short-lived JWT (falls back to cookie if not available) ===
    // WHY: matches your global page; enables WebSocket auth when needed.
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
            return null; // cookie auth still works
        }
    }

    // === Build and start the SignalR connection, then join THIS room ===
    (async () => {
        // Short-circuit: we need a room id to proceed.
        if (!roomId) {
            status.innerHTML = `<div class="alert alert-danger">Missing room id.</div>`;
            return;
        }

        const token = await getJwt();

        // Build the connection with/without JWT.
        const builder = new signalR.HubConnectionBuilder();
        const configured = token
            ? builder.withUrl('/chathub', { accessTokenFactory: () => token })
            : builder.withUrl('/chathub');

        // IMPORTANT: build from `configured`, not `builder`, so we keep URL+token.
        const conn = configured.withAutomaticReconnect().build();

        // === Handlers the hub will call back ===

        // WHY: load last N messages as soon as we join the room so the view isn't empty.
        conn.on('LoadHistory', (rid, items) => {
            if (rid !== roomId) return;
            // Optional: clear any stale lines before rendering history.
            // messages.innerHTML = '';
            for (const item of items) {
                const li = document.createElement('li');
                li.className = 'list-group-item';
                li.textContent = `${item.UserName}: ${item.Text}`;
                messages.appendChild(li);
            }
            messages.scrollTop = messages.scrollHeight;
        });

        // WHY: render live messages for this room only.
        conn.on('ReceiveRoomMessage', (rid, userName, text) => {
            if (rid !== roomId) return;
            const li = document.createElement('li');
            li.className = 'list-group-item';
            li.textContent = `${userName}: ${text}`;
            messages.appendChild(li);
            messages.scrollTop = messages.scrollHeight;
        });

        // WHY: show system activity lines (joined/left/created) for this room only.
        conn.on('SystemNotice', (rid, text) => {
            if (rid !== roomId) return;
            const li = document.createElement('li');
            li.className = 'list-group-item text-muted fst-italic';
            li.textContent = text;
            messages.appendChild(li);
            messages.scrollTop = messages.scrollHeight;
        });
        conn.on('RoomDeleted', (rid, reason) => {
            if (rid !== roomId) return;

            // safer: build the alert node and set textContent
            const alert = document.createElement('div');
            alert.className = 'alert alert-warning';
            alert.textContent = reason;
            status.replaceChildren(alert);

            input.disabled = true;
            sendBtn.disabled = true;

            setTimeout(() => { window.location.href = '/Rooms'; }, 1200);
        });

        // === Connection status UI ===
        conn.onreconnecting(() => {
            status.innerHTML = `<div class="alert alert-warning py-2">Reconnecting…</div>`;
            sendBtn.disabled = true; input.disabled = true;
        });

        conn.onreconnected(async () => {
            status.innerHTML = `<div class="alert alert-success py-2">Reconnected</div>`;
            sendBtn.disabled = false; input.disabled = false;

            // IMPORTANT: re-join THIS room after reconnect so we keep receiving events.
            await conn.invoke('JoinByCode', roomId);
        });

        conn.onclose(err => {
            const safeErr = err ? `: ${sanitizeForInnerHTML(err)}` : '';
            status.innerHTML = `<div class="alert alert-danger py-2">Connection closed${safeErr}</div>`;
            sendBtn.disabled = true; input.disabled = true;
        });

        // === Start and join THIS room ===
        try {
            await conn.start();
            input.disabled = false; sendBtn.disabled = false;
            status.innerHTML = `<div class="alert alert-success py-2">Connected to <strong>${sanitizeForInnerHTML(roomName)}</strong></div>`;

            // Tell the hub to add this connection to the SignalR group for the room.
            await conn.invoke('JoinByCode', roomId);
        } catch (err) {
            status.innerHTML = `<div class="alert alert-danger">Connect failed: ${sanitizeForInnerHTML(err)}</div>`;
            console.error('SignalR connect error', err);
            return;
        }

        // === Send message button ===
        sendBtn.addEventListener('click', async () => {
            const text = input.value.trim();
            if (!text) return;
            input.value = '';
            try {
                await conn.invoke('SendRoomMessage', roomId, text);
            } catch (err) {
                status.innerHTML = `<div class="alert alert-warning">Send failed: ${sanitizeForInnerHTML(err)}</div>`;
                console.error('Send failed', err);
            }
        });

        // === Enter to send ===
        input.addEventListener('keydown', e => {
            if (e.key === 'Enter') { sendBtn.click(); e.preventDefault(); }
        });

        // === Copy join code to clipboard ===
        copyBtn?.addEventListener('click', async () => {
            try {
                await navigator.clipboard.writeText(joinCode);
                status.innerHTML = `<div class="alert alert-info py-2">Join code copied</div>`;
            } catch {
                status.innerHTML = `<div class="alert alert-warning py-2">Couldn’t copy. Select and copy manually.</div>`;
            }
        });

        // === Leave button: tell the hub, then navigate back to /Rooms ===
        leaveBtn?.addEventListener('click', async () => {
            leaveBtn.disabled = true;
            try {
                await conn.invoke('LeaveRoom', roomId);
            } catch { /* ignore */ }
            window.location.href = '/Rooms';
        });
    })();
})();
