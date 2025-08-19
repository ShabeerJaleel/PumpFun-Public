import React, { useState, useEffect, useCallback, useRef } from 'react';
import TokenCard from './TokenCard';
import { fetchTokens } from '../utils/helpers';
import styles from '../styles/TokenTable.module.css';

// Add this outside the component to store last fetch times for all panels
const panelLastFetchTimes = new Map();

export function TokenTable({ filters, onTokenSelect, selectedToken, isVisible, onPinsChange }) {
    const [tokens, setTokens] = useState([]);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);
    const [pinnedTokens, setPinnedTokens] = useState(() => {
        const saved = localStorage.getItem(`pinnedTokens-${filters.paneId}`);
        return saved ? JSON.parse(saved) : [];
    });
    
    // Add these new states/refs
    const previousTokens = useRef(new Map());
    const pendingUpdates = useRef(new Map());
    const updateTimeout = useRef(null);
    const lastUpdate = useRef(Date.now());
    const [newTokens, setNewTokens] = useState(new Set());

    // Add ref for tracking if the component is mounted
    const isMounted = useRef(true);

    useEffect(() => {
        return () => {
            isMounted.current = false;
            // Clean up this panel's last fetch time when unmounting
            panelLastFetchTimes.delete(filters.paneId);
        };
    }, [filters.paneId]);

    const applyUpdates = useCallback(() => {
        if (pendingUpdates.current.size === 0) return;

        setTokens(currentTokens => {
            const newTokens = [...currentTokens];
            pendingUpdates.current.forEach((newToken, address) => {
                const index = newTokens.findIndex(t => t.tokenAddress === address);
                if (index >= 0) {
                    newTokens[index] = newToken;
                } else {
                    newTokens.push(newToken);
                }
            });
            pendingUpdates.current.clear();
            return newTokens;
        });
    }, []);

    const queueUpdate = useCallback((newTokenList) => {
        const now = Date.now();
        const timeSinceLastUpdate = now - lastUpdate.current;

        // Create a Set of valid token addresses from new data
        const validTokenAddresses = new Set(newTokenList.map(t => t.tokenAddress));
        
        // Create a map of current tokens for comparison
        const currentTokensMap = new Map(previousTokens.current);
        
        // Track new tokens
        const newAddresses = new Set();
        newTokenList.forEach(token => {
            const existingToken = currentTokensMap.get(token.tokenAddress);
            if (!existingToken) {
                newAddresses.add(token.tokenAddress);
            }
            // Queue updates for new/changed tokens
            pendingUpdates.current.set(token.tokenAddress, token);
        });
        
        // Mark tokens for removal if they're no longer in the feed
        setTokens(currentTokens => {
            return currentTokens.filter(token => 
                validTokenAddresses.has(token.tokenAddress) || 
                pinnedTokens.includes(token.tokenAddress)
            );
        });

        // Update newTokens state if we found new tokens
        if (newAddresses.size > 0) {
            setNewTokens(newAddresses);
            // Clear new token flags after 5 seconds
            setTimeout(() => setNewTokens(new Set()), 5000);
        }

        if (updateTimeout.current) {
            clearTimeout(updateTimeout.current);
        }

        // If it's been more than 2 seconds since last update, update immediately
        if (timeSinceLastUpdate > 2000) {
            applyUpdates();
            lastUpdate.current = now;
        } else {
            // Otherwise, queue update for later
            updateTimeout.current = setTimeout(() => {
                applyUpdates();
                lastUpdate.current = Date.now();
            }, 2000 - timeSinceLastUpdate);
        }
    }, [applyUpdates, pinnedTokens]);

    const handlePin = useCallback((token) => {
        setPinnedTokens(prev => {
            const newPinned = prev.includes(token.tokenAddress)
                ? prev.filter(addr => addr !== token.tokenAddress)
                : [...prev, token.tokenAddress];
            
            if (newPinned.length === 0) {
                localStorage.removeItem(`pinnedTokens-${filters.paneId}`);
            } else {
                localStorage.setItem(`pinnedTokens-${filters.paneId}`, JSON.stringify(newPinned));
            }
            
            onPinsChange(newPinned);
            return newPinned;
        });
    }, [filters.paneId, onPinsChange]);

    // Update the fetchData callback
    const fetchData = useCallback(async () => {
        if (!isVisible) return;

        // Check if enough time has passed since last fetch for this panel
        const now = Date.now();
        const lastFetchTime = panelLastFetchTimes.get(filters.paneId) || 0;
        const timeSinceLastFetch = now - lastFetchTime;
        
        if (timeSinceLastFetch < 5000) {
            return; // Skip fetch if less than 5 seconds has passed for this panel
        }

        // Update last fetch time for this panel before starting the fetch
        panelLastFetchTimes.set(filters.paneId, now);
        
        setLoading(true);
        try {
            // Calculate createdAfter date if ageInMinutes is provided
            const createdAfter = filters.ageInMinutes 
                ? new Date(Date.now() - filters.ageInMinutes * 60000).toISOString()
                : null;

            // Construct query parameters
            const params = new URLSearchParams();
            if (filters.marketCapMin) params.append('marketCapMin', filters.marketCapMin);
            if (filters.marketCapMax) params.append('marketCapMax', filters.marketCapMax);
            if (filters.analysisCompleted) params.append('analysisCompleted', 'true');
            if (createdAfter) params.append('createdAfter', createdAfter);

            const tokenList = await fetchTokens(params);
            
            // Queue updates instead of setting directly
            if (isMounted.current) {
                queueUpdate(tokenList);
                previousTokens.current = new Map(tokenList.map(token => [token.tokenAddress, token]));
                setError(null);
            }
        } catch (err) {
            console.error('Error fetching tokens:', err);
            if (isMounted.current) {
                setError(err.message || 'Failed to fetch tokens');
                setTokens([]); // Clear tokens on error
            }
        } finally {
            if (isMounted.current) {
                setLoading(false);
            }
        }
    }, [filters, isVisible, queueUpdate]);

    useEffect(() => {
        if (!isVisible) return;

        fetchData();
        const interval = setInterval(fetchData, 1000); // Check more frequently
        return () => clearInterval(interval);
    }, [fetchData, isVisible]);

    // Update useEffect to handle pin storage changes
    useEffect(() => {
        if (!isVisible) return;

        const handleStorageChange = (e) => {
            if (e.key === `pinnedTokens-${filters.paneId}`) {
                const newPinnedTokens = e.newValue ? JSON.parse(e.newValue) : [];
                setPinnedTokens(newPinnedTokens);
            }
        };

        // Load initial pinned tokens
        const savedPins = localStorage.getItem(`pinnedTokens-${filters.paneId}`);
        setPinnedTokens(savedPins ? JSON.parse(savedPins) : []);

        window.addEventListener('storage', handleStorageChange);
        window.addEventListener('pinsCleared', () => setPinnedTokens([]));

        return () => {
            window.removeEventListener('storage', handleStorageChange);
            window.removeEventListener('pinsCleared', () => setPinnedTokens([]));
        };
    }, [filters.paneId, isVisible]);

    if (loading && tokens.length === 0) {
        return <div className={styles.loading}>Loading...</div>;
    }

    if (error) {
        return <div className={styles.error}>{error}</div>;
    }

    // Update the sorting logic
    const sortTokens = (tokens) => {
        return [...tokens].sort((a, b) => {
            // First sort by pinned status
            if (pinnedTokens.includes(a.tokenAddress) && !pinnedTokens.includes(b.tokenAddress)) return -1;
            if (!pinnedTokens.includes(a.tokenAddress) && pinnedTokens.includes(b.tokenAddress)) return 1;

            // Then sort by the selected field
            const direction = filters.sortDirection === 'asc' ? 1 : -1;

            if (filters.sortBy === 'marketCap') {
                const aValue = parseFloat(a.marketCap) || 0;
                const bValue = parseFloat(b.marketCap) || 0;
                return (aValue - bValue) * direction;
            }

            // Default sort by creation date
            return (new Date(a.createdAt) - new Date(b.createdAt)) * direction;
        });
    };

    const filterTokens = (tokens) => {
        let filtered = [...tokens];

        // Apply search filter if search term exists
        if (filters.searchTerm) {
            const searchTerm = filters.searchTerm.toLowerCase();
            filtered = filtered.filter(token => 
                token.name?.toLowerCase().includes(searchTerm) ||
                token.symbol?.toLowerCase().includes(searchTerm) ||
                token.tokenAddress?.toLowerCase().includes(searchTerm)
            );
        }

        return filtered;
    };

    // Update the render section to use sortTokens and filterTokens
    return (
        <div className={styles.tokensGrid}>
            {filterTokens(sortTokens(tokens)).map(token => (
                <TokenCard
                    key={token.tokenAddress}
                    token={token}
                    onSelect={onTokenSelect}
                    isSelected={selectedToken?.tokenAddress === token.tokenAddress}
                    isPinned={pinnedTokens.includes(token.tokenAddress)}
                    onPin={handlePin}
                    isNew={newTokens.has(token.tokenAddress)}
                    selectedToken={selectedToken}
                />
            ))}
        </div>
    );
}
