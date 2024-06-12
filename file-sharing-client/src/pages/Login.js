import { useState } from "react";
import httpClient from "../httpClient";
import { useNavigate } from 'react-router-dom'

function Login() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState('');
    const navigate = useNavigate()

    const handleSubmit = async () => {
        try {
            const response = await httpClient.post("/login", {
                username,
                password
            });

            if (response.status === 200) {
                setMessage("Login successful!");
                localStorage.setItem('token', response.data.token); 
                console.log(localStorage.getItem('token'));
                navigate('/');
            }
        } catch (error) {
            setMessage("Login failed: " + error.response.data);
        }
    };

    return (
        <div className="flex justify-center items-center h-screen">
            <div className="border-2 rounded w-60 h-auto p-5 justify-center items-center text-center">
                <h1 className="font-bold text-xl mb-4">Login</h1>
                {message && <div className="mb-4 text-red-500">{message}</div>}
                <div className="mb-4">
                    <label className="block mb-2">Username</label>
                    <input
                        className="border-2 w-full p-2"
                        onChange={(e) => setUsername(e.target.value)}
                        name="username"
                        type="text"
                        value={username}
                    />
                </div>
                <div className="mb-4">
                    <label className="block mb-2">Password</label>
                    <input
                        className="border-2 w-full p-2"
                        onChange={(e) => setPassword(e.target.value)}
                        name="password"
                        type="password"
                        value={password}
                    />
                </div>
                <button
                    className="border-2 rounded px-4 py-2 mt-4 bg-blue-500 text-white"
                    onClick={handleSubmit}
                >
                    Submit
                </button>
            </div>
        </div>
    );
}

export default Login;