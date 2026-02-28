import { Badge } from "@/components/ui/badge";
import { LineChart, Clock, UserPlus, Bell, Trash2, Check } from "lucide-react";
import { useAlerts } from "@/hooks/use-alerts";
import { Alert } from "@/types/alert";
import { cn } from "@/lib/utils";
import { useState } from "react";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";

const getAlertBadgeProps = (type: Alert['type']) => {
  switch (type) {
    case 'SLA':
      return { icon: <Clock className="h-3 w-3" />, text: 'SLA niespełnione' };
    case 'Incydent':
      return { icon: <UserPlus className="h-3 w-3" />, text: 'Nowy incydent' };
    default:
      return { icon: <Clock className="h-3 w-3" />, text: 'Alert' };
  }
};


interface AlertItemProps {
  alert: Alert;
  onDelete: (id: string) => void;
}

function AlertItem({ alert, onDelete }: AlertItemProps) {
  return (
    <div className="rounded-lg border p-4 relative">
      {!alert.isRead && (
        <div className="absolute top-2 right-2 p-2">
          <Check 
            className="h-4 w-4 text-muted-foreground hover:text-primary cursor-pointer" 
            onClick={() => onDelete(alert.id)}
          />
        </div>
      )}
      <div className="flex items-center gap-2 mb-1">
        <Badge variant="outline" className="flex items-center gap-1">
          {getAlertBadgeProps(alert.type).icon}
          {getAlertBadgeProps(alert.type).text}
        </Badge>
        {!alert.isRead && (
          <Badge variant="secondary" className="bg-primary text-primary-foreground">
            Nowy
          </Badge>
        )}
      </div>
      <p className="text-sm text-muted-foreground">{alert.message}</p>
    </div>
  );
}

export function AlertIndicator() {
  const { data: alerts } = useAlerts();
  const unreadCount = alerts?.filter(alert => !alert.isRead).length ?? 0;

  return (
    <div className="relative w-[26px] mr-1.5">
      <Bell 
        className={cn(
          "h-6 w-6",
          unreadCount > 0 && "text-red-500 vibrate"
        )} 
      />
      {unreadCount > 0 && (
        <div className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-5 h-5 flex items-center justify-center text-xs font-bold">
          {unreadCount > 99 ? '99+' : unreadCount}
        </div>
      )}
    </div>
  );
}

export function AlertList() {
  const [showOnlyUnread, setShowOnlyUnread] = useState(() => 
    localStorage.getItem('alertsOnlyUnread') === 'true'
  );
  const { data: alerts, isLoading, changeIsRead } = useAlerts(showOnlyUnread);

  const handleSwitchChange = (checked: boolean) => {
    setShowOnlyUnread(checked);
    localStorage.setItem('alertsOnlyUnread', checked.toString());
  };

  return (
    <>
      <div className="flex items-center space-x-2 mb-4">
        <Switch
          id="unread-mode"
          checked={showOnlyUnread}
          onCheckedChange={handleSwitchChange}
        />
        <Label htmlFor="unread-mode">Pokaż tylko nieprzeczytane</Label>
      </div>
      <div className="space-y-4">
        {alerts?.map((alert) => (
          <AlertItem 
            key={alert.id} 
            alert={alert} 
            onDelete={() => changeIsRead(alert.id, true)}
          />
        ))}
      </div>
    </>
  );
} 