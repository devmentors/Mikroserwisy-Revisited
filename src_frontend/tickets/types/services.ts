// Server response type
export interface ServiceMetricsResponse {
  name: string;
  status: 'Healthy' | 'Error' | 'Unknown';
  lastChecked: string;
  metrics?: {
    cpu: {
      usage: number;
    };
    memory: {
      used: number;
      total: number;
    };
  };
}

// Client-side type with history
export interface ServiceStatus {
  name: string;
  status: 'Healthy' | 'Error' | 'Unknown';
  lastChecked: string;
  metrics?: {
    cpu: {
      usage: number;
      history: Array<{
        timestamp: string;
        value: number;
      }>;
    };
    memory: {
      used: number;
      total: number;
      history: Array<{
        timestamp: string;
        value: number;
      }>;
    };
  };
} 