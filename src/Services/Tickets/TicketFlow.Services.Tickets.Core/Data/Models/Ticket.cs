using TicketFlow.Shared.Exceptions;

namespace TicketFlow.Services.Tickets.Core.Data.Models;

public sealed class Ticket
{
    public Guid Id { get; }
    public string PersonToken { get; }
    public string Title { get; private set; }
    public string Description { get; }
    public string LanguageCode { get; }
    public DateTimeOffset CreatedAt { get; }

    public TicketType? Type { get; private set; }
    public TicketStatus Status { get; private set; }
    public SeverityLevel? Severity { get; private set; }
    public TicketCategory Category { get; private set; } = TicketCategory.Other;
    public string? TranslatedDescription { get; private set; }
    public string? Notes { get; private set; }
    public string? InternalNotes { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset? DeadlineUtc { get; private set; }
    public string? Resolution { get; private set; }

    private bool _versionAlreadyChanged;
    
    public Agent? AssignedAgent { get; private set; }
    
    public bool IsEnglish => LanguageCode is "en";

    private Ticket()
    {
    }

    public Ticket(Guid id, string personToken, string title, string description, TicketCategory category, string languageCode)
    {
        Id = id;
        PersonToken = personToken;
        Title = title;
        Description = description;
        LanguageCode = languageCode;
        Status = TicketStatus.BeforeQualification;
        Category = category;
        CreatedAt = DateTimeOffset.UtcNow;
        Version = 1;
        _versionAlreadyChanged = true;
    }

    public void SetTranslation(string translatedDescription)
    {
        if (Status is TicketStatus.Resolved)
        {
            throw new TicketFlowException("Cannot set translation after the ticket is resolved.");
        }
        
        TranslatedDescription = translatedDescription;
        IncreaseVersion();
    }

    public void SetInternalNotes(string internalNotes)
    {
        if (Status is TicketStatus.Resolved)
        {
            throw new TicketFlowException("Cannot set notes after the ticket is resolved.");
        }
        
        InternalNotes = internalNotes;
        IncreaseVersion();
    }

    public void AddInternalNote(string newNote)
    {
        SetInternalNotes(InternalNotes + Environment.NewLine + $"[{DateTimeOffset.UtcNow.ToString("s")}] {newNote}");
    }

    public void AddClientNote(string newNote)
    {
        if (string.IsNullOrEmpty(Notes))
        {
            Notes = $"[{DateTimeOffset.UtcNow.ToString("O")}] {newNote}";
            return;
        }
        
        Notes += Environment.NewLine + $"[{DateTimeOffset.UtcNow.ToString("s")}] {newNote}";
    }

    public void AssignTo(Guid agentId)
    {
        if (Status is TicketStatus.BeforeQualification or TicketStatus.WaitingForScheduledAction or TicketStatus.Resolved)
        {
            throw new TicketFlowException("Cannot assign agent before qualification or after the ticket is resolved.");
        }
        
        if (AssignedTo is not null)
        {
            throw new TicketFlowException("Cannot reassign agent.");
        }
        
        AssignedTo = agentId;
        IncreaseVersion();
    }

    public void WaitForScheduledActions()
    {
        if (Status is TicketStatus.Resolved)
        {
            throw new TicketFlowException("Cannot set ticket for scheduled action after is resolved.");
        }
        
        Status = TicketStatus.WaitingForScheduledAction;
        IncreaseVersion();
    }
    
    public void SetBeforeQualification()
    {
        if (Status is TicketStatus.Resolved)
        {
            throw new TicketFlowException("Cannot set ticket as before qualification after is resolved.");
        }
        
        Status = TicketStatus.BeforeQualification;
        IncreaseVersion();
    }

    public void Resolve(string resolution)
    {
        if (Status is TicketStatus.Resolved)
        {
            throw new TicketFlowException("Cannot change ticket after is resolved.");
        }
        
        AddInternalNote($"RESOLVED BY: {AssignedAgent?.FullName}");
        Resolution = resolution;
        Status = TicketStatus.Resolved;
        IncreaseVersion();
    }

    public void Block(string? reason = null)
    {
        if (Status is TicketStatus.Resolved)
        {
            throw new TicketFlowException("Cannot change ticket after is resolved.");
        }
        
        Status = TicketStatus.Blocked;
        if (reason is not null)
        {
            AddInternalNote("BLOCKED: " + reason);
        }
        IncreaseVersion();
    }
    
    public void Unblock(string? reason = null)
    {
        if (Status is TicketStatus.Resolved)
        {
            throw new TicketFlowException("Cannot change ticket after is resolved.");
        }
        
        Status = TicketStatus.Qualified;
        if (reason is not null)
        {
            AddInternalNote("UNBLOCKED: " + reason);
        }
        IncreaseVersion();
    }
    
    public void Qualify(TicketType ticketType, SeverityLevel severityLevel)
    {
        if (Status is not TicketStatus.BeforeQualification)
        {
            throw new TicketFlowException("Ticket type must be before before qualification.");
        }
        
        Type = ticketType;
        Severity = severityLevel;
        Status = TicketStatus.Qualified;
        IncreaseVersion();
    }

    public void SetCalculatedDeadline(DateTimeOffset deadline)
    {
        DeadlineUtc = deadline;
        IncreaseVersion();
    }

    private void IncreaseVersion()
    {
        if (_versionAlreadyChanged)
        {
            return;
        }
        
        _versionAlreadyChanged = true;
        Version++;
    }
}