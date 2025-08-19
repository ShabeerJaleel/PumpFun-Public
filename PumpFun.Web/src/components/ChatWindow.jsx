import React, { useState, useRef, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { 
    MainContainer,
    ChatContainer,
    MessageList,
    Message,
    MessageGroup,
    MessageInput,
    ConversationHeader,
    Button
} from '@chatscope/chat-ui-kit-react';
import { BsTrash, BsBellFill, BsBellSlashFill } from 'react-icons/bs'; // Add BsBellFill and BsBellSlashFill
import { addUrlLinks } from '../utils/helpers';

// Option 1: Remove the styles import entirely since it's included with the component library
import styles from '@chatscope/chat-ui-kit-styles/dist/default/styles.min.css';
import style from '../styles/ChatWindow.module.css';

const notificationAudio = new Audio('/notification.mp3');
notificationAudio.preload = 'auto';

const MAX_MESSAGES = 100;

const extractTweetId = (url) => {
    const match = url.match(/(?:twitter\.com|x\.com)\/\w+\/status\/(\d+)/);
    return match ? match[1] : null;
};

const formatMessageWithGroupName = (message) => {
    let content = '';
    
    // Show full-size images if present
    if (message.images && message.images.length > 0) {
        content += '<div style="display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 8px;">';
        content += message.images.map(img => 
            `<div style="max-width: 100%;">
                <img src="${img}" style="max-width: 100%; height: auto; border-radius: 8px;" />
            </div>`
        ).join('');
        content += '</div>';
    }

    const messageText = message.formattedMessage || message.message;
    
    // Check for Twitter/X.com links
    const words = messageText.split(' ');
    const formattedWords = words.map(word => {
        if (word.includes('twitter.com') || word.includes('x.com')) {
            const tweetId = extractTweetId(word);
            if (tweetId) {
                return `<iframe 
                    style="border: none; width: 100%; max-width: 550px; height: 300px;" 
                    src="https://platform.twitter.com/embed/Tweet.html?id=${tweetId}">
                </iframe>`;
            }
        }
        return word;
    });

    const formattedText = formattedWords.join(' ');
    
    if (!message.groupName) {
        content += addUrlLinks(formattedText);
    } else {
        content += `<span style="color: #2196F3; font-weight: bold;">${message.groupName}</span>: ${addUrlLinks(formattedText)}`;
    }
    
    return content;
};

const formatTime = (date) => {
    return new Date(date).toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
    });
};

export function ChatWindow({ messages, setMessages }) {
    const [disableInput] = useState(true);
    const [isMuted, setIsMuted] = useState(() => {
        const saved = localStorage.getItem('chatMuted');
        return saved ? JSON.parse(saved) : false;
    });
    const hubConnection = useRef(null);
    const lastMessageId = useRef(0);

    // Remove local messages state and use props instead
    const clearMessages = () => {
        setMessages([]);
    };

    // Update SignalR setup to use setMessages from props
    useEffect(() => {
        hubConnection.current = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/telegram")
            .withAutomaticReconnect()
            .build();

        hubConnection.current.on("ReceiveTelegramMessage", (message) => {
            console.log("Received message:", message);
            
            if (!isMuted) {
                notificationAudio.play().catch(err => console.log('Audio play failed:', err));
            }

            const timestamp = new Date().toISOString();
            setMessages(prev => {
                const existingIndex = prev.findIndex(msg => msg.telegramId === message.messageId);
                
                if (existingIndex !== -1) {
                    const updatedMessages = [...prev];
                    updatedMessages[existingIndex] = {
                        ...updatedMessages[existingIndex],
                        telegramId: message.messageId,
                        type: "html",
                        direction: "incoming",
                        position: "single",
                        payload: `${formatMessageWithGroupName(message)}
<div style="text-align: right; font-size: 0.75rem; color: #666; margin-top: 2px;">${formatTime(timestamp)} (edited)</div>`
                    };
                    return updatedMessages;
                } else {
                    lastMessageId.current += 1;
                    return [...prev.slice(-MAX_MESSAGES + 1), {
                        id: lastMessageId.current,
                        telegramId: message.messageId,
                        type: "html",
                        direction: "incoming",
                        position: "single",
                        payload: `${formatMessageWithGroupName(message)}
<div style="text-align: right; font-size: 0.75rem; color: #666; margin-top: 2px;">${formatTime(timestamp)}</div>`
                    }];
                }
            });
        });

        // Add connection state logging
        hubConnection.current.onclose(() => console.log("SignalR Disconnected"));
        hubConnection.current.onreconnecting(() => console.log("SignalR Reconnecting"));
        hubConnection.current.onreconnected(() => console.log("SignalR Reconnected"));

        hubConnection.current.start()
            .then(() => console.log("SignalR Connected"))
            .catch(err => console.error("Error starting SignalR:", err));

        return () => {
            if (hubConnection.current) {
                hubConnection.current.stop();
            }
        };
    }, [isMuted, setMessages]); // Add setMessages to dependencies

    return (
        <div style={{ position: 'relative', height: '100%' }}> {/* Add height: '100%' back */}
            <MainContainer style={{ height: '100%' }}> {/* Add style to MainContainer */}
                <ChatContainer>
                    <ConversationHeader style={{ padding: '4px 8px', minHeight: 'auto', display: 'flex', justifyContent: 'flex-end' }}>
                        <ConversationHeader.Actions>
                            <Button 
                                icon={isMuted ? <BsBellSlashFill /> : <BsBellFill />}
                                onClick={() => setIsMuted(!isMuted)}
                                style={{ padding: '4px', marginRight: '4px' }}
                                title={isMuted ? "Unmute notifications" : "Mute notifications"}
                            />
                            <Button 
                                icon={<BsTrash />} 
                                onClick={clearMessages}
                                style={{ padding: '4px' }}
                                title="Clear messages"
                            />
                        </ConversationHeader.Actions>
                    </ConversationHeader>
                    <MessageList>
                        {messages.map((msg, i) => (
                            <Message
                                key={msg.id || i}
                                model={msg} // Each message is already in the correct format
                                className={msg.id === lastMessageId.current ? 'message-flash' : ''}
                            />
                        ))}
                    </MessageList>
                    <MessageInput 
                        placeholder="Messages disabled"
                        disabled={disableInput}
                        attachButton={false}
                    />
                </ChatContainer>
            </MainContainer>
        </div>
    );
}
