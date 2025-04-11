import { useState } from "react";
import httpClient from "../httpClient";
import PropTypes from "prop-types";

function File({ item, index }) {
  const [downloading, setDownloading] = useState(false);

  const handleDownload = (item) => {
    setDownloading(true);

    httpClient.get(`/downloadfile?fileId=${item.fileId}`, { responseType: "blob" })
      .then((res) => {
        const url = window.URL.createObjectURL(new Blob([res.data]));
        const link = document.createElement("a");
        link.href = url;
        link.setAttribute("download", item.fileName); //We already have the filename so no need to get it from backend response
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      })
      .catch((e) => {
        console.error("Download error:", e);
      })
      .finally(() => {
        setDownloading(false);
      });
  };

  return (
    <div
      key={index}
      className="bg-white rounded-lg overflow-hidden shadow-md hover:shadow-lg transform hover:scale-105 transition duration-300 flex flex-col"
    >
      <div className="relative w-full h-48 overflow-hidden">
        <img
          className="object-cover w-full h-full"
          src={item.thumbnail ? `data:image/png;base64,${item.thumbnail}` : "https://www.svgrepo.com/show/508699/landscape-placeholder.svg"}
          alt="File"
        />
      </div>

      <div className="flex flex-col flex-grow p-4">
        <h3 className="text-lg font-semibold mb-1 break-words">{item.fileName}</h3>
        <p className="text-gray-600 text-sm flex-grow">
          ID: {item.fileId}<br />
          Uploader: {item.uploaderName}<br />
          Uploaded: {new Date(item.uploadDate).toLocaleString()}
        </p>

        <button
          onClick={() => handleDownload(item)}
          disabled={downloading}
          className="mt-4 w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 rounded transition flex items-center justify-center"
        >
          {downloading ? (
            <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"></path>
            </svg>
          ) : (
            "Download"
          )}
        </button>
      </div>
    </div>
  );
}

File.propTypes = {
  item: PropTypes.shape({
    fileName: PropTypes.string.isRequired,
    fileId: PropTypes.number.isRequired,
    uploaderID: PropTypes.number,
    uploaderName: PropTypes.string.isRequired,
    uploadDate: PropTypes.string.isRequired,
    thumbnail: PropTypes.string,
  }).isRequired,
  index: PropTypes.number.isRequired,
};

export default File;
