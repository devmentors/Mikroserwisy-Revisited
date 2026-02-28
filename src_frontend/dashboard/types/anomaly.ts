export enum AnomalyType {
    ConsumerErrorBeforeHandler = "ConsumerErrorBeforeHandler",
    ConsumerErrorAfterHandler = "ConsumerErrorAfterHandler",
    ConsumerDelayBeforeHandler = "ConsumerDelayBeforeHandler",
    ConsumerDelayAfterHandler = "ConsumerDelayAfterHandler",
    ProducerErrorBeforeTransaction = "ProducerErrorBeforeTransaction",
    ProducerErrorWithinTransaction = "ProducerErrorWithinTransaction",
    ProducerErrorAfterTransaction = "ProducerErrorAfterTransaction",
    OutboxErrorOnSave = "OutboxErrorOnSave",
    OutboxErrorOnPublish = "OutboxErrorOnPublish"
}

export interface EnableAnomalyRequest {
    anomalyType: AnomalyType;
    messageType: string;
    additionalParams: Map<string, string>;
}

export interface AnomalyDescription {
    anomalyType: string;
    messageType: string;
    params: Record<string, string>;
}

// Add translations
export const anomalyTypeLabels: Record<AnomalyType, string> = {
    ConsumerErrorBeforeHandler: "Błąd konsumenta przed handlerem",
    ConsumerErrorAfterHandler: "Błąd konsumenta po handlerze",
    ConsumerDelayBeforeHandler: "Opóźnienie konsumenta przed handlerem",
    ConsumerDelayAfterHandler: "Opóźnienie konsumenta po handlerze",
    ProducerErrorBeforeTransaction: "Błąd producenta przed transakcją",
    ProducerErrorWithinTransaction: "Błąd producenta w trakcie transakcji",
    ProducerErrorAfterTransaction: "Błąd producenta po transakcji",
    OutboxErrorOnSave: "Błąd outboxa podczas zapisu",
    OutboxErrorOnPublish: "Błąd outboxa podczas publikacji"
}; 