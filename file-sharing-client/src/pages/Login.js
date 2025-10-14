import { useState } from "react";
import httpClient from "../httpClient";
import { useNavigate } from 'react-router-dom';

function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [message, setMessage] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async () => {
    if (!username.trim() || !password.trim()) {
      setMessage("Please fill in all fields");
      return;
    }

    try {
      const response = await httpClient.post("/login", {
        username,
        password
      });

      if (response.status === 200) {
        setMessage("Login successful!");
        localStorage.setItem('token', response.data.token);
        localStorage.setItem('userName', response.data.userName);
        navigate('/');
      }
    } catch (error) {
      setMessage("Login failed: " + (error.response?.data || error.message));
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100 p-4">
      <div className="w-full max-w-sm bg-white rounded-lg shadow-lg p-6">
        <h1 className="text-3xl font-bold text-center text-blue-600 mb-6">Login</h1>

        {message && (
          <div className={`text-center p-3 mb-6 rounded ${message.includes('successful') ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
            {message}
          </div>
        )}

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Username</label>
            <input
              className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-400 focus:outline-none"
              onChange={(e) => setUsername(e.target.value)}
              name="username"
              type="text"
              value={username}
              placeholder="Enter your username"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
            <input
              className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-400 focus:outline-none"
              onChange={(e) => setPassword(e.target.value)}
              name="password"
              type="password"
              value={password}
              placeholder="Enter your password"
            />
          </div>

          <button
            className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 rounded-lg transition"
            onClick={handleSubmit}
          >
            Submit
          </button>
        </div>
      </div>
    </div>
  );
}

export default Login;
