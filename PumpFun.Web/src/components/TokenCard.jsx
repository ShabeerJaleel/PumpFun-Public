import React, { useState, useRef, useEffect } from 'react';
import { 
    formatElapsedTime, 
    truncateDescription, 
    copyToClipboard, 
    formatMarketCap,
    truncateText,
    openSearchWindows,
    openPopupWindow 
} from '../utils/helpers';
import { OverlayTrigger, Tooltip, Overlay, Popover } from 'react-bootstrap';
import CopyToast from './CopyToast';
import CreateTokenModal from './CreateTokenModal';
import { BsSearch, BsPin, BsPinFill, BsPlusCircle, BsPersonFill, BsWallet2, BsArrowRepeat, BsLightning, BsCartCheck, BsClock, BsTwitter, BsTelegram, BsGlobe } from 'react-icons/bs';
import styles from '../styles/TokenCard.module.css';

// Remove onModify from props
function TokenCard({ token, onSelect, isSelected, isPinned, onPin, isNew }) {
    const [showToast, setShowToast] = useState(false);
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [showContextMenu, setShowContextMenu] = useState(false);
    const [contextMenuPosition, setContextMenuPosition] = useState({ x: 0, y: 0 });
    const [analysisRequested, setAnalysisRequested] = useState(false);  // Add this state
    const cardRef = useRef(null);

    const NAME_MAX_LENGTH = 20;
    const DESCRIPTION_MAX_LENGTH = 80;

    // Add null checks and default values
    const isNameTruncated = (token.name || '').length > NAME_MAX_LENGTH;
    const isDescriptionTruncated = (token.description || '').length > DESCRIPTION_MAX_LENGTH;

    const handleCopy = async (e) => {
        e.preventDefault();
        e.stopPropagation(); // Prevent card selection when copying
        try {
            await copyToClipboard(token.tokenAddress);
            setShowToast(true);
        } catch (err) {
            console.error('Failed to copy:', err);
        }
    };

    const handleClick = (e) => {
        if (!e.target.closest('a')) {
            onSelect(token);
        }
        setShowContextMenu(false);
    };

    useEffect(() => {
        const handleClickOutside = (event) => {
            // Don't close the menu if clicking a link inside it
            if (event.target.closest(`.${styles.contextMenuItem}`)) {
                return;
            }
            if (showContextMenu && !cardRef.current?.contains(event.target)) {
                setShowContextMenu(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, [showContextMenu]);

    const handleContextMenu = (e) => {
        e.preventDefault();
        // Calculate position relative to viewport, but offset slightly to not appear under cursor
        setContextMenuPosition({ x: e.clientX + 2, y: e.clientY + 2 });
        setShowContextMenu(true);
    };

    const handlePin = (e) => {
        e.preventDefault();
        e.stopPropagation();
        onPin(token);
        // Remove the event dispatch from here as it's now handled in TokenTable
    };

    const handleCreate = (e) => {
        e.preventDefault();
        e.stopPropagation();
        setShowCreateModal(true);
    };

    const handleAnalyseRequest = async (e) => {
        e.preventDefault();
        e.stopPropagation();
        
        // Close the context menu immediately
        setShowContextMenu(false);
        
        try {
            const response = await fetch('/api/tokens/analyse', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(token.tokenAddress)
            });

            if (response.ok) {
                setAnalysisRequested(true);
                setShowToast(true);
            }
        } catch (error) {
            console.error('Error requesting analysis:', error);
        }
    };

    // Add useEffect to handle content transitions
    useEffect(() => {
        if (!cardRef.current) return;
        
        const contentElements = cardRef.current.querySelectorAll(`.${styles.description}, .${styles.metricValue}`);
        contentElements.forEach(element => {
            // Only animate if content actually changed
            if (element.textContent !== element.dataset.previousContent) {
                element.style.opacity = '0';
                setTimeout(() => {
                    element.style.opacity = '1';
                    element.dataset.previousContent = element.textContent;
                }, 50);
            }
        });
    }, [token]);

    const renderAddressTooltip = (props) => (
        <Tooltip id={`tooltip-${token.tokenAddress}`} {...props}>
            {token.tokenAddress}
        </Tooltip>
    );

    const renderImageTooltip = (props) => (
        <Tooltip id="image-tooltip" {...props}>
            <div className={styles.previewImageContainer}>
                <img src={token.image} alt={token.name} className={styles.previewImage} />
            </div>
        </Tooltip>
    );

    const renderNameTooltip = (props) => (
        <Tooltip id={`name-tooltip-${token.tokenAddress}`} {...props}>
            {token.name}
        </Tooltip>
    );

    const renderDescriptionTooltip = (props) => (
        <Tooltip id={`desc-tooltip-${token.tokenAddress}`} {...props}>
            {token.description}
        </Tooltip>
    );

    const renderDevTooltip = (props) => (
        <Tooltip id={`dev-tooltip-${token.tokenAddress}`} {...props}>
            Dev Stats:
            Created: {token.tokensCreatedByDev} tokens
            Holdings: {token.devHolds}%
            Self-buy: {token.isDevBoughtOwnToken ? 'Yes' : 'No'}
            Relaunches: {token.timesRelaunchedByDev || 0}
        </Tooltip>
    );

    // Add these tooltip render functions
    const renderSocialTooltip = (type, count) => (props) => (
        <Tooltip id={`${type}-tooltip-${token.tokenAddress}`} {...props}>
            {`${count} tokens found with same ${type}`}
        </Tooltip>
    );

    const renderDevStatsTooltip = (count) => (props) => (
        <Tooltip id={`dev-tooltip-${token.tokenAddress}`} {...props}>
            {`Developer has created ${count} tokens`}
        </Tooltip>
    );

    const renderDevHoldsTooltip = (percentage) => (props) => (
        <Tooltip id={`holds-tooltip-${token.tokenAddress}`} {...props}>
            {`Developer holds ${Math.round(percentage)}% of tokens`}
        </Tooltip>
    );

    const renderSnipingTooltip = (percentage) => (props) => (
        <Tooltip id={`sniping-tooltip-${token.tokenAddress}`} {...props}>
            {`${Math.round(percentage)}% of transactions are from known snipers`}
        </Tooltip>
    );

    const renderDevBoughtTooltip = (percentage) => (props) => (
        <Tooltip id={`devbought-tooltip-${token.tokenAddress}`} {...props}>
            {`Developer bought ${Math.round(percentage)}% of their own tokens`}
        </Tooltip>
    );

    const renderRelaunchTooltip = (count) => (props) => (
        <Tooltip id={`relaunch-tooltip-${token.tokenAddress}`} {...props}>
            {`Developer has relaunched this token ${count} times`}
        </Tooltip>
    );

    // Update the tooltip render function
    const renderBuysTooltip = (percentage) => (props) => (
        <Tooltip id={`buys-tooltip-${token.tokenAddress}`} {...props}>
            {`${Math.round(percentage)}% of buys occurred in the same second`}
        </Tooltip>
    );

    // Helper to wrap content with tooltip only if count exists
    const maybeWrapWithTooltip = (content, type, count) => {
        if (!count || count <= 0) return content;

        return (
            <OverlayTrigger
                placement="top"
                overlay={renderSocialTooltip(type, count)}
            >
                {content}
            </OverlayTrigger>
        );
    };

    const formatAddress = (address) => {
        if (!address) return '';
        return `${address.slice(0, 5)}...${address.slice(-5)}`;
    };

    const handleSearch = (e) => {
        e.preventDefault();
        e.stopPropagation();
        openSearchWindows(token.symbol, token.tokenAddress);
    };

    const handleSocialClick = (e, url) => {
        e.preventDefault();
        e.stopPropagation();
        openPopupWindow(url, 'Social Link');
    };

    return (
        <>
            <div 
                ref={cardRef}
                className={`${styles.tokenCard} ${isSelected ? styles.selected : ''} ${isNew ? styles.new : ''} ${isPinned ? styles.pinned : ''} ${token.analysisCompleted ? styles.analyzed : ''}`}
                onClick={handleClick}
                onContextMenu={handleContextMenu}
                style={{ cursor: 'pointer', position: 'relative' }}
                data-pinned={isPinned}
            >
                <button 
                    className={`btn btn-link btn-sm p-0 ${styles.searchButton}`}
                    onClick={handleSearch}
                    title="Search on X/Twitter"
                >
                    <BsSearch />
                </button>
                <button 
                    className={`btn btn-link btn-sm p-0 ${styles.pinButton}`}
                    onClick={handlePin}
                    title={isPinned ? "Unpin" : "Pin"}
                >
                    {isPinned ? <BsPinFill /> : <BsPin />}
                </button>
                
                <div className={styles.cardHeader}>
                    <OverlayTrigger
                        placement="right"
                        delay={{ show: 250, hide: 400 }}
                        overlay={token.image ? renderImageTooltip : <></>}
                    >
                        <div className={styles.imageContainer}>
                            {token.image ? (
                                <img 
                                    src={token.image} 
                                    alt={token.name}
                                />
                            ) : (
                                <div className={styles.imagePlaceholder}></div>
                            )}
                        </div>
                    </OverlayTrigger>
                    <div className={styles.info}>
                        <div className="d-flex align-items-center gap-1">
                            <h3 className={styles.symbol}>
                                {token.symbol}
                            </h3>
                            <span className={styles.age}>• {formatElapsedTime(token.createdAt)}</span>
                        </div>
                        {isNameTruncated ? (
                            <OverlayTrigger
                                placement="top"
                                delay={{ show: 250, hide: 400 }}
                                overlay={renderNameTooltip}
                            >
                                <span className={styles.name}>
                                    {truncateText(token.name || '', NAME_MAX_LENGTH)}
                                    {token.analysisCompleted && token.sameNameTokenCount > 0 && (
                                        <span className={styles.socialBadge}>
                                            {token.sameNameTokenCount}
                                        </span>
                                    )}
                                </span>
                            </OverlayTrigger>
                        ) : (
                            <span className={styles.name}>
                                {truncateText(token.name || '', NAME_MAX_LENGTH)}
                                {token.analysisCompleted && token.sameNameTokenCount > 0 && (
                                    <span className={styles.socialBadge}>
                                        {token.sameNameTokenCount}
                                    </span>
                                )}
                            </span>
                        )}
                        <div className={styles.metric}>
                            <div className="d-flex align-items-center gap-2">
                                <span className={styles.metricValue}>
                                    {formatMarketCap(token.marketCap)}
                                </span>
                                <span 
                                    className={styles.addressDisplay}
                                    onClick={handleCopy}
                                    title="Click to copy address"
                                >
                                    {formatAddress(token.tokenAddress)}
                                </span>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div className={styles.cardBody}>
                    {isDescriptionTruncated ? (
                        <OverlayTrigger
                            placement="top"
                            delay={{ show: 250, hide: 400 }}
                            overlay={renderDescriptionTooltip}
                        >
                            <div className={styles.description}>
                                {token.description || ''}
                            </div>
                        </OverlayTrigger>
                    ) : (
                        <div className={styles.description}>
                            {token.description || ''}
                        </div>
                    )}
                </div>

                <div className={styles.cardFooter}>
                    {token.twitter && (
                        maybeWrapWithTooltip(
                            <a 
                                href={token.twitter} 
                                onClick={(e) => handleSocialClick(e, token.twitter)}
                                className={styles.socialLink}
                            >
                                <BsTwitter />
                                {token.analysisCompleted && token.sameTwitterTokenCount > 0 && (
                                    <span className={styles.socialBadge}>
                                        {token.sameTwitterTokenCount}
                                    </span>
                                )}
                            </a>,
                            'Twitter',
                            token.analysisCompleted ? token.sameTwitterTokenCount : 0
                        )
                    )}
                    {token.telegram && (
                        maybeWrapWithTooltip(
                            <a 
                                href={token.telegram} 
                                onClick={(e) => handleSocialClick(e, token.telegram)}
                                className={styles.socialLink}
                            >
                                <BsTelegram />
                                {token.analysisCompleted && token.sameTelegramTokenCount > 0 && (
                                    <span className={styles.socialBadge}>
                                        {token.sameTelegramTokenCount}
                                    </span>
                                )}
                            </a>,
                            'Telegram',
                            token.analysisCompleted ? token.sameTelegramTokenCount : 0
                        )
                    )}
                    {token.website && (
                        maybeWrapWithTooltip(
                            <a 
                                href={token.website} 
                                onClick={(e) => handleSocialClick(e, token.website)}
                                className={styles.socialLink}
                            >
                                <BsGlobe />
                                {token.analysisCompleted && token.sameWebsiteTokenCount > 0 && (
                                    <span className={styles.socialBadge}>
                                        {token.sameWebsiteTokenCount}
                                    </span>
                                )}
                            </a>,
                            'Website',
                            token.analysisCompleted ? token.sameWebsiteTokenCount : 0
                        )
                    )}
                    {token.tokensCreatedByDev > 1 && (
                        <OverlayTrigger
                            placement="top"
                            overlay={renderDevStatsTooltip(token.tokensCreatedByDev)}
                        >
                            <span className={styles.socialLink}>
                                <BsPersonFill />
                                <span className={styles.socialBadge}>
                                    {token.tokensCreatedByDev}
                                </span>
                            </span>
                        </OverlayTrigger>
                    )}
                    {token.devHolds > 0 && (
                        <OverlayTrigger
                            placement="top"
                            overlay={renderDevHoldsTooltip(token.devHolds)}
                        >
                            <span className={styles.socialLink}>
                                <BsWallet2 />
                                <span className={styles.socialBadge}>
                                    {Math.round(token.devHolds)}%
                                </span>
                            </span>
                        </OverlayTrigger>
                    )}
                    {token.snipingPercentage > 0 && (
                        <OverlayTrigger
                            placement="top"
                            overlay={renderSnipingTooltip(token.snipingPercentage)}
                        >
                            <span className={styles.socialLink}>
                                <BsLightning /> {/* Keep lightning for sniping */}
                                <span className={styles.socialBadge}>
                                    {Math.round(token.snipingPercentage)}%
                                </span>
                            </span>
                        </OverlayTrigger>
                    )}
                    {token.devBoughtPercentage > 0 && (
                        <OverlayTrigger
                            placement="top"
                            overlay={renderDevBoughtTooltip(token.devBoughtPercentage)}
                        >
                            <span className={styles.socialLink}>
                                <BsCartCheck />
                                <span className={styles.socialBadge}>
                                    {Math.round(token.devBoughtPercentage)}%
                                </span>
                            </span>
                        </OverlayTrigger>
                    )}
                    {token.timesRelaunchedByDev > 0 && (
                        <OverlayTrigger
                            placement="top"
                            overlay={renderRelaunchTooltip(token.timesRelaunchedByDev)}
                        >
                            <span className={styles.socialLink}>
                                <BsArrowRepeat />
                                <span className={styles.socialBadge}>
                                    {token.timesRelaunchedByDev}
                                </span>
                            </span>
                        </OverlayTrigger>
                    )}
                    {token.buysAtTheSameSecond > 0 && (
                        <OverlayTrigger
                            placement="top"
                            overlay={renderBuysTooltip(token.buysAtTheSameSecond)}
                        >
                            <span className={styles.socialLink}>
                                <BsClock /> {/* Changed to clock icon for same-second buys */}
                                <span className={styles.socialBadge}>
                                    {Math.round(token.buysAtTheSameSecond)}%
                                </span>
                            </span>
                        </OverlayTrigger>
                    )}
                </div>
            </div>

            {showContextMenu && (
                <div 
                    className={styles.contextMenu}
                    style={{
                        top: contextMenuPosition.y,
                        left: contextMenuPosition.x,
                    }}
                >
                    <div className={styles.contextMenuItems}>
                        <a 
                            href={`https://bullx.io/terminal?chainId=1399811149&address=${token.tokenAddress}`}
                            target="_blank" 
                            rel="noopener noreferrer"
                            className={styles.contextMenuItem}
                            onClick={() => setShowContextMenu(false)}  // Add this line
                        >
                            <i className="bi bi-graph-up me-2"></i>
                            View on BullX
                        </a>
                        <a 
                            href={`https://pump.fun/coin/${token.tokenAddress}`}
                            target="_blank" 
                            rel="noopener noreferrer"
                            className={styles.contextMenuItem}
                            onClick={() => setShowContextMenu(false)}  // Add this line
                        >
                            <span className={`${styles.pumpfunIcon} me-2`}></span>
                            View on Pump.Fun
                        </a>
                        <button 
                            className={styles.contextMenuItem}
                            onClick={(e) => {
                                e.preventDefault();
                                e.stopPropagation();
                                setShowContextMenu(false);
                                setShowCreateModal(true);
                            }}
                        >
                            <BsPlusCircle className="me-2" />
                            Create Similar Token
                        </button>
                        {!token.analysisCompleted && !analysisRequested && (
                            <button 
                                className={styles.contextMenuItem}
                                onClick={handleAnalyseRequest}
                            >
                                <BsLightning className="me-2" />
                                Analyze Token
                            </button>
                        )}
                    </div>
                </div>
            )}

            <CreateTokenModal 
                show={showCreateModal}
                onHide={() => setShowCreateModal(false)}
                sourceToken={token}
            />
            <CopyToast 
                show={showToast} 
                onClose={() => setShowToast(false)} 
                message={analysisRequested ? "Analysis requested" : "Address copied"}
            />
        </>
    );
}

export default React.memo(TokenCard, (prev, next) => {
    // Only re-render if important props changed
    return (
        prev.token.tokenAddress === next.token.tokenAddress &&
        prev.isSelected === next.isSelected &&
        prev.isPinned === next.isPinned &&
        prev.isNew === next.isNew &&
        JSON.stringify(prev.token) === JSON.stringify(next.token)
    );
});
