import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Modal, Form, Button, Alert, Image, Popover, Overlay } from 'react-bootstrap';
import { BsUpload, BsLink45Deg, BsExclamationTriangle } from 'react-icons/bs';
import styles from '../styles/CreateTokenModal.module.css'; // Import the new CSS module

function CreateTokenModal({ show, onHide, sourceToken }) {
    const [formData, setFormData] = useState({
        tokenAddress: '',
        name: '',
        symbol: '',
        description: '',
        twitter: '',
        telegram: '',
        website: '',
        imageUrl: '',
        imageData: null,
        initialBuyAmount: '0', // Ensure default is '0'
        isSimulation: true  // Add simulation field with default true
    });
    const [preview, setPreview] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(null);
    const [showImageUrlInput, setShowImageUrlInput] = useState(false);
    const [showImageUrlPopover, setShowImageUrlPopover] = useState(false);
    const [imageUrlTarget, setImageUrlTarget] = useState(null);
    const [tempImageUrl, setTempImageUrl] = useState('');
    const [showConfirmation, setShowConfirmation] = useState(false);

    // Add ref to track if modal has been initialized
    const initialized = useRef(false);

    // Modify useEffect to only initialize once when modal opens
    useEffect(() => {
        if (show && sourceToken && !initialized.current) {
            setFormData({
                tokenAddress: sourceToken.tokenAddress || '',
                name: sourceToken.name || '',
                symbol: sourceToken.symbol || '',
                description: sourceToken.description || '',
                twitter: sourceToken.twitter || '',
                telegram: sourceToken.telegram || '',
                website: sourceToken.website || '',
                imageUrl: sourceToken.image || '',
                imageData: null,
                initialBuyAmount: '0',
                isSimulation: true
            });
            setPreview(sourceToken.image || '');
            initialized.current = true;
        }

        // Reset initialization when modal closes
        if (!show) {
            initialized.current = false;
        }
    }, [show, sourceToken]);

    // Add a ref to track if form has been modified
    const formModified = useRef(false);

    // Fix initialBuyAmount in the second useEffect
    useEffect(() => {
        if (show && sourceToken && !formModified.current) {
            // Only initialize if the form hasn't been modified
            setFormData({
                tokenAddress: sourceToken.tokenAddress || '',
                name: sourceToken.name || '',
                symbol: sourceToken.symbol || '',
                description: sourceToken.description || '',
                twitter: sourceToken.twitter || '',
                telegram: sourceToken.telegram || '',
                website: sourceToken.website || '',
                imageUrl: sourceToken.image || '',
                imageData: null,
                initialBuyAmount: '0',  // Change from empty string to '0'
                isSimulation: true
            });
            setPreview(sourceToken.image || '');
        }
    }, [sourceToken, show]);

    // Modify useEffect to reset success state when modal closes
    useEffect(() => {
        if (!show) {
            initialized.current = false;
            formModified.current = false;
            setSuccess(null);  // Reset success state
            setError('');      // Reset error state
        }
    }, [show]);

    // Add these at component level
    useEffect(() => {
        if (!show) {
            setShowConfirmation(false);  // Reset confirmation dialog when modal closes
        }
    }, [show]);

    // Modify handleInputChange to mark form as modified
    const handleInputChange = (e) => {
        const { name, value } = e.target;
        formModified.current = true;
        setFormData(prev => ({ ...prev, [name]: value }));
        if (name === 'isSimulation' && value === true) {
            setShowConfirmation(false);  // Reset confirmation when switching to simulation mode
        }
    };

    // Add reset logic when modal is closed
    const handleHide = () => {
        formModified.current = false;
        setSuccess(null);     // Reset success state
        setError('');         // Reset error state
        onHide();
    };

    // Add function to convert image URL to Blob
    const convertImageUrlToBlob = async (url) => {
        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error('Failed to fetch image');
            const blob = await response.blob();
            return blob;
        } catch (error) {
            console.error('Error converting image URL to blob:', error);
            return null;
        }
    };

    // Update handleImageUrlChange to also store the blob
    const handleImageUrlChange = useCallback(async (e) => {
        const url = e.target.value;
        formModified.current = true;
        setFormData(prev => ({ ...prev, imageUrl: url }));
        
        if (url) {
            try {
                const blob = await convertImageUrlToBlob(url);
                if (blob) {
                    setFormData(prev => ({ ...prev, imageData: blob }));
                }
                setPreview(url);
                setError('');
            } catch (err) {
                setError('Could not load image');
                setPreview('');
            }
        } else {
            setPreview('');
            setFormData(prev => ({ ...prev, imageData: null }));
        }
    }, []);

    // Update handleFileChange to store both file and blob
    const handleFileChange = async (e) => {
        const file = e.target.files[0];
        if (file) {
            formModified.current = true;
            setFormData(prev => ({ 
                ...prev, 
                imageData: file,
                imageUrl: '' 
            }));
            setPreview(URL.createObjectURL(file));
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        // Show confirmation dialog if simulation is disabled
        if (!formData.isSimulation) {
            setShowConfirmation(true);
            return;
        }

        proceedWithSubmit();
    };

    const proceedWithSubmit = async () => {
        setShowConfirmation(false);
        setLoading(true);
        setError('');
        
        try {
            const requestData = {
                tokenAddress: sourceToken.tokenAddress,
                name: formData.name,
                symbol: formData.symbol,
                description: formData.description,
                twitter: formData.twitter || '',
                telegram: formData.telegram || '',
                website: formData.website || '',
                imageUrl: formData.imageUrl || preview || '',  // Use either direct URL or preview URL
                initialBuyAmount: parseFloat(formData.initialBuyAmount) || 0,
                isSimulation: formData.isSimulation
            };

            const response = await fetch('/api/tokens/create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to create token');
            }

            const result = await response.json();
            setSuccess(result);
            
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    const urlButtonRef = useCallback(node => {
        if (node) setImageUrlTarget(node);
    }, []);

    const handleRootClose = (e) => {
        if (e.target.closest('.image-url-popover')) {
            e.stopPropagation();
            return;
        }
        setShowImageUrlPopover(false);
    };

    const handleImageUrlSubmit = (e) => {
        e.preventDefault();
        e.stopPropagation();
        if (tempImageUrl) {
            handleImageUrlChange({ target: { value: tempImageUrl } });
            setTempImageUrl('');
            setShowImageUrlPopover(false);
        }
    };

    const ConfirmationDialog = () => (
        <Modal 
            show={showConfirmation} 
            onHide={() => setShowConfirmation(false)} 
            className={styles.confirmationDialog}
            backdrop="static"
            keyboard={false}
            centered
        >
            <Modal.Header closeButton>
                <Modal.Title className="text-danger">
                    <BsExclamationTriangle className="me-2" />
                    Warning
                </Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <p>You are about to create a token with real transactions. This action cannot be undone and will cost real SOL.</p>
                <p>Are you sure you want to proceed?</p>
            </Modal.Body>
            <Modal.Footer>
                <Button variant="secondary" onClick={() => setShowConfirmation(false)}>
                    Cancel
                </Button>
                <Button variant="danger" onClick={proceedWithSubmit}>
                    Yes, Create Token
                </Button>
            </Modal.Footer>
        </Modal>
    );

    return (
        <>
            <Modal 
                show={show} 
                onHide={handleHide} // Use handleHide instead of onHide
                centered 
                size="md"
                className={styles.createTokenModal} // Use the new CSS module class
            >
                <Modal.Header closeButton className="border-0 pb-0">
                    <Modal.Title className="h5">Create Token</Modal.Title>
                </Modal.Header>
                <Modal.Body className="pt-2">
                    <Form onSubmit={handleSubmit}>
                        <div className="d-flex gap-3">
                            <div className={styles.tokenFormMain}>
                                <Form.Group className="mb-2">
                                    <Form.Control
                                        type="text"
                                        name="name"
                                        value={formData.name}
                                        onChange={handleInputChange}
                                        placeholder="Token Name"
                                        required
                                        className="form-control-sm"
                                    />
                                </Form.Group>

                                <Form.Group className="mb-2">
                                    <Form.Control
                                        type="text"
                                        name="symbol"
                                        value={formData.symbol}
                                        onChange={handleInputChange}
                                        placeholder="Symbol"
                                        required
                                        className="form-control-sm"
                                    />
                                </Form.Group>

                                <Form.Group className="mb-2">
                                    <Form.Control
                                        as="textarea"
                                        name="description"
                                        value={formData.description}
                                        onChange={handleInputChange}
                                        placeholder="Description"
                                        rows={2}
                                        className="form-control-sm"
                                    />
                                </Form.Group>
                            </div>
                            
                            <div className={styles.imageSection}>
                                <div 
                                    className={`${styles.imagePreview} mb-2`} 
                                    onClick={() => document.getElementById('imageUpload').click()}
                                >
                                    {preview ? (
                                        <Image src={preview} alt="Token Preview" />
                                    ) : (
                                        <div className={styles.imagePlaceholder}>
                                            <BsUpload size={24} />
                                        </div>
                                    )}
                                </div>
                                <div className={styles.imageActions}>
                                    <input
                                        type="file"
                                        id="imageUpload"
                                        accept="image/*"
                                        onChange={handleFileChange}
                                        className="d-none"
                                    />
                                    <Button
                                        variant="outline-secondary"
                                        size="sm"
                                        className="w-100 mb-1"
                                        onClick={() => document.getElementById('imageUpload').click()}
                                    >
                                        Upload Image
                                    </Button>
                                    <Button
                                        ref={urlButtonRef}
                                        variant="outline-secondary"
                                        size="sm"
                                        className="w-100"
                                        onClick={() => setShowImageUrlPopover(true)}
                                    >
                                        <BsLink45Deg /> Image URL
                                    </Button>

                                    <Overlay
                                        show={showImageUrlPopover}
                                        target={imageUrlTarget}
                                        placement="bottom"
                                        rootClose={false}
                                        container={document.body}
                                    >
                                        <Popover 
                                            id="image-url-popover"
                                            className={styles.imageUrlPopover}
                                            onClick={(e) => e.stopPropagation()}
                                        >
                                            <Popover.Body>
                                                <Form 
                                                    onSubmit={handleImageUrlSubmit}
                                                    onClick={(e) => e.stopPropagation()}
                                                >
                                                    <div className="d-flex gap-2">
                                                        <Form.Control
                                                            type="url"
                                                            size="sm"
                                                            placeholder="Enter image URL"
                                                            value={tempImageUrl}
                                                            onChange={(e) => {
                                                                e.stopPropagation();
                                                                setTempImageUrl(e.target.value);
                                                            }}
                                                            autoFocus
                                                            onBlur={(e) => e.target.focus()}
                                                        />
                                                        <Button 
                                                            variant="primary" 
                                                            size="sm"
                                                            type="submit"
                                                        >
                                                            Add
                                                        </Button>
                                                    </div>
                                                </Form>
                                            </Popover.Body>
                                        </Popover>
                                    </Overlay>
                                </div>
                            </div>
                        </div>

                        <div className={styles.socialLinksContainer}>
                            <Form.Group className="mb-2">
                                <div className="input-group input-group-sm">
                                    <span className="input-group-text">
                                        <i className="bi bi-twitter"></i>
                                    </span>
                                    <Form.Control
                                        type="url"
                                        name="twitter"
                                        value={formData.twitter}
                                        onChange={handleInputChange}
                                        placeholder="Twitter URL"
                                        className="form-control-sm"
                                    />
                                </div>
                            </Form.Group>

                            <Form.Group className="mb-2">
                                <div className="input-group input-group-sm">
                                    <span className="input-group-text">
                                        <i className="bi bi-telegram"></i>
                                    </span>
                                    <Form.Control
                                        type="url"
                                        name="telegram"
                                        value={formData.telegram}
                                        onChange={handleInputChange}
                                        placeholder="Telegram URL"
                                        className="form-control-sm"
                                    />
                                </div>
                            </Form.Group>

                            <Form.Group className="mb-2">
                                <div className="input-group input-group-sm">
                                    <span className="input-group-text">
                                        <i className="bi bi-globe"></i>
                                    </span>
                                    <Form.Control
                                        type="url"
                                        name="website"
                                        value={formData.website}
                                        onChange={handleInputChange}
                                        placeholder="Website URL"
                                        className="form-control-sm"
                                    />
                                </div>
                            </Form.Group>
                        </div>

                        {error && (
                            <Alert variant="danger" className="py-2 mt-2 mb-0">
                                {error}
                            </Alert>
                        )}
                        {success && (
                            <Alert variant="success" className="py-2 mt-2 mb-0">
                                {formData.isSimulation 
                                    ? "Simulation was successful!"
                                    : <>
                                        Token created successfully!{' '}
                                        <Alert.Link 
                                            href={`https://pump.fun/coin/${success.tokenAddress}`} 
                                            target="_blank"
                                        >
                                            View on pump.fun
                                        </Alert.Link>
                                    </>
                                }
                            </Alert>
                        )}
                    </Form>
                </Modal.Body>
                <Modal.Footer className="border-0 pt-0">
                    <div className="w-100">
                        <div className={styles.simulationControls}>
                            <div className="d-flex justify-content-end align-items-center gap-3">
                                <div className={styles.buyAmountContainer}>
                                    <span className={styles.buyAmountLabel}>Buy Amount (SOL):</span>
                                    <Form.Control
                                        type="number"
                                        name="initialBuyAmount"
                                        value={formData.initialBuyAmount}
                                        onChange={handleInputChange}
                                        min="0"
                                        max="1"
                                        step="0.1"  // Change from 0.01 to 0.1
                                        className="form-control-sm"
                                    />
                                    {parseFloat(formData.initialBuyAmount) > 0.1 && (
                                        <div className={styles.textWarning} title="High initial buy amount">
                                            <i className="bi bi-exclamation-triangle"></i>
                                        </div>
                                    )}
                                </div>
                                <Form.Check
                                    type="checkbox"
                                    id="simulation-check"
                                    label="Simulation Mode"
                                    checked={formData.isSimulation}
                                    onChange={(e) => {
                                        formModified.current = true;
                                        setFormData(prev => ({
                                            ...prev,
                                            isSimulation: e.target.checked
                                        }));
                                        setShowConfirmation(false);  // Reset confirmation when changing simulation mode
                                    }}
                                />
                            </div>
                        </div>
                        <div className="d-flex justify-content-end gap-2">
                            <Button 
                                variant="secondary" 
                                size="sm" 
                                onClick={onHide}
                                disabled={loading}  // Add this line
                            >
                                Cancel
                            </Button>
                            <Button 
                                variant={formData.isSimulation ? "primary" : "danger"}
                                size="sm"
                                onClick={handleSubmit}
                                disabled={loading}
                            >
                                {loading ? 'Creating...' : 'Create Token'}
                            </Button>
                        </div>
                    </div>
                </Modal.Footer>
            </Modal>
            <ConfirmationDialog />
        </>
    );
}

export default CreateTokenModal;
