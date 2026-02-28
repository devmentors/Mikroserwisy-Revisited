"use client";

import { Markdown } from "@copilotkit/react-ui";
import { FillerMessage } from "./filler-message";

interface Message {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
}

interface CustomAssistantMessageProps {
  message?: Message;
  isLoading?: boolean;
  isGenerating?: boolean;
  subComponent?: React.ReactNode;
}

export function CustomAssistantMessage({
  message,
  isLoading,
  isGenerating,
  subComponent,
}: CustomAssistantMessageProps) {
  const hasContent = message?.content && message.content.length > 0;

  // Show filler when loading and no content yet
  if (isLoading && !hasContent) {
    return (
      <div className="animate-fade-in">
        <FillerMessage category="thinking" />
      </div>
    );
  }

  // Normal rendering with streaming cursor
  return (
    <div className="animate-fade-in">
      {message && <Markdown content={message.content || ""} />}
      {isGenerating && (
        <span className="inline-block w-2 h-4 ml-0.5 bg-current animate-pulse" />
      )}
      {subComponent}
    </div>
  );
}
