import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { ServiceMetricsResponse, ServiceStatus } from "@/types/services";
import { useEffect, useRef, useState } from "react";
import { API } from '@/lib/api-config';

const HISTORY_LENGTH = 20;

export function useServices() {
  const [services, setServices] = useState<ServiceStatus[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const historyRef = useRef<Record<string, ServiceStatus>>({});
  const connectionRef = useRef<HubConnection>();

  useEffect(() => {
    if (connectionRef.current) {
      return;
    }

    const setupConnection = async () => {
      try {
        const connection = new HubConnectionBuilder()
          .withUrl(`${API.systemMetrics}/live-metrics`)
          .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: retryContext => {
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
          })
          .build();

        connectionRef.current = connection;

        connection.onclose(() => setIsConnected(false));
        connection.onreconnecting(() => setIsConnected(false));
        connection.onreconnected(() => setIsConnected(true));

        connection.on("MetricTick", (service: ServiceMetricsResponse) => {
          const previousService = historyRef.current[service.name];
          const newService: ServiceStatus = {
            ...service,
            metrics: service.metrics ? {
              cpu: {
                usage: service.metrics.cpu.usage,
                history: [
                  ...(previousService?.metrics?.cpu?.history || []),
                  { timestamp: service.lastChecked, value: service.metrics.cpu.usage }
                ].slice(-HISTORY_LENGTH)
              },
              memory: {
                used: service.metrics.memory.used,
                total: service.metrics.memory.total,
                history: [
                  ...(previousService?.metrics?.memory?.history || []),
                  { timestamp: service.lastChecked, value: service.metrics.memory.used }
                ].slice(-HISTORY_LENGTH)
              }
            } : undefined
          };
          
          historyRef.current[service.name] = newService;
          
          setServices(prevServices => {
            // Create a map of existing services for easier lookup
            const servicesMap = new Map(prevServices.map(s => [s.name, s]));
            // Update or add the new service
            servicesMap.set(service.name, newService);
            // Convert map back to array and sort by name for consistent ordering
            return Array.from(servicesMap.values()).sort((a, b) => 
              a.name.localeCompare(b.name)
            );
          });
        });

        await connection.start();
        setIsConnected(true);
      } catch (err) {
        console.error("Failed to establish connection:", err);
        connectionRef.current = undefined;
      }
    };

    const timer = setTimeout(() => {
      setupConnection();
    }, 100);

    return () => {
      clearTimeout(timer);
      if (connectionRef.current) {
        connectionRef.current.stop();
        connectionRef.current = undefined;
      }
    };
  }, []);

  return {
    services,
    isConnected
  };
}