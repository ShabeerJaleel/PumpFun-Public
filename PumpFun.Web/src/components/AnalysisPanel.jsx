import React from 'react';
import styles from '../styles/AnalysisPanel.module.css';

export function AnalysisPanel({ token, onClose }) {
    const createMarkup = (htmlContent) => {
        if (!htmlContent) return { __html: '' };
        return { __html: htmlContent };
    };

    return (
        <div className={`${styles.analysisPanel} ${token ? styles.visible : ''}`}>
            <div className={styles.analysisHeader}>
                <h4>{token?.name} Analysis</h4>
                <button 
                    type="button" 
                    className="btn-close btn-close-sm" 
                    onClick={onClose}
                    aria-label="Close"
                ></button>
            </div>
            <div className={styles.analysisContent}>
                <div 
                    className={styles.analysisText}
                    dangerouslySetInnerHTML={createMarkup(token?.analysis)}
                />
            </div>
        </div>
    );
}
