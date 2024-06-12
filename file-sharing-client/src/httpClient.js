import axios from 'axios';

const httpClient = axios.create({
  baseURL: 'http://localhost:5166',
});

export default httpClient;
