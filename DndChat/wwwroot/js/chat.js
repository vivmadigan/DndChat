// Wrap everything in an IIFE(Immediately Invoked Function Expression) so it runs immediately *when the script file loads*
// (your _Layout renders scripts at the end of <body>, so this runs after the DOM exists).
(function () {

    // Grab the root element and read two values Razor wrote into data-* attributes.
    // These come from your JoinViewModel on the server.
    const root = document.getElementById('root');
    const joined = (root?.dataset.joined || '').toLowerCase() === 'true';
    const username = root?.dataset.username || ''

    // Log details to the console to make sure information is correct        ;
    console.log('joined?', joined, 'username:', username, 'origin:', window.location.origin);

    // Cache references to UI elements we’ll read/update.
    const messages = document.getElementById('messagesList');
    const input = document.getElementById('messageInput');
    const sendBtn = document.getElementById('sendBtn');
    const status = document.getElementById('status');

    // If the user hasn’t joined (or username too short), we STOP here.
    // This prevents trying to connect to SignalR before the form is submitted.
    if (!joined || username.length < 3) {
        console.log('Not joined yet—JS will wait.');
        return; 
    }

    // Build a SignalR connection to your hub endpoint. Program.cs maps it with:
    //   app.MapHub<ChatHub>("/chathub");
    // AutomaticReconnect means it will try to reconnect if the connection drops.
    const conn = new signalR.HubConnectionBuilder()
        .withUrl('/chathub')
        .withAutomaticReconnect()
        .build();

    // Register a handler for server → client messages named "ReceiveMessage".
    // On the server, ChatHub calls:
    //   Clients.All.SendAsync("ReceiveMessage", user, message);
    // When that happens, this function runs and appends a list item to the UI.
    conn.on('ReceiveMessage', (u, m) => {
        const li = document.createElement('li');
        li.className = 'list-group-item';
        li.textContent = `${u}: ${m}`;
        messages.appendChild(li);
        messages.scrollTop = messages.scrollHeight;
    });

    // Start the connection. Under the hood:
    // 1) Browser POSTs /chathub/negotiate
    // 2) Chooses WebSockets/SSE/Long Polling
    // 3) Opens the live connection
    conn.start().then(() => {
        // Enable the input and button now that we’re connected.
        input.disabled = false;
        sendBtn.disabled = false;
        status.innerHTML = `<div class="alert alert-success py-2">Connected as <strong>${username}</strong></div>`;
        console.log('SignalR connected');
    }).catch(err => {
        status.innerHTML = `<div class="alert alert-danger">Connection failed: ${err}</div>`;
        console.error('SignalR connect error', err);
    });
    // When the user clicks “Chat”, send the message to the server by invoking the hub method.
    // This calls C# ChatHub.SendMessage(string user, string message).
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
    // Let Enter key send messages too (without submitting any form).
    input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') { sendBtn.click(); e.preventDefault(); }
    });
})();
