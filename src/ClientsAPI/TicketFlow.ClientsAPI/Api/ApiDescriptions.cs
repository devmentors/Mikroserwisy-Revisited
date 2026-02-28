namespace TicketFlow.ClientsAPI.Api;

public static class ApiDescriptions
{
    public static class V1
    {
        public const string DocumentDescription = """
            External API for customers to submit and track their inquiries.

            ## Contract Guarantees

            This API follows strict versioning. Breaking changes are only introduced in new versions.

            ### V1 Contract:
            - Response fields are returned in specific order (id, title, status, createdAt, ...)
            - Dates are in ISO 8601 format WITHOUT milliseconds: `2026-01-04T12:00:00Z`
            - Status values are PascalCase: `New`, `InProgress`, `Resolved`
            - Empty list returns HTTP 200 with `[]`, not 204
            - Default limit: 100 items

            ### Hyrum's Law Notice
            All observable behaviors documented above are part of the contract.
            """;

        public const string SubmitInquiry = """
            Creates a new customer inquiry that will be processed by our support team.

            **Contract guarantees:**
            - Returns HTTP 201 Created on success
            - Response contains `id` field first, then `message`
            """;

        public const string GetInquiry = """
            Returns details of a specific inquiry.

            **Contract guarantees:**
            - Fields returned in order: id, title, status, createdAt, name, email, description, category, ticketId
            - Date format: `2026-01-04T12:00:00Z` (NO milliseconds)
            - Status is PascalCase: `New`, `InProgress`, `Resolved`
            """;

        public const string ListInquiries = """
            Returns all inquiries for the given email address.

            **Contract guarantees:**
            - Response fields in order: data, totalCount
            - Each item has fields in order: id, title, status, createdAt
            - Empty list returns HTTP 200 with `{ data: [], totalCount: 0 }`, NOT 204
            - Date format: `2026-01-04T12:00:00Z` (NO milliseconds)
            - Default limit: 100 items
            """;
    }

    public static class V2
    {
        public const string DocumentDescription = """
            External API for customers - Version 2 with improved response format.

            ## Breaking Changes from V1:
            - Response fields in alphabetical order (JSON default)
            - Dates include milliseconds: `2026-01-04T12:00:00.000Z`
            - Status values are lowercase: `new`, `inProgress`, `resolved`
            - Additional fields: `estimatedResponseTime`, `supportTier`
            """;

        public const string GetInquiry = """
            Returns details of a specific inquiry with additional fields.

            **Changes from V1:**
            - Fields in alphabetical order (JSON default)
            - Dates include milliseconds: `2026-01-04T12:00:00.000Z`
            - Status is lowercase: `new`, `inprogress`, `resolved`
            - New fields: `estimatedResponseTime`, `supportTier`

            **Hyrum's Law Example:**
            These changes don't break the schema, but WILL break clients that depend on:
            - Field ordering (regex parsing)
            - Exact date format (substring operations)
            - Status casing (string comparison without ToLower)
            """;

        public const string ListInquiries = """
            Returns all inquiries with improved pagination.

            **Changes from V1:**
            - `data` renamed to `items`
            - `totalCount` moved to `pagination.total`
            - Items have alphabetical field order
            - Dates include milliseconds
            """;

        public const string SubmitInquiry = "Creates a new customer inquiry. Response includes estimated response time.";
    }
}
