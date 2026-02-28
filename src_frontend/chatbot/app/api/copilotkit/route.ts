import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";

const CHATBOT_AGENT_URL = process.env.CHATBOT_AGENT_URL || "http://localhost:8000";

export const POST = async (req: Request) => {
  const userId = req.headers.get("X-User-Id");
  const userRole = req.headers.get("X-User-Role") || "client";
  const userEmail = req.headers.get("X-User-Email") || "unknown@example.com";
  const agentId = req.headers.get("X-Agent-Id");

  const userHeaders = {
    ...(userId && { "X-User-Id": userId }),
    "X-User-Role": userRole,
    "X-User-Email": userEmail,
    ...(agentId && { "X-Agent-Id": agentId }),
  };

  const runtime = new CopilotRuntime({
    agents: {
      "ticketflow-agent": new HttpAgent({
        url: CHATBOT_AGENT_URL,
        headers: userHeaders,
      }),
    },
  });

  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter: new ExperimentalEmptyAdapter(),
    endpoint: "/api/copilotkit",
  });

  return handleRequest(req);
};
