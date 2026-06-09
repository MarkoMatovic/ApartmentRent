import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

/**
 * Legacy route — redirects to the main pricing page which contains all
 * subscription and purchase options (analytics, tokens, listings, boost, etc.).
 */
const SubscriptionPage: React.FC = () => {
    const navigate = useNavigate();

    useEffect(() => {
        navigate('/pricing', { replace: true });
    }, [navigate]);

    return null;
};

export default SubscriptionPage;
