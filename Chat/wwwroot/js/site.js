document.addEventListener('DOMContentLoaded', function () {
    // Состояние приложения
    const state = {
        isConnecting: false,
        isSending: false,
        connectionAttempts: 0,
        MAX_RETRY_ATTEMPTS: 5,
        lastSentMessage: '',
        lastSentTime: 0,
        activeGroup: null
    };

    // Получаем элементы DOM
    const chatContainer = document.querySelector('.vk-style-chat');
    if (!chatContainer) {
        console.error("Chat container not found");
        return;
    }

    const currentUserId = chatContainer.dataset.currentUserId;
    const otherUserId = chatContainer.dataset.otherUserId;
    const messageInput = document.getElementById('messageInput');
    const sendButton = document.getElementById('sendButton');
    const messagesList = document.getElementById('messagesList');
    const userStatus = document.getElementById('userStatus');
    const typingIndicator = document.getElementById('typingIndicator');

    // Инициализация SignalR соединения
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub", {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .configureLogging(signalR.LogLevel.Warning)
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: retryContext => {
                if (retryContext.elapsedMilliseconds < 60000) {
                    return Math.random() * 2000 + 2000;
                }
                return null;
            }
        })
        .build();

    // Обработчики событий
    connection.on("ReceiveMessage", (message) => {
        console.log("Received message:", message);
        if (!isMessageExists(message.id)) {
            addMessage(message, message.senderId == currentUserId);
        }
    });

    connection.on("MessageSent", (message) => {
        console.log("Message sent confirmation:", message);
        updateMessageStatus(message.id);
    });

    connection.on("UserTyping", () => {
        console.log("User is typing...");
        showTypingIndicator(true);
    });

    connection.on("UserStatusChanged", (status) => {
        console.log("User status changed:", status);
        updateUserStatus(status);
    });

    // Функции для работы с UI
    function isMessageExists(messageId) {
        return !!document.querySelector(`[data-message-id="${messageId}"]`);
    }

    function addMessage(message, isOutgoing) {
        const messageElement = document.createElement('div');
        messageElement.className = `message ${isOutgoing ? 'out' : 'in'}`;
        messageElement.dataset.messageId = message.id;

        const timeString = new Date(message.sentTime).toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit'
        });

        messageElement.innerHTML = `
            <div class="message-content">${escapeHtml(message.content)}</div>
            <div class="message-time">
                ${timeString}
                ${isOutgoing ? '<span class="status-icon">✓</span>' : ''}
            </div>
        `;

        messagesList.appendChild(messageElement);
        messagesList.scrollTop = messagesList.scrollHeight;
    }

    function updateMessageStatus(messageId) {
        const messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
        if (messageElement) {
            const statusIcon = messageElement.querySelector('.status-icon');
            if (statusIcon) {
                statusIcon.classList.add('read');
                statusIcon.textContent = '✓✓';
            }
        }
    }

    function showTypingIndicator(show) {
        if (typingIndicator) {
            typingIndicator.style.display = show ? 'block' : 'none';
            if (show) {
                setTimeout(() => showTypingIndicator(false), 2000);
            }
        }
    }

    function updateUserStatus(status) {
        if (userStatus) {
            userStatus.innerHTML = status.isOnline
                ? '<span class="online">online</span>'
                : `<span>last seen ${new Date(status.lastOnline).toLocaleString()}</span>`;
        }
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Отправка сообщения
    async function sendMessage() {
        if (state.isSending || !connection.connectionStarted) return;

        const message = messageInput.value.trim();
        if (!message) return;

        // Защита от быстрой повторной отправки
        const now = Date.now();
        if (message === state.lastSentMessage && (now - state.lastSentTime) < 2000) {
            return;
        }

        state.lastSentMessage = message;
        state.lastSentTime = now;
        state.isSending = true;
        sendButton.disabled = true;

        try {
            console.log("Sending message to:", otherUserId);
            await connection.invoke("SendMessage", parseInt(otherUserId), message);
            messageInput.value = '';
            messageInput.focus();
        } catch (err) {
            console.error("Send failed:", err);
            alert("Failed to send message: " + err.message);
        } finally {
            sendButton.disabled = false;
            state.isSending = false;
        }
    }

    // Индикатор набора текста
    let typingTimer;
    messageInput.addEventListener('input', () => {
        clearTimeout(typingTimer);
        if (connection.state === signalR.HubConnectionState.Connected) {
            console.log("Sending typing notification");
            connection.invoke("SendTypingNotification", parseInt(otherUserId))
                .catch(err => console.error("Typing error:", err));
        }
        typingTimer = setTimeout(() => showTypingIndicator(false), 2000);
    });

    // Подключение к группе чата
    async function joinChatGroup() {
        try {
            console.log("Joining chat group for users:", currentUserId, otherUserId);
            await connection.invoke("JoinChatGroup", currentUserId, otherUserId);
            state.activeGroup = `chat_${currentUserId}_${otherUserId}`;
            console.log("Successfully joined chat group");
        } catch (err) {
            console.error("Error joining chat group:", err);
            setTimeout(joinChatGroup, 5000);
        }
    }

    // Запуск соединения
    async function startConnection() {
        if (state.isConnecting || connection.state !== signalR.HubConnectionState.Disconnected) {
            return;
        }

        state.isConnecting = true;
        state.connectionAttempts++;

        try {
            console.log("Attempting to connect to SignalR hub...");
            await connection.start();
            console.log("Successfully connected to chat hub");

            await joinChatGroup();

            state.connectionAttempts = 0;
        } catch (err) {
            console.error("Connection error:", err);

            if (state.connectionAttempts < state.MAX_RETRY_ATTEMPTS) {
                setTimeout(startConnection, 5000);
            } else {
                alert("Failed to connect to chat. Please refresh the page.");
            }
        } finally {
            state.isConnecting = false;
        }
    }

    // Обработка закрытия соединения
    connection.onclose(async (error) => {
        console.log("Connection closed. Error:", error);
        await startConnection();
    });

    // Назначение обработчиков
    sendButton.addEventListener('click', sendMessage);
    messageInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') sendMessage();
    });

    // Инициализация
    startConnection();

    // Периодическая проверка соединения
    setInterval(() => {
        if (connection.state === signalR.HubConnectionState.Connected) {
            console.log("Connection state: Connected");
        } else {
            console.log("Connection state:", connection.state);
        }
    }, 10000);
});