"use client"

import { ApplicationTile } from "@/components/application-tile";
import { faServer, faDatabase, faTicket, faBug, faStopwatch, faComments } from "@fortawesome/free-solid-svg-icons";
import { API } from '@/lib/api-config';

export default function ApplicationsList() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      <ApplicationTile
        applicationName="Inquiries.Service"
        applicationBaseAddress={API.inquiries}
        icon={faBug}
      />
      <ApplicationTile
        applicationName="Tickets.Service"
        applicationBaseAddress={API.tickets}
        icon={faTicket}
      />
      <ApplicationTile
        applicationName="SLA"
        applicationBaseAddress={API.sla}
        icon={faStopwatch}
      />
      <ApplicationTile
        applicationName="Communication"
        applicationBaseAddress={API.communication}
        icon={faComments}
      />
    </div>
  );
}
