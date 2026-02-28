"use client";

import { useState, useEffect } from "react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { toast } from "sonner";
import { AnomalyType, EnableAnomalyRequest, AnomalyDescription } from "@/types/anomaly";
import { IconDefinition } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { anomalyTypeLabels } from "@/types/anomaly";
import { Separator } from "@/components/ui/separator";
import { MESSAGE_TYPES } from "@/types/message";
import { faEnvelope } from "@fortawesome/free-solid-svg-icons";

interface ApplicationTileProps {
    applicationName: string;
    applicationBaseAddress: string;
    icon?: IconDefinition;
}

export function ApplicationTile({ applicationName, applicationBaseAddress, icon }: ApplicationTileProps) {
    const [enabledAnomalies, setEnabledAnomalies] = useState<AnomalyDescription[]>([]);
    const [newAnomaly, setNewAnomaly] = useState<Partial<EnableAnomalyRequest>>({
        anomalyType: undefined,
        messageType: "",
        additionalParams: new Map()
    });
    const [paramKey, setParamKey] = useState<string>('StatusFilter');
    const [paramValue, setParamValue] = useState<string>('');

    const fetchAnomalies = async () => {
        try {
            const response = await fetch(`${applicationBaseAddress}/anomalies`);
            if (!response.ok) throw new Error('Failed to fetch anomalies');
            const data = await response.json();
            setEnabledAnomalies(data);
        } catch (error: unknown) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
            toast.error(`Failed to fetch anomalies: ${errorMessage}`);
        }
    };

    useEffect(() => {
        fetchAnomalies();
    }, [applicationBaseAddress]);

    const enableAnomaly = async () => {
        if (!newAnomaly.anomalyType || !newAnomaly.messageType) {
            toast.error("Please fill in all required fields");
            return;
        }

        try {
            const requestBody = {
                ...newAnomaly,
                additionalParams: newAnomaly.additionalParams ? Object.fromEntries(newAnomaly.additionalParams) : {}
            };

            const response = await fetch(`${applicationBaseAddress}/anomalies`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(requestBody),
            });

            if (!response.ok) throw new Error('Failed to enable anomaly');
            
            toast.success("Anomaly enabled successfully");
            fetchAnomalies();
            
            // Reset all form states
            setNewAnomaly({
                anomalyType: undefined,
                messageType: "",
                additionalParams: new Map()
            });
            setParamKey('StatusFilter');
            setParamValue('');
            
        } catch (error: unknown) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
            toast.error(`Failed to enable anomaly: ${errorMessage}`);
        }
    };

    const disableAnomaly = async (anomalyType: string, messageType: string) => {
        try {
            const response = await fetch(
                `${applicationBaseAddress}/anomalies/${anomalyType}/messages/${messageType}`,
                { method: 'DELETE' }
            );

            if (!response.ok) throw new Error('Failed to disable anomaly');
            
            toast.success("Anomaly disabled successfully");
            fetchAnomalies();
        } catch (error: unknown) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
            toast.error(`Failed to disable anomaly: ${errorMessage}`);
        }
    };

    return (
        <Card className="w-full">
            <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-2xl pb-2">
                    {icon && <FontAwesomeIcon icon={icon} className="h-6 w-6" />}
                    {applicationName}
                </CardTitle>
                <Separator className="mt-2" />
            </CardHeader>
            <div className="mt-2">
                <CardContent>
                    <div className="space-y-2">
                        <h4 className="text-sm font-semibold">Włącz nową anomalię</h4>
                        
                        <div className="flex gap-4 mb-2">
                            <Select
                                value={newAnomaly.anomalyType}
                                onValueChange={(value) => 
                                    setNewAnomaly(prev => ({ ...prev, anomalyType: value as AnomalyType }))
                                }
                            >
                                <SelectTrigger>
                                    <SelectValue placeholder="Wybierz typ anomalii" />
                                </SelectTrigger>
                                <SelectContent>
                                    {Object.values(AnomalyType).map((type) => (
                                        <SelectItem key={type} value={type}>
                                            {anomalyTypeLabels[type]}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>

                            <Select
                                value={newAnomaly.messageType}
                                onValueChange={(value) => 
                                    setNewAnomaly(prev => ({ ...prev, messageType: value }))
                                }
                            >
                                <SelectTrigger>
                                    <SelectValue placeholder="Wybierz typ wiadomości" />
                                </SelectTrigger>
                                <SelectContent>
                                    {MESSAGE_TYPES.map((type) => (
                                        <SelectItem key={type} value={type}>
                                            {type}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="flex-1 border rounded-lg p-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <label className="text-sm font-medium">Parameter</label>
                                    <Select
                                        value={paramKey}
                                        onValueChange={setParamKey}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Wybierz parametr" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="StatusFilter">Filtrowanie po statusie</SelectItem>
                                            <SelectItem 
                                                value="DelayInMs" 
                                                disabled={!(newAnomaly.anomalyType === AnomalyType.ConsumerDelayBeforeHandler || 
                                                           newAnomaly.anomalyType === AnomalyType.ConsumerDelayAfterHandler)}
                                                title={!(newAnomaly.anomalyType === AnomalyType.ConsumerDelayBeforeHandler || 
                                                        newAnomaly.anomalyType === AnomalyType.ConsumerDelayAfterHandler) 
                                                    ? "Dostępne tylko dla anomalii opóźniających" 
                                                    : undefined}
                                            >
                                                Opóźnienie (ms)
                                            </SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>

                                <div className="space-y-2">
                                    <label className="text-sm font-medium">Wartość</label>
                                    <div className="flex gap-2">
                                        <Input
                                            type="text"
                                            placeholder="Wartość parametru"
                                            value={paramValue}
                                            onChange={(e) => setParamValue(e.target.value)}
                                        />
                                        <Button 
                                            variant="secondary"
                                            onClick={() => {
                                                if (paramKey && paramValue) {
                                                    setNewAnomaly(prev => {
                                                        const newParams = new Map(prev.additionalParams);
                                                        newParams.set(paramKey, paramValue);
                                                        return { ...prev, additionalParams: newParams };
                                                    });
                                                    setParamValue('');
                                                }
                                            }}
                                        >
                                            Dodaj
                                        </Button>
                                    </div>
                                </div>
                            </div>

                            {/* Display added parameters */}
                            {newAnomaly.additionalParams && newAnomaly.additionalParams.size > 0 && (
                                <div className="mt-4">
                                    <label className="text-sm font-medium">Dodane parametry:</label>
                                    <div className="mt-2 space-y-2">
                                        {Array.from(newAnomaly.additionalParams).map(([key, value]) => (
                                            <div key={key} className="flex items-center justify-between bg-secondary/20 p-2 rounded-md">
                                                <span className="text-sm">
                                                    {key}: {value}
                                                </span>
                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    onClick={() => {
                                                        setNewAnomaly(prev => {
                                                            const newParams = new Map(prev.additionalParams);
                                                            newParams.delete(key);
                                                            return { ...prev, additionalParams: newParams };
                                                        });
                                                    }}
                                                >
                                                    Usuń
                                                </Button>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>

                        <Button 
                            className="w-full mt-4"
                            onClick={enableAnomaly}
                        >
                            Włącz anomalię
                        </Button>
                    </div>

                    <Separator className="my-6" />

                    <div className="space-y-2">
                        <h4 className="text-sm font-semibold">Włączone anomalie</h4>
                        {enabledAnomalies.length === 0 ? (
                            <p className="text-sm text-muted-foreground">Brak włączonych anomalii</p>
                        ) : (
                            <div className="space-y-2">
                                {enabledAnomalies.map((anomaly, index) => (
                                    <div key={index} className="flex items-center justify-between p-2 border rounded-lg">
                                        <div>
                                            <p className="font-medium">{anomalyTypeLabels[anomaly.anomalyType as AnomalyType] || anomaly.anomalyType}</p>
                                            <p className="text-sm text-muted-foreground">
                                                <FontAwesomeIcon icon={faEnvelope} className="mr-2" />
                                                {anomaly.messageType}
                                            </p>
                                            {Object.entries(anomaly.params).map(([key, value]) => (
                                                <p key={key} className="text-sm text-muted-foreground">
                                                    {key}: {value}
                                                </p>
                                            ))}
                                        </div>
                                        <Button
                                            variant="destructive"
                                            size="sm"
                                            onClick={() => disableAnomaly(anomaly.anomalyType, anomaly.messageType)}
                                        >
                                            Wyłącz
                                        </Button>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </CardContent>
            </div>
        </Card>
    );
} 