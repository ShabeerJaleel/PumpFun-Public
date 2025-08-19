// ...existing code...
export function formatDate(date) {
    // Implement date formatting logic
    return new Date(date).toLocaleDateString();
}

// Add truncateDescription function
export function truncateDescription(description, length = 20) {
    if (!description) return '';
    return description.length > length ? 
        description.substring(0, length) + '...' : description;
}

// Add truncateAddress function
export function truncateAddress(address, length = 5) {
    return `${address.substring(0, length)}...`;
}

// Modify copyToClipboard function to return a promise
export function copyToClipboard(text) {
    return navigator.clipboard.writeText(text);
}

// Add appendSocialLinks function
export function appendSocialLinks(token, cell) {
    if (token.twitter) {
        const twitterLink = document.createElement('a');
        twitterLink.href = token.twitter;
        twitterLink.target = '_blank';
        twitterLink.innerHTML = '<i class="bi bi-twitter"></i>';
        twitterLink.classList.add('me-2');
        cell.appendChild(twitterLink);
    }
    if (token.telegram) {
        const telegramLink = document.createElement('a');
        telegramLink.href = token.telegram;
        telegramLink.target = '_blank';
        telegramLink.innerHTML = '<i class="bi bi-telegram"></i>';
        telegramLink.classList.add('me-2');
        cell.appendChild(telegramLink);
    }
    if (token.website) {
        const websiteLink = document.createElement('a');
        websiteLink.href = token.website;
        websiteLink.target = '_blank';
        websiteLink.innerHTML = '<i class="bi bi-globe"></i>';
        cell.appendChild(websiteLink);
    }
}

// Add formatElapsedTime function
export function formatElapsedTime(date) {
    const seconds = Math.floor((new Date() - new Date(date)) / 1000);
    
    const intervals = [
        { label: 'w', seconds: 604800 },
        { label: 'd', seconds: 86400 },
        { label: 'h', seconds: 3600 },
        { label: 'm', seconds: 60 },
        { label: 's', seconds: 1 }
    ];

    for (let i = 0; i < intervals.length; i++) {
        const interval = Math.floor(seconds / intervals[i].seconds);
        if (interval >= 1) {
            return `${interval}${intervals[i].label}`;
        }
    }
    return 'now';
}

// Add a generic truncateText function
export function truncateText(text, length) {
    if (!text) return '';
    return text.length > length ? 
        text.substring(0, length) + '...' : text;
}

// Add function to convert URLs to clickable links if not already in anchor tags
export const addUrlLinks = (text) => {
    // Create a temporary container
    const container = document.createElement('div');
    container.innerHTML = text;

    // Fix existing links first
    container.querySelectorAll('a').forEach(link => {
        link.setAttribute('target', '_blank');
        link.setAttribute('rel', 'noopener noreferrer');
    });

    // Convert remaining plain URLs to links
    const urlRegex = /(https?:\/\/[^\s<>"]+)/g;
    const walker = document.createTreeWalker(
        container,
        NodeFilter.SHOW_TEXT,
        null,
        false
    );

    const nodesToReplace = [];
    while (walker.nextNode()) {
        const node = walker.currentNode;
        if (node.parentElement && node.parentElement.nodeName === 'A') continue;

        let lastIndex = 0;
        let match;
        let fragment = document.createDocumentFragment();
        
        while ((match = urlRegex.exec(node.textContent)) !== null) {
            // Add text before the URL
            fragment.appendChild(document.createTextNode(
                node.textContent.substring(lastIndex, match.index)
            ));

            const url = match[0];
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('target', '_blank');
            link.setAttribute('rel', 'noopener noreferrer');
            link.textContent = url;

            fragment.appendChild(link);
            lastIndex = match.index + url.length;
        }

        if (lastIndex > 0) {
            // Add remaining text
            fragment.appendChild(document.createTextNode(
                node.textContent.substring(lastIndex)
            ));
            nodesToReplace.push({ node, fragment });
        }
    }

    // Replace text nodes with fragments
    nodesToReplace.forEach(({ node, fragment }) => {
        node.parentNode.replaceChild(fragment, node);
    });

    return container.innerHTML;
};

// ...existing code...

// Simplify fetchTokens to return the direct response data
export async function fetchTokens(params) {
    try {
        const response = await fetch(`/api/tokens?${params.toString()}`);
        if (!response.ok) {
            const errorText = await response.text();
            console.error('Fetch tokens error:', {
                status: response.status,
                statusText: response.statusText,
                response: errorText
            });
            throw new Error(`Network response was not ok: ${response.status} ${response.statusText} ${errorText}`);
        }
        return await response.json();
    } catch (error) {
        console.error('Error fetching tokens:', error);
        throw error;
    }
}

// Modify formatMarketCap to handle actual values and format as K/M/B
export function formatMarketCap(marketCap) {
    const numericMarketCap = parseFloat(marketCap);
    if (isNaN(numericMarketCap) || numericMarketCap <= 0) return 'N/A';
    
    if (numericMarketCap >= 1e9) { // 1B
        return `${(numericMarketCap / 1e9).toFixed(2)}B`;
    }
    if (numericMarketCap >= 1e6) { // 1M
        return `${(numericMarketCap / 1e6).toFixed(2)}M`;
    }
    if (numericMarketCap >= 1e3) { // 1K
        return `${(numericMarketCap / 1e3).toFixed(2)}K`;
    }
    return numericMarketCap.toFixed(2);
}


// Add popup window helper
export function openPopupWindow(url, title) {
    const width = Math.min(800, window.innerWidth * 0.7);
    const height = Math.min(600, window.innerHeight * 0.8);
    const left = (window.innerWidth - width) / 2;
    const top = (window.innerHeight - height) / 2;
    
    return window.open(
        url,
        title,
        `width=${width},height=${height},left=${left},top=${top},toolbar=no,location=no,status=no,menubar=no,scrollbars=yes,resizable=yes`
    );
}

// Modify existing openSearchWindows to use the new popup function
export function openSearchWindows(symbol, contractAddress) {
    
    // First search: $SYMBOL
     
    // Second search: contract address
    setTimeout(() => {
        openPopupWindow(`https://x.com/search?q=${encodeURIComponent(symbol)}`, 'Symbol Search');
    }, 100);
    
    // Second search: contract address
    setTimeout(() => {
        openPopupWindow(`https://x.com/search?q=${encodeURIComponent(contractAddress)}`, 'Address Search');
    }, 100);
}

// ...existing code...
