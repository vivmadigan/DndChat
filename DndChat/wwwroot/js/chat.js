// Immediately-invoked module so our variables don’t leak into window.*
(() => {
  // DOMPurify is used only when we set innerHTML (status banners).
  const sanitizeForInnerHTML = (value) => DOMPurify.sanitize(String(value));

    // Pull auth + UI state that Razor wrote into #root.
    const root = document.getElementById('root');
    const joined = (root?.dataset.joined || '').toLowerCase() === 'true';
    const username = root?.dataset.username || '';

    // Cache the main UI elements we update frequently.
    const messages = document.getElementById('messagesList');
    const input = document.getElementById('messageInput');
    const sendBtn = document.getElementById('sendBtn');
    const status = document.getElementById('status');

    // Cache the new private-room toolbar elements.
    const createPrivateBtn = document.getElementById('createPrivateBtn');
    const joinPrivateBtn   = document.getElementById('joinPrivateBtn');
    const roomIdInput      = document.getElementById('roomIdInput');
    const roomBadge        = document.getElementById('roomBadge');

    // Tracks which private room is active. When null, you’re in Global.
    let currentRoomId = null;

    // Lock inputs until the SignalR connection is up.
    input.disabled = true;
    sendBtn.disabled = true;

    // If the user isn’t authenticated, don’t even try to connect.
    if (!joined || username.length < 3) {
        console.log('Not authenticated; waiting.');
    return;
  }

    // Cookie-protected call that mints a short-lived JWT for SignalR.
    async function getJwt() {
    try {
      const res = await fetch('/api/auth/token', {
        method: 'POST',
        headers: {'X-Requested-With': 'XMLHttpRequest' }
      });
    if (!res.ok) throw new Error('HTTP ' + res.status);
    const {accessToken} = await res.json();
    return accessToken;
    } catch (err) {
        console.warn('JWT fetch failed, falling back to cookie auth', err);
        return null; // cookie auth will still work
    }
  }

  // Bootstraps SignalR. If we have a JWT, pass it; otherwise use cookies.
  (async () => {
    const token = await getJwt();

    // Build the connection with the right URL/options.
    const builder = new signalR.HubConnectionBuilder();
    const configured = token
    ? builder.withUrl('/chathub', {accessTokenFactory: () => token })
    : builder.withUrl('/chathub');

    // IMPORTANT: build from `configured` so we keep the URL + token.
    const conn = configured.withAutomaticReconnect().build();

    // Render normal global chat lines.
    conn.on('ReceiveMessage', (user, text) => {
      const li = document.createElement('li');
    li.className = 'list-group-item';
    li.textContent = `${user}: ${text}`;
    messages.appendChild(li);
    messages.scrollTop = messages.scrollHeight;
    });

    // Render system notices (used by both global and private rooms).
    conn.on('SystemNotice', (roomId, text) => {
      const li = document.createElement('li');
    li.className = 'list-group-item text-muted fst-italic';
    li.textContent = text;
    messages.appendChild(li);
    messages.scrollTop = messages.scrollHeight;
    });

    // Render messages for the currently active private room only.
    conn.on('ReceiveRoomMessage', (roomId, userName, text) => {
      if (currentRoomId && roomId === currentRoomId) {
        const li = document.createElement('li');
    li.className = 'list-group-item';
    li.textContent = `${userName}: ${text}`;
    messages.appendChild(li);
    messages.scrollTop = messages.scrollHeight;
      }
    });

    // Load recent history after joining a private room so it’s not empty.
    conn.on('LoadHistory', (roomId, items) => {
      // If you prefer a clean view per room, uncomment:
      // messages.innerHTML = '';
      for (const item of items) {
        const li = document.createElement('li');
    li.className = 'list-group-item';
    li.textContent = `${item.UserName}: ${item.Text}`;
    messages.appendChild(li);
      }
    messages.scrollTop = messages.scrollHeight;
    });

    // Connection status UI.
    conn.onreconnecting(() => {
        status.innerHTML = `<div class="alert alert-warning py-2">Reconnecting…</div>`;
        sendBtn.disabled = true; input.disabled = true;
    });

    conn.onreconnected(async () => {
        const safeUser = sanitizeForInnerHTML(username);
        status.innerHTML = `<div class="alert alert-success py-2">Connected as <strong>${safeUser}</strong></div>`;
        sendBtn.disabled = false; input.disabled = false;

        // If you are currently in a private room, re-join that only.
        // Otherwise, re-join the global group.
        if (currentRoomId) {
            await conn.invoke('JoinByCode', currentRoomId); // joinCode == roomId in MVP
        } else {
            await conn.invoke('JoinGlobal');
        }
    });

    conn.onclose(err => {
        const safeErr = err ? `: ${sanitizeForInnerHTML(err)}` : '';
        status.innerHTML = `<div class="alert alert-danger py-2">Connection closed${safeErr}</div>`;
        sendBtn.disabled = true; input.disabled = true;
        });

        // Start the connection and unlock inputs.
        try {
            await conn.start();
        input.disabled = false; sendBtn.disabled = false;
        const safeUser = sanitizeForInnerHTML(username);
            status.innerHTML = `<div class="alert alert-success py-2">Connected as <strong>${safeUser}</strong></div>`;

            // Explicitly join the global SignalR group on initial connect
            await conn.invoke('JoinGlobal');

        } catch (err) {
            status.innerHTML = `<div class="alert alert-danger">Connection failed: ${sanitizeForInnerHTML(err)}</div>`;
        console.error('SignalR connect error', err);
        return;
        }

      // Create a private room, join it, and show the code for sharing.
      createPrivateBtn?.addEventListener('click', async () => {
          try {
              // Optional: leave global so this window only shows the private room stream
              await conn.invoke('LeaveGlobal'); // NEW (optional)

              const result = await conn.invoke('CreatePrivateRoom', null);
              currentRoomId = result.roomId;       // joinCode == roomId in MVP
              roomBadge.textContent = `Private room: ${currentRoomId}`;
              roomIdInput.value = currentRoomId;
              // messages.innerHTML = ''; // optional clear
          } catch (err) {
              status.innerHTML = `<div class="alert alert-warning">Create failed: ${sanitizeForInnerHTML(err)}</div>`;
              console.error(err);
          }
      });

      // Join an existing private room by its code (which is the room id).
      joinPrivateBtn?.addEventListener('click', async () => {
          const code = roomIdInput.value.trim();
          if (!code) return;
          try {
              // Optional: leave global to avoid mixed streams in this window
              await conn.invoke('LeaveGlobal'); // NEW (optional)

              await conn.invoke('JoinByCode', code);
              currentRoomId = code;
              roomBadge.textContent = `Private room: ${code}`;
              // messages.innerHTML = ''; // optional clear
          } catch (err) {
              status.innerHTML = `<div class="alert alert-warning">Join failed: ${sanitizeForInnerHTML(err)}</div>`;
              console.error(err);
          }
      });

    // Route outgoing messages: to the active private room or to Global if none.
    sendBtn.addEventListener('click', async () => {
      const text = input.value.trim();
    if (!text) return;

    input.value = '';
    try {
        if (currentRoomId) {
        await conn.invoke('SendRoomMessage', currentRoomId, text);
        } else {
        await conn.invoke('SendMessage', text);
        }
      } catch (err) {
        status.innerHTML = `<div class="alert alert-warning">Send failed: ${sanitizeForInnerHTML(err)}</div>`;
    console.error('Send failed', err);
      }
    });

    // Allow Enter to send.
    input.addEventListener('keydown', e => {
      if (e.key === 'Enter') {sendBtn.click(); e.preventDefault(); }
    });
  })();
})();
