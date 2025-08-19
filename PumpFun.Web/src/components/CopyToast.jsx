import React from 'react';
import { Toast } from 'react-bootstrap';

const CopyToast = ({ show, onClose, message = "Address copied", style = {} }) => (
    <Toast 
        show={show} 
        onClose={onClose} 
        delay={3000} 
        autohide
        style={{
            position: 'fixed',
            top: '20px',
            right: '20px',
            zIndex: 9999,
            background: 'rgba(33, 37, 41, 0.95)',
            color: 'white',
            borderRadius: '8px',
            boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
            border: '1px solid rgba(255, 255, 255, 0.1)',
            ...style
        }}
    >
        <Toast.Body style={{ 
            padding: '8px 12px',
            fontSize: '14px'
        }}>
            {message}
        </Toast.Body>
    </Toast>
);

export default CopyToast;
