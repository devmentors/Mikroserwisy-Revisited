type ApiMode = 'gateway' | 'direct';

const API_URLS: Record<ApiMode, {
  inquiries: string;
  tickets: string;
  communication: string;
  systemMetrics: string;
  sla: string;
  anonymization: string;
}> = {
  direct: {
    inquiries: 'http://localhost:5500',
    tickets: 'http://localhost:5400',
    communication: 'http://localhost:5600',
    systemMetrics: 'http://localhost:5900',
    sla: 'http://localhost:5700',
    anonymization: 'http://localhost:6200',
  },
  gateway: {
    inquiries: 'http://localhost:5100',
    tickets: 'http://localhost:5100',
    communication: 'http://localhost:5100',
    systemMetrics: 'http://localhost:5100',
    sla: 'http://localhost:5100',
    anonymization: 'http://localhost:6200',
  },
};

const mode = (process.env.NEXT_PUBLIC_API_MODE || 'gateway') as ApiMode;

export const API = API_URLS[mode];
export const API_MODE = mode;
