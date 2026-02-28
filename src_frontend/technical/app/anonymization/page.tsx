"use client";

import { useState, useEffect } from "react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { API } from "@/lib/api-config";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faUserShield, faCheck, faTimes, faSpinner, faClock } from "@fortawesome/free-solid-svg-icons";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import {
  AnonymizationRequest,
  AnonymizationStatus,
  anonymizationStatusLabels,
  anonymizationStatusColors
} from "@/types/anonymization";

export default function AnonymizationPage() {
  const [requests, setRequests] = useState<AnonymizationRequest[]>([]);
  const [email, setEmail] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  const fetchRequests = async () => {
    try {
      const response = await fetch(`${API.anonymization}/anonymization-requests`);
      if (!response.ok) throw new Error("Failed to fetch anonymization requests");
      const data = await response.json();
      setRequests(data);
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : "Unknown error occurred";
      toast.error(`Failed to fetch requests: ${errorMessage}`);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchRequests();
    // Poll for updates every 5 seconds
    const interval = setInterval(fetchRequests, 5000);
    return () => clearInterval(interval);
  }, []);

  const submitRequest = async () => {
    if (!email.trim()) {
      toast.error("Podaj adres email");
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await fetch(`${API.anonymization}/anonymization-requests`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ email }),
      });

      if (!response.ok) {
        const errorData = await response.text();
        throw new Error(errorData || "Failed to create anonymization request");
      }

      toast.success("Zadanie anonimizacji zostalo utworzone");
      setEmail("");
      fetchRequests();
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : "Unknown error occurred";
      toast.error(`Blad: ${errorMessage}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const getStatusIcon = (status: AnonymizationStatus) => {
    switch (status) {
      case AnonymizationStatus.Completed:
        return <FontAwesomeIcon icon={faCheck} className="text-green-500" />;
      case AnonymizationStatus.Failed:
        return <FontAwesomeIcon icon={faTimes} className="text-red-500" />;
      case AnonymizationStatus.InProgress:
        return <FontAwesomeIcon icon={faSpinner} className="text-blue-500 animate-spin" />;
      default:
        return <FontAwesomeIcon icon={faClock} className="text-yellow-500" />;
    }
  };

  const formatDate = (dateString: string | null) => {
    if (!dateString) return "-";
    return new Date(dateString).toLocaleString("pl-PL");
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FontAwesomeIcon icon={faUserShield} />
            Nowe zadanie anonimizacji
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4">
            <Input
              type="email"
              placeholder="Adres email uzytkownika do anonimizacji"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="flex-1"
            />
            <Button onClick={submitRequest} disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <FontAwesomeIcon icon={faSpinner} className="mr-2 animate-spin" />
                  Przetwarzanie...
                </>
              ) : (
                "Utworz zadanie"
              )}
            </Button>
          </div>
          <p className="text-sm text-muted-foreground mt-2">
            Wprowadz adres email uzytkownika, ktorego dane maja zostac zanonimizowane zgodnie z GDPR.
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Historia zadan anonimizacji</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <FontAwesomeIcon icon={faSpinner} className="animate-spin text-2xl" />
            </div>
          ) : requests.length === 0 ? (
            <p className="text-muted-foreground text-center py-8">
              Brak zadan anonimizacji
            </p>
          ) : (
            <div className="space-y-4">
              {requests.map((request) => (
                <div key={request.id} className="border rounded-lg p-4">
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center gap-2">
                      {getStatusIcon(request.status)}
                      <span className="font-medium">{request.requestedByEmail}</span>
                    </div>
                    <Badge className={anonymizationStatusColors[request.status]}>
                      {anonymizationStatusLabels[request.status]}
                    </Badge>
                  </div>

                  <div className="text-sm text-muted-foreground space-y-1">
                    <p>
                      <span className="font-medium">Token:</span> {request.personToken}
                    </p>
                    <p>
                      <span className="font-medium">Utworzono:</span> {formatDate(request.createdAt)}
                    </p>
                    {request.completedAt && (
                      <p>
                        <span className="font-medium">Zakonczono:</span> {formatDate(request.completedAt)}
                      </p>
                    )}
                  </div>

                  {request.serviceStatuses && request.serviceStatuses.length > 0 && (
                    <>
                      <Separator className="my-3" />
                      <div className="space-y-2">
                        <p className="text-sm font-medium">Status uslug:</p>
                        <div className="grid grid-cols-3 gap-2">
                          {request.serviceStatuses.map((service) => (
                            <div
                              key={service.serviceName}
                              className={`p-2 rounded text-sm flex items-center gap-2 ${
                                service.completed
                                  ? "bg-green-100 dark:bg-green-900/20"
                                  : "bg-gray-100 dark:bg-gray-800"
                              }`}
                            >
                              {service.completed ? (
                                <FontAwesomeIcon icon={faCheck} className="text-green-500" />
                              ) : (
                                <FontAwesomeIcon icon={faClock} className="text-gray-400" />
                              )}
                              <span className="capitalize">{service.serviceName}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    </>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
