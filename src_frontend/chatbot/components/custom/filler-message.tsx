"use client";

import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";

const FILLER_MESSAGES = {
  thinking: [
    "Analizuje zapytanie...",
    "Przygotowuje odpowiedz...",
    "Przetwarzam informacje...",
    "Szukam najlepszego rozwiazania...",
    "Laczy sie z systemem...",
  ],
  toolExecution: [
    "Wykonuje operacje...",
    "Sprawdzam dane w systemie...",
    "Komunikuje sie z serwisem...",
    "Pobieram wymagane informacje...",
  ],
};

type FillerCategory = keyof typeof FILLER_MESSAGES;

interface FillerMessageProps {
  category?: FillerCategory;
}

export function FillerMessage({ category = "thinking" }: FillerMessageProps) {
  const messages = FILLER_MESSAGES[category];
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isVisible, setIsVisible] = useState(true);

  useEffect(() => {
    const interval = setInterval(() => {
      setIsVisible(false);

      setTimeout(() => {
        setCurrentIndex((prev) => (prev + 1) % messages.length);
        setIsVisible(true);
      }, 150);
    }, 2500);

    return () => clearInterval(interval);
  }, [messages.length]);

  return (
    <div className="flex items-center gap-2 text-muted-foreground">
      <Loader2 className="h-4 w-4 animate-spin" />
      <span
        className={`transition-opacity duration-150 ${
          isVisible ? "opacity-100" : "opacity-0"
        }`}
      >
        {messages[currentIndex]}
      </span>
    </div>
  );
}
