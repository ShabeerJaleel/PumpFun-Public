import React, { useState, useEffect } from 'react';
import { TokenTable } from './TokenTable';
import { FilterPanel } from './FilterPanel';
import { BsFilter, BsPin, BsPinFill, BsSortDown } from 'react-icons/bs'; // Add BsSearch, BsX
import { formatMarketCap } from '../utils/helpers';  // Add this import at the top
import { ChatWindow } from './ChatWindow';  // Add this import
import styles from '../styles/MultiPaneLayout.module.css';  // Add this import

const defaultFilters = {
    marketCapMin: '',
    marketCapMax: '',
    analysisCompleted: false,
    ageInMinutes: '',
    showFilters: false
};

// Add this near the defaultFilters
const SORT_OPTIONS = [
    { label: 'Time ↓', value: { sortBy: 'createdAt', sortDirection: 'desc' } },
    { label: 'Time ↑', value: { sortBy: 'createdAt', sortDirection: 'asc' } },
    { label: 'Market Cap ↓', value: { sortBy: 'marketCap', sortDirection: 'desc' } },
    { label: 'Market Cap ↑', value: { sortBy: 'marketCap', sortDirection: 'asc' } }
];

export function MultiPaneLayout({ onTokenSelect, selectedToken, onPinsChange, searchTerm }) {  // Add selectedToken prop
    const [filters, setFilters] = useState(() => {
        const savedFilters = localStorage.getItem('paneFilters');
        if (savedFilters) {
            return JSON.parse(savedFilters);
        }
        return {
            pane1: { ...defaultFilters },
            pane2: { ...defaultFilters },
            pane3: { ...defaultFilters }
        };
    });

    const [collapsedPanes, setCollapsedPanes] = useState(() => {
        const saved = localStorage.getItem('collapsedPanes');
        return saved ? JSON.parse(saved) : {};
    });

    const [paneData, setPaneData] = useState(() => ({
        pane1: { pinnedTokens: [] },
        pane2: { pinnedTokens: [] },
        pane3: { pinnedTokens: [] }
    }));

    // Add sort state
    const [sortOptions, setSortOptions] = useState(() => ({
        pane1: { sortBy: 'createdAt', sortDirection: 'desc' },
        pane2: { sortBy: 'createdAt', sortDirection: 'desc' },
        pane3: { sortBy: 'createdAt', sortDirection: 'desc' }
    }));

    const [activeSortMenu, setActiveSortMenu] = useState(null);

    // Add chat panel state
    const [isChatDocked, setIsChatDocked] = useState(() => {
        const saved = localStorage.getItem('chatDocked');
        return saved ? JSON.parse(saved) : true;
    });

    // Update chatMessages initialization to load from localStorage
    const [chatMessages, setChatMessages] = useState(() => {
        const saved = localStorage.getItem('chatMessages');
        return saved ? JSON.parse(saved) : [];
    });

    // Save chat dock state
    useEffect(() => {
        localStorage.setItem('chatDocked', JSON.stringify(isChatDocked));
    }, [isChatDocked]);

    // Add effect to save messages to localStorage
    useEffect(() => {
        localStorage.setItem('chatMessages', JSON.stringify(chatMessages));
    }, [chatMessages]);

    useEffect(() => {
        localStorage.setItem('paneFilters', JSON.stringify(filters));
    }, [filters]);

    useEffect(() => {
        localStorage.setItem('collapsedPanes', JSON.stringify(collapsedPanes));
    }, [collapsedPanes]);

    const toggleFilters = (paneId) => {
        setFilters(prev => ({
            ...prev,
            [paneId]: { ...prev[paneId], showFilters: !prev[paneId].showFilters }
        }));
    };

    const togglePane = (paneId) => {
        setCollapsedPanes(prev => ({
            ...prev,
            [paneId]: !prev[paneId]
        }));
    };

    const updateFilters = (paneId, newFilters) => {
        setFilters(prev => ({
            ...prev,
            [paneId]: { ...prev[paneId], ...newFilters, showFilters: false }
        }));
    };

    const handlePinsChange = (paneId, pinnedTokens) => {
        setPaneData(prev => {
            const newPaneData = {
                ...prev,
                [paneId]: { ...prev[paneId], pinnedTokens }
            };
            const hasAnyPins = Object.values(newPaneData).some(pane => 
                pane.pinnedTokens && pane.pinnedTokens.length > 0
            );
            onPinsChange?.(hasAnyPins);
            return newPaneData;
        });
    };

    const handleClearPins = (paneId) => {
        setPaneData(prev => ({
            ...prev,
            [paneId]: { ...prev[paneId], pinnedTokens: [] }
        }));
        localStorage.removeItem(`pinnedTokens-${paneId}`);
        // Add this line to notify the TokenTable
        onPinsChange?.(false);
    };

    // Update sort handler
    const handleSort = (paneId, sortOption) => {
        setSortOptions(prev => ({
            ...prev,
            [paneId]: sortOption
        }));
        setActiveSortMenu(null); // Close menu after selection
    };

    const getFilterSummary = (filter) => {
        const parts = [];
        if (filter.marketCapMin || filter.marketCapMax) {
            parts.push(`MC: ${filter.marketCapMin ? formatMarketCap(filter.marketCapMin) : '0'}-${filter.marketCapMax ? formatMarketCap(filter.marketCapMax) : '∞'}`);
        }
        if (filter.ageInMinutes) {
            parts.push(`Age: <${filter.ageInMinutes}m`);
        }
        if (filter.analysisCompleted) {
            parts.push('Analyzed');
        }
        return parts.join(' | ');
    };

    // Add this function inside MultiPaneLayout component
    const getVisiblePanesCount = () => {
        return ['pane1', 'pane2', 'pane3'].filter(paneId => !collapsedPanes[paneId]).length;
    };

    return (
        <div className={styles.layoutContainer}>
            <div className={styles.paneMenu}>
                {['pane1', 'pane2', 'pane3'].map((paneId, index) => (
                    <button
                        key={paneId}
                        className={`${styles.menuItem} ${collapsedPanes[paneId] ? styles.collapsed : ''}`}
                        onClick={() => togglePane(paneId)}
                        title={collapsedPanes[paneId] ? 'Show panel' : 'Hide panel'}
                    >
                        <div className={styles.menuItemContent}>
                            <span className={styles.menuNumber}>{index + 1}</span>
                            {collapsedPanes[paneId] ? 
                                <BsPin size={16} /> : 
                                <BsPinFill size={16} />
                            }
                        </div>
                    </button>
                ))}
                {/* Add chat panel button */}
                <button
                    className={`${styles.menuItem} ${collapsedPanes['pane4'] ? styles.collapsed : ''}`}
                    onClick={() => togglePane('pane4')}
                    title={collapsedPanes['pane4'] ? 'Show chat' : 'Hide chat'}
                >
                    <div className={styles.menuItemContent}>
                        <span className={styles.menuNumber}>4</span>
                        {collapsedPanes['pane4'] ? 
                            <BsPin size={16} /> : 
                            <BsPinFill size={16} />
                        }
                    </div>
                </button>
            </div>
            <div className={`${styles.panesContainer} ${!collapsedPanes['pane4'] ? styles.withChat : ''} ${
                getVisiblePanesCount() === 1 ? styles.singlePane : 
                getVisiblePanesCount() === 2 ? styles.twoPane : 
                getVisiblePanesCount() === 3 ? styles.threePane :
                'four-panes'
            }`}>
                {['pane1', 'pane2', 'pane3'].map((paneId) => (
                    <div key={paneId} className={`${styles.pane} ${collapsedPanes[paneId] ? styles.collapsed : ''}`}>
                        {!collapsedPanes[paneId] && (
                            <>
                                <div className={styles.paneHeader}>
                                    <div className={styles.paneTitle}>
                                        <span className={styles.paneNumber}>{paneId.replace('pane', '')}</span>
                                        <div className={styles.filterSummary}>
                                            {getFilterSummary(filters[paneId])}
                                        </div>
                                    </div>
                                    <div className={styles.paneControls}>
                                        <div className="position-relative">
                                            <button 
                                                className={styles.sortButton}
                                                onClick={() => setActiveSortMenu(activeSortMenu === paneId ? null : paneId)}
                                                title="Sort options"
                                            >
                                                <BsSortDown />
                                                {SORT_OPTIONS.find(opt => 
                                                    opt.value.sortBy === sortOptions[paneId].sortBy && 
                                                    opt.value.sortDirection === sortOptions[paneId].sortDirection
                                                )?.label || 'Sort'}
                                            </button>
                                            {activeSortMenu === paneId && (
                                                <div className={styles.sortMenu}>
                                                    {SORT_OPTIONS.map((option) => (
                                                        <button
                                                            key={`${option.value.sortBy}-${option.value.sortDirection}`}
                                                            className={`${styles.sortMenuItem} ${
                                                                sortOptions[paneId].sortBy === option.value.sortBy &&
                                                                sortOptions[paneId].sortDirection === option.value.sortDirection
                                                                    ? styles.active
                                                                    : ''
                                                            }`}
                                                            onClick={() => handleSort(paneId, option.value)}
                                                        >
                                                            {option.label}
                                                        </button>
                                                    ))}
                                                </div>
                                            )}
                                        </div>
                                        <button 
                                            className={styles.btnIcon}
                                            onClick={() => toggleFilters(paneId)}
                                            title="Show filters"
                                        >
                                            <BsFilter />
                                        </button>
                                        <button 
                                            className={`${styles.btnIcon} ${styles.collapseButton}`}
                                            onClick={() => togglePane(paneId)}
                                            title={collapsedPanes[paneId] ? 'Show panel' : 'Hide panel'}
                                        >
                                            {collapsedPanes[paneId] ? <BsPin /> : <BsPinFill />}
                                        </button>
                                    </div>
                                </div>
                                <div className={styles.paneContent}>
                                    <TokenTable 
                                        filters={{ 
                                            ...filters[paneId], 
                                            paneId,
                                            ...sortOptions[paneId], 
                                            searchTerm // Pass the global search term
                                        }}
                                        onTokenSelect={onTokenSelect}
                                        isVisible={!collapsedPanes[paneId]} // Ensure isVisible is correctly passed
                                        onPinsChange={(pins) => handlePinsChange(paneId, pins)}
                                        selectedToken={selectedToken}  // Pass the prop correctly
                                    />
                                    {filters[paneId].showFilters && (
                                        <FilterPanel
                                            filters={filters[paneId]}
                                            onClose={() => toggleFilters(paneId)}
                                            onApply={(newFilters) => updateFilters(paneId, newFilters)}
                                            hasPins={paneData[paneId].pinnedTokens?.length > 0}
                                            onClearPins={() => handleClearPins(paneId)}
                                        />
                                    )}
                                </div>
                            </>
                        )}
                    </div>
                ))}
                <div className={`${styles.pane} ${styles.chatPane} ${collapsedPanes['pane4'] ? styles.collapsed : ''}`}>
                    {!collapsedPanes['pane4'] && (
                        <>
                            <div className={styles.paneHeader}>
                                <div className={styles.paneTitle}>
                                    <span className={styles.paneNumber}>4</span>
                                    <div>Telegram Messages</div>
                                </div>
                                <div className={styles.paneControls}>
                                    <button 
                                        className={`${styles.btnIcon} ${styles.collapseButton}`}
                                        onClick={() => togglePane('pane4')}
                                        title={collapsedPanes['pane4'] ? 'Show chat' : 'Hide chat'}
                                    >
                                        {collapsedPanes['pane4'] ? <BsPin /> : <BsPinFill />}
                                    </button>
                                </div>
                            </div>
                            <div className={styles.paneContent}>
                                <ChatWindow 
                                    messages={chatMessages}
                                    setMessages={setChatMessages}
                                />
                            </div>
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}
