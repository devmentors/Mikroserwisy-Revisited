// Dashboard uses BFF (Backend for Frontend)
export const BFF_URL = 'http://localhost:5200';

export const API = {
  // Base URL for backwards compatibility
  tickets: BFF_URL,
  // Scatter & Gather approach
  dashboard: `${BFF_URL}/manager/dashboard`,
  dashboardSearch: `${BFF_URL}/manager/dashboard/search`,
  // Event-Driven Projections approach
  aggregationProjections: `${BFF_URL}/aggregation/projections`,
  aggregationStatistics: `${BFF_URL}/aggregation/statistics`,
  aggregationSearch: `${BFF_URL}/aggregation/search`,
};
