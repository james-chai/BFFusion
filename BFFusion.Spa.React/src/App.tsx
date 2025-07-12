import { useState, useEffect } from 'react'
import axios from 'axios'
import './App.css'

interface Claim {
    type: string;
    value: string;
}

interface UserProfile {
    isAuthenticated: boolean;
    nameClaimType: string;
    roleClaimType: string;
    claims: Claim[];
}

function App() {
    const [azureApiData, setAzureApiData] = useState<string[]>([]);
    const [downstreamApiData, setDownstreamApiData] = useState<string[]>([]);
    const [userProfile, setUserProfile] = useState<UserProfile>({
        isAuthenticated: false,
        nameClaimType: '',
        roleClaimType: '',
        claims: []
    });

    useEffect(() => {
        console.info('App component mounted');
        getUserProfile();
    }, []);

    const getApiServerUri = () => {
        return "https://localhost:5001"; 
        //return `${window.location.protocol}//${window.location.host}`;
    };

    const getCurrentHost = () => {
        return `${window.location.protocol}//${window.location.host}`;
    };

    const getUserProfile = async () => {
        try {
            const response = await axios.get<UserProfile>(`${getApiServerUri()}/api/user`, { withCredentials: true });
            setUserProfile(response.data);
        } catch (error) {
            console.error('Error fetching user profile:', error);
        }
    };

    const getDirectApiData = async () => {
        try {
            const response = await axios.get<string[]>(`${getApiServerUri()}/api/direct-api`, { withCredentials: true });

            if (!Array.isArray(response.data)) {
                throw new Error('Invalid API response format');
            }

            setAzureApiData(response.data);
        } catch (error) {
            console.error('Error fetching direct API data:', error);
        }
    };

    const getDownstreamApiData = async () => {
        try {
            const response = await axios.get<string[]>(`${getApiServerUri()}/api/weather-forecasts`, { withCredentials: true });

            if (!Array.isArray(response.data)) {
                throw new Error('Invalid API response format');
            }

            setDownstreamApiData(response.data);
        } catch (error) {
            console.error('Error fetching downstream API data:', error);
        }
    };

    const login = () => {
        window.location.href = `${getApiServerUri()}/api/account/login?returnUrl=${encodeURIComponent(getCurrentHost())}`;
    };

    const logout = async () => {
        try {
            const redirectUrl = encodeURIComponent(getCurrentHost());

            const response = await fetch(`${getApiServerUri()}/api/account/logout?redirectUrl=${redirectUrl}`, {
                method: 'POST',
                credentials: 'include',  // important: send cookies!
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            // The response will be a 302 redirect from the BFF server
            // browsers don’t follow redirect automatically for fetch, so do it manually:
            if (response.redirected) {
                window.location.href = response.url;
            } else {
                // If no redirect, fallback to homepage or login page
                window.location.href = getCurrentHost();
            }
        } catch (error) {
            console.error('Logout failed:', error);
            window.location.href = getCurrentHost();
        }
    };

    return (
        <div className="app-container">
            <h1>BFFusion React App</h1>

            <div className="auth-section">
                <h2>User Profile</h2>
                <pre>{JSON.stringify(userProfile, null, 2)}</pre>
                <div className="button-group">
                    <button className="login-btn" onClick={login}>Login</button>
                    <button className="logout-btn" onClick={logout}>Logout</button>
                </div>
            </div>

            <div className="api-section">
                <h2>API Data</h2>
                <div className="button-group">
                    <button onClick={getDirectApiData}>Load Direct API Data</button>
                    <button onClick={getDownstreamApiData}>Load Downstream API Data</button>
                </div>

                <div className="data-display">
                    <h3>Direct API Data:</h3>
                    <ul>
                        {azureApiData?.map((item, index) => (
                            <li key={index}>{item}</li>
                        ))}
                    </ul>

                    <h3>Downstream API Data:</h3>
                    <ul>
                        {downstreamApiData?.map((item, index) => (
                            <li key={index}>
                                <pre>{JSON.stringify(item, null, 2)}</pre>
                                <strong>{item.date}</strong>: {item.summary}, {item.temperatureC}°C / {item.temperatureF}°F
                            </li>
                        ))}
                    </ul>
                </div>
            </div>
        </div>
    );
}

export default App