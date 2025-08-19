import React, { useState } from 'react';
import { BsX, BsPinFill } from 'react-icons/bs';
import styles from '../styles/FilterPanel.module.css';

export function FilterPanel({ filters, onClose, onApply, onClearPins, hasPins }) {
    const [localFilters, setLocalFilters] = useState({
        marketCapMin: filters.marketCapMin || '',
        marketCapMax: filters.marketCapMax || '',
        analysisCompleted: filters.analysisCompleted || false,
        ageInMinutes: filters.ageInMinutes || ''
    });

    const handleApply = () => {
        onApply(localFilters);
    };

    const handleReset = () => {
        setLocalFilters({
            marketCapMin: '',
            marketCapMax: '',
            analysisCompleted: false,
            ageInMinutes: ''
        });
    };

    const clearField = (fieldName) => {
        setLocalFilters(prev => ({
            ...prev,
            [fieldName]: ''
        }));
    };

    return (
        <div className={styles.filterPanel}>
            <div className={styles.filterHeader}>
                <h5>Filter Options</h5>
                <div className="d-flex gap-2">
                    {hasPins && (
                        <button 
                            className={styles.btnIcon}
                            onClick={onClearPins}
                            title="Unpin All"
                        >
                            <BsPinFill /> {/* Changed icon to BsPinFill for clarity */}
                        </button>
                    )}
                    <button type="button" className="btn-close" onClick={onClose} />
                </div>
            </div>
            <div className={styles.filterContent}>
                <div className="mb-3">
                    <label className="form-label">Market Cap Range</label>
                    <div className="input-group mb-2">
                        <input
                            type="number"
                            className="form-control"
                            placeholder="Min"
                            value={localFilters.marketCapMin}
                            onChange={(e) => setLocalFilters(prev => ({
                                ...prev,
                                marketCapMin: e.target.value
                            }))}
                        />
                        {localFilters.marketCapMin && (
                            <button 
                                className="btn btn-outline-secondary" 
                                type="button"
                                onClick={() => clearField('marketCapMin')}
                            >
                                <BsX />
                            </button>
                        )}
                        <span className="input-group-text">to</span>
                        <input
                            type="number"
                            className="form-control"
                            placeholder="Max"
                            value={localFilters.marketCapMax}
                            onChange={(e) => setLocalFilters(prev => ({
                                ...prev,
                                marketCapMax: e.target.value
                            }))}
                        />
                        {localFilters.marketCapMax && (
                            <button 
                                className="btn btn-outline-secondary" 
                                type="button"
                                onClick={() => clearField('marketCapMax')}
                            >
                                <BsX />
                            </button>
                        )}
                    </div>
                </div>
                
                <div className="mb-3">
                    <label className="form-label">Token Age (minutes)</label>
                    <div className="input-group">
                        <input
                            type="number"
                            className="form-control"
                            placeholder="Maximum age in minutes"
                            value={localFilters.ageInMinutes}
                            onChange={(e) => setLocalFilters(prev => ({
                                ...prev,
                                ageInMinutes: e.target.value
                            }))}
                        />
                        {localFilters.ageInMinutes && (
                            <button 
                                className="btn btn-outline-secondary" 
                                type="button"
                                onClick={() => clearField('ageInMinutes')}
                            >
                                <BsX />
                            </button>
                        )}
                    </div>
                </div>

                <div className="mb-3 form-check">
                    <input
                        type="checkbox"
                        className="form-check-input"
                        id="analysisCompleted"
                        checked={localFilters.analysisCompleted}
                        onChange={(e) => setLocalFilters(prev => ({
                            ...prev,
                            analysisCompleted: e.target.checked
                        }))}
                    />
                    <label className="form-check-label" htmlFor="analysisCompleted">
                        Analysis Completed
                    </label>
                </div>

                <div className="d-flex gap-2">
                <button 
                        className="btn btn-secondary flex-grow-1"
                        onClick={handleReset}
                    >
                        Reset
                    </button>
                    <button 
                        className="btn btn-primary flex-grow-1"
                        onClick={handleApply}
                    >
                        Apply
                    </button>
                   
                </div>
            </div>
        </div>
    );
}
