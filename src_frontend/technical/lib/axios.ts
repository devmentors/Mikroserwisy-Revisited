import axios from 'axios';
import { API } from '@/lib/api-config';

// Create axios instance with default config
const api = axios.create({
  baseURL: API.tickets,
  headers: {
    'Content-Type': 'application/json',
  },
});

export default api; 