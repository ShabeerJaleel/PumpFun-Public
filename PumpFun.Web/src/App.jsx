import React, { useState, useEffect } from 'react';
import { MultiPaneLayout } from './components/MultiPaneLayout';
import { AnalysisPanel } from './components/AnalysisPanel';
import { BsSun, BsMoon, BsPin, BsSearch, BsX } from 'react-icons/bs';
import './styles/Global.css';

function App() {
    const [selectedToken, setSelectedToken] = useState(null);
    const [theme, setTheme] = useState('light');
    const [hasPinnedTokens, setHasPinnedTokens] = useState(false);
    const [isChatCollapsed, setIsChatCollapsed] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');

    useEffect(() => {
        const checkPinnedTokens = () => {
            const hasAnyPins = ['pane1', 'pane2', 'pane3'].some(paneId => {
                const pinnedTokens = localStorage.getItem(`pinnedTokens-${paneId}`);
                return pinnedTokens && JSON.parse(pinnedTokens).length > 0;
            });
            setHasPinnedTokens(hasAnyPins);
        };

        checkPinnedTokens();

        const handlePinChange = () => {
            checkPinnedTokens();
        };

        window.addEventListener('pinsChanged', handlePinChange);
        window.addEventListener('pinsCleared', handlePinChange);

        return () => {
            window.removeEventListener('pinsChanged', handlePinChange);
            window.removeEventListener('pinsCleared', handlePinChange);
        };
    }, []);

    const toggleTheme = () => {
        const newTheme = theme === 'light' ? 'dark' : 'light';
        setTheme(newTheme);
        document.documentElement.setAttribute('data-theme', newTheme);
    };

    const handleUnpinAll = () => {
        ['pane1', 'pane2', 'pane3'].forEach(paneId => {
            localStorage.removeItem(`pinnedTokens-${paneId}`);
        });
        setHasPinnedTokens(false);
        // Update this to use a custom event that TokenTable listens for
        window.dispatchEvent(new CustomEvent('pinsCleared'));
    };

    return (
        <div className="app">
            <div className="controls-container">
                <div className="search-container">
                    <input
                        type="text"
                        placeholder="Search tokens..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="search-input"
                    />
                    {searchTerm && (
                        <button
                            className="clear-search"
                            onClick={() => setSearchTerm('')}
                        >
                            <BsX />
                        </button>
                    )}
                </div>
                <button 
                    onClick={handleUnpinAll}
                    title="Unpin all tokens"
                    className="btn-icon"
                    disabled={!hasPinnedTokens}
                >
                    <BsPin size={16} />
                </button>
                <button 
                    onClick={toggleTheme}
                    title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
                    className="btn-icon"
                >
                    {theme === 'light' ? <BsMoon size={16} /> : <BsSun size={16} />}
                </button>
            </div>
            <main>
                <MultiPaneLayout 
                    onTokenSelect={setSelectedToken} 
                    selectedToken={selectedToken}
                    onPinsChange={(hasPins) => setHasPinnedTokens(hasPins)}
                    searchTerm={searchTerm} // Pass search term to MultiPaneLayout
                />
                <AnalysisPanel 
                    token={selectedToken} 
                    onClose={() => setSelectedToken(null)} 
                />
            </main>
        </div>
    );
}

export default App;
