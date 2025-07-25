import { useState, useEffect, useRef } from 'react';
import httpClient from '../httpClient';
import File from '../component/File';
import { Link, useNavigate } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCloudUploadAlt, faFolderOpen, faCheckCircle } from '@fortawesome/free-solid-svg-icons';


function Home() {
  // const [file, setFile] = useState(null);
  const [data, setData] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  // const [loading, setLoading] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const [uploadQueue, setUploadQueue] = useState([]);
  const [toast, setToast] = useState('');

  const fileInputRef = useRef(null);
  const isLoggedIn = localStorage.getItem('token') !== null;
  const navigate = useNavigate();

  const handleFiles = (files) => {
    Array.from(files).forEach((file) => {
      const newQueue = [...uploadQueue, { name: file.name, progress: 0 }];
      setUploadQueue(newQueue);

      const formData = new FormData();
      formData.append('file', file);

      const token = localStorage.getItem('token');

      const config = {
        headers: {
          'Content-Type': 'multipart/form-data',
          'Authorization': `Bearer ${token}`,
        },
        onUploadProgress: (progressEvent) => {
          const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          setUploadQueue((prev) =>
            prev.map((f) => (f.name === file.name ? { ...f, progress: percent } : f))
          );
        }
      };

      httpClient.post('/upload', formData, config)
        .then(() => {
          showToast(`${file.name} uploaded successfully!`);
          getFiles();
        })
        .catch((err) => {
          console.error(err);
          alert(`Failed to upload ${file.name}`);
        })
        .finally(() => {
          setUploadQueue((prev) => prev.filter((f) => f.name !== file.name));
        });
    });
  };

  const showToast = (message) => {
    setToast(message);
    setTimeout(() => setToast(''), 3000);
  };

  const filterData = (item) => {
    if (!searchTerm.trim()) return true;
  
    const term = searchTerm.trim().toLowerCase();
  
    return (
      item.fileName.toLowerCase().includes(term)
    );
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

      <section className="bg-white p-6 rounded-lg shadow-md max-w-2xl mx-auto mb-10">
        <label className="block mb-4 text-lg font-medium text-gray-700">Upload a file</label>

        <div
          className={`border-2 border-dashed rounded-lg p-12 text-center cursor-pointer transition ${dragOver ? 'border-blue-500 bg-blue-50' : 'border-gray-300'}`}
          onDragOver={(e) => {
            e.preventDefault();
            setDragOver(true);
          }}
          onDragLeave={() => setDragOver(false)}
          onDrop={(e) => {
            e.preventDefault();
            setDragOver(false);
            if (e.dataTransfer.files?.length) handleFiles(e.dataTransfer.files);
          }}
          onClick={() => fileInputRef.current?.click()}
        >
          <div className="flex flex-col items-center justify-center">
            <FontAwesomeIcon icon={faCloudUploadAlt} className="text-5xl text-blue-500 mb-4" />
            <p className="text-lg font-medium text-gray-700 mb-2">Drag & drop files here</p>
            <p className="text-gray-500 mb-4">or</p>
            <button className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-full">
              <FontAwesomeIcon icon={faFolderOpen} className="mr-2" /> Browse Files
            </button>
            <input type="file" ref={fileInputRef} multiple onChange={(e) => handleFiles(e.target.files)} className="hidden" />
          </div>
        </div>

        {uploadQueue.length > 0 && (
          <div className="mt-6">
            <h3 className="text-lg font-medium text-gray-700 mb-4">Uploading files...</h3>
            <div className="space-y-4">
              {uploadQueue.map(({ name, progress }) => (
                <div key={name}>
                  <div className="flex justify-between text-sm text-gray-700 mb-1">
                    <span className="truncate" style={{ maxWidth: '70%' }}>{name}</span>
                    <span>{Math.round(progress)}%</span>
                  </div>
                  <div className="h-3 bg-gray-200 rounded-full overflow-hidden">
                    <div className="h-full bg-blue-500 rounded-full transition-all duration-300" style={{ width: `${progress}%` }}></div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </section>

      {toast && (
        <div className="fixed bottom-6 right-6 bg-green-500 text-white px-6 py-3 rounded-lg shadow-lg flex items-center transition-all duration-300">
          <FontAwesomeIcon icon={faCheckCircle} className="mr-2" />
          <span>{toast}</span>
        </div>
      )}

      
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
