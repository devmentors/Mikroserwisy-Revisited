import { MiniSparkline } from "@/components/metrics/mini-sparkline";
import { ServiceStatus } from "@/types/services";
import { cn } from "@/lib/utils";
import { Wifi } from "lucide-react";

interface ServiceStatusListProps {
  services: ServiceStatus[];
  isConnected: boolean;
}

export function ServiceStatusList({ services, isConnected }: ServiceStatusListProps) {
  const getStatusColor = (status: ServiceStatus['status']) => {
    switch (status) {
      case 'Healthy':
        return 'bg-emerald-500';
      case 'Error':
        return 'bg-red-500';
      case 'Unknown':
      default:
        return 'bg-gray-500';
    }
  };

  const getChartColor = (status: ServiceStatus['status']) => {
    switch (status) {
      case 'Healthy':
        return '#10b981'; // emerald-500
      case 'Error':
        return '#ef4444'; // red-500
      case 'Unknown':
      default:
        return '#6b7280'; // gray-500
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 pb-2 text-sm text-muted-foreground">
        <Wifi className={cn(
          "h-4 w-4 transition-colors",
          isConnected ? "text-emerald-500" : "text-gray-500"
        )} />
        {isConnected ? "Połączono" : "Rozłączono"}
      </div>

      {(!services || !Array.isArray(services)) && (
        <div>Metryki nie są dostępne</div>
      )}

      {services.length === 0 && (
        <div>Brak dostępnych metryk</div>
      )}

      {services.map((service) => (
        <div key={service.name} className="flex items-center justify-between p-4 bg-background rounded-lg border">
          <div className="flex items-center gap-3">
            <div className={cn(
              "w-2.5 h-2.5 rounded-full",
              getStatusColor(service.status)
            )} />
            <div>
              <h3 className="font-medium">{service.name}</h3>
              <p className="text-sm text-muted-foreground">
                Ostatni pomiar: {new Date(service.lastChecked).toLocaleString('pl-PL', {
                  hour: '2-digit',
                  minute: '2-digit',
                  second: '2-digit',
                  hour12: false
                })}
              </p>
            </div>
          </div>
          {service.metrics && (
            <div className="flex gap-4">
              {service.metrics.cpu && (
                <div className="flex items-center gap-2">
                  <div className="w-24 h-8 border-gray-300">
                    <MiniSparkline 
                      data={service.metrics.cpu.history} 
                      color={getChartColor(service.status)}
                      width={96}
                      height={32}
                      maxValue={100}
                    />
                  </div>
                  <span className="text-sm">
                    {service.metrics.cpu.usage}%
                  </span>
                </div>
              )}
              {service.metrics.memory && (
                <div className="flex items-center gap-2">
                  <div className="w-24 h-8 border-gray-300">
                    <MiniSparkline 
                      data={service.metrics.memory.history}
                      color={getChartColor(service.status)}
                      width={96}
                      height={32}
                      maxValue={service.metrics.memory.total}
                    />
                  </div>
                  <span className="text-sm">
                    {service.metrics.memory.used}Mb
                  </span>
                </div>
              )}
            </div>
          )}
        </div>
      ))}
    </div>
  );
} 