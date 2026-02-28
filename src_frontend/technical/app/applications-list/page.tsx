"use client"

import { ApplicationTile } from "@/components/application-tile";
import { faServer, faDatabase, faTicket, faBug, faStopwatch, faComments } from "@fortawesome/free-solid-svg-icons";
import { DIRECT_API } from '@/lib/api-config';

export default function ApplicationsList() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      <ApplicationTile
        applicationName="Inquiries.Service"
        applicationBaseAddress={DIRECT_API.inquiries}
        icon={faBug}
      />
      <ApplicationTile
        applicationName="Tickets.Service"
        applicationBaseAddress={DIRECT_API.tickets}
        icon={faTicket}
      />
      <ApplicationTile
        applicationName="SLA"
        applicationBaseAddress={DIRECT_API.sla}
        icon={faStopwatch}
      />
      <ApplicationTile
        applicationName="Communication"
        applicationBaseAddress={DIRECT_API.communication}
        icon={faComments}
      />
    </div>
  );
}
