import httpClient from "../httpClient";
import PropTypes from 'prop-types';

function File(props) {
    let item = props.item;
    let index = props.index;

    const handleDownload = (fileName) => {
        console.log("handledownloadfunction");
        console.log(fileName);
        httpClient.get(`/downloadfile?fileName=${fileName}`)
        .then((res) => {
          console.log(res)
          const { fileContents, fileDownloadName } = res.data;
    
          const link = document.createElement('a');
          link.href = `data:application/octet-stream;base64,${fileContents}`;
          link.download = fileDownloadName;
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
        })
        .catch((e) => {
          console.log(e);
        });
      };

    return (
        <>
          <div key={index} className="max-w-80 rounded overflow-hidden shadow-lg bg-white">
            <img className="w-full" src="https://www.svgrepo.com/show/508699/landscape-placeholder.svg" alt="File" />
            <div className="px-6 py-4">
              <div className="font-bold text-xl mb-2">{item.fileName}</div>
              <p className="text-gray-700 text-base">
                file_Id: {item.fileId}, {item.uploaderID !== -1 ? `uploaderID: ${item.uploaderID},` : ''} uploadDate: {new Date(item.uploadDate).toLocaleString()}
              </p>
            </div>
            <div className="px-6 pt-4 pb-2">
              <button 
                className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded" 
                onClick={() => handleDownload(item.fileName)}
              >
                Download
              </button>
            </div>
          </div>
        </>
    );
}

File.propTypes = {
    item: PropTypes.shape({
        fileName: PropTypes.string.isRequired,
        fileId: PropTypes.number.isRequired,
        uploaderID: PropTypes.number,
        uploadDate: PropTypes.string.isRequired,
    }).isRequired,
    index: PropTypes.number.isRequired,
};

export default File;