import { useState, useEffect, useRef } from 'react';
import httpClient from '../httpClient';
import File from '../component/File';
import { Link, useNavigate } from 'react-router-dom';

function Home() {
  const [file, setFile] = useState(null);
  const [data, setData] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(false);
  const fileInputRef = useRef(null);
  const isLoggedIn = localStorage.getItem('token') !== null;
  const navigate = useNavigate();

  const handleFileChange = (e) => {
    setFile(e.target.files[0]);
  };

  const filterData = (item) => {
    if (!searchTerm.trim()) return true;
  
    const term = searchTerm.trim().toLowerCase();
  
    return (
      item.fileName.toLowerCase().includes(term)
    );
  };

  const handleUpload = async () => {
    if (!file) {
      alert('Please select a file first.');
      return;
    }
    setLoading(true); 
  
    const formData = new FormData();
    formData.append('file', file);
  
    try {
      const token = localStorage.getItem('token');
  
      await httpClient.post('/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
          'Authorization': `Bearer ${token}`,
        },
      });
  
      alert('File uploaded successfully!');
      setFile(null);
      fileInputRef.current.value = '';
      getFiles();
    } catch (error) {
      console.error('Error uploading file:', error);
      alert('Upload failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const getFiles = () => {
    httpClient.get('/files').then((res) => {
      setData(res.data);
    });
  };

  useEffect(() => {
    getFiles();
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/');
    window.location.reload();
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <header className="flex justify-between items-center mb-10">
        <h1 className="text-4xl sm:text-5xl font-bold text-blue-700">File Share</h1>

        {isLoggedIn ? (
          <button
            onClick={handleLogout}
            className="text-sm text-white bg-red-500 hover:bg-red-600 px-4 py-2 rounded shadow transition"
          >
            Logout
          </button>
        ) : (
          <div className="flex gap-4">
            <Link to="/register">
              <button className="text-sm border-2 border-blue-500 text-blue-500 hover:bg-blue-500 hover:text-white px-4 py-2 rounded transition">
                Register
              </button>
            </Link>
            <Link to="/login">
              <button className="text-sm border-2 border-blue-500 text-blue-500 hover:bg-blue-500 hover:text-white px-4 py-2 rounded transition">
                Login
              </button>
            </Link>
          </div>
        )}
      </header>

      <section className="bg-white p-6 rounded-lg shadow-md max-w-2xl mx-auto">
        <label className="block mb-2 text-lg font-medium text-gray-700">
          Upload a file
        </label>
        <input
          type="file"
          ref={fileInputRef}
          onChange={handleFileChange}
          className="block w-full mb-4 text-sm text-gray-600 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-100 file:text-blue-700 hover:file:bg-blue-200"
        />
        <button
          onClick={handleUpload}
          disabled={loading}
          className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 px-4 rounded transition flex items-center justify-center"
        >
          {loading ? (
            <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"></path>
            </svg>
          ) : (
            'Upload'
          )}
        </button>

      </section>

      <section className="mt-10">
        <h2 className="text-2xl font-semibold mb-6 text-center text-gray-800">Uploaded Files</h2>

        <div className="flex justify-center mb-6">
          <input
            type="text"
            placeholder="Search files..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full max-w-md border border-gray-300 rounded-lg px-4 py-2 text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        {data
          .filter(filterData)
          .map((item, index) => (
            <File key={index} index={index} item={item} />
        ))}
        </div>
      </section>
    </div>
  );
}

export default Home;
