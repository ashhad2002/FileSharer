import { useState, useEffect} from 'react';
import httpClient from '../httpClient';
import File from '../component/File';
import { Link } from 'react-router-dom'


function Home() {
  const [file, setFile] = useState(null);
  const [data, setData] = useState([]);
  const isLoggedIn = localStorage.getItem('token') !== null;

  const handleFileChange = (e) => {
    setFile(e.target.files[0]);
  };

  const handleUpload = async () => {
    console.log("Uploading...");
    if (!file) {
      alert('Please select a file first.');
      return;
    }

    const formData = new FormData();
    formData.append('file', file);

    try {
      console.log(data);
      await httpClient.post('/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });
      getFiles();
    } catch (error) {
      console.error('Error uploading file:', error);
    }
  };

  const getFiles = () => {
    httpClient.get('/files')
    .then((res) => {
      console.log(res.data)
      setData(res.data);
    });
  };

  useEffect(() => {
    getFiles();
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
};

  return (
    <>
      <h1 className="text-9xl font-bold text-center">
        File Share
      </h1>

      {isLoggedIn ? (
          <p className="absolute top-0 right-5">You are logged in. <a onClick={() => handleLogout()}>logout</a></p>
      ) : (
          <div className="absolute top-0 right-5">
            <Link to="/register">
                <button className="border-2 rounded px-4 py-2 mt-2 bg-blue-500 text-white">
                    Register
                </button>
            </Link>
            OR
            <Link to="/login">
                <button className="border-2 rounded px-4 py-2 mt-2 bg-blue-500 text-white">
                    Login
                </button>
            </Link>
          </div>
      )}

      <label className="ml-1 block mb-2 mt-3 text-sm font-medium text-gray-900">
        Upload file
      </label>
      <div className='w-3/4'>
        <input className="ml-1 block w-full text-sm text-gray-900 border border-gray-300 rounded-lg cursor-pointer bg-gray-50 dark:text-gray-400 focus:outline-none dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400" id="file_input" type="file" onChange={handleFileChange} />
      </div>
      <button className="ml-1 block mt-1 text-sm text-gray-900 border border-gray-300 rounded-sm cursor-pointer bg-gray-50 dark:text-white focus:outline-none dark:bg-gray-700 dark:border-gray-600 px-3" onClick={() => handleUpload()}>Upload</button>
      
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6 mt-5">
        {data.map((item, index) => (
          <File key={index} index={index} item={item} />
        ))}
      </div>
      
    </>
  );
}

export default Home;