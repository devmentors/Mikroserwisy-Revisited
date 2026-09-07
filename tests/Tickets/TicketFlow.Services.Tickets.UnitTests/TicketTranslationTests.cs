using FluentAssertions;
using TicketFlow.Services.Tickets.Core.Data.Models;
using Xunit;

namespace TicketFlow.Services.Tickets.UnitTests;

public class TicketTranslationTests
{
    [Fact]
    public void The_first_translation_is_applied()
    {
        var ticket = NewTicket();

        var changed = ticket.SetTranslation("I cannot log in.");

        changed.Should().BeTrue();
        ticket.TranslatedDescription.Should().Be("I cannot log in.");
    }

    [Fact]
    public void The_same_translation_delivered_twice_changes_nothing()
    {
        var ticket = NewTicket();
        ticket.SetTranslation("I cannot log in.");
        var versionAfterFirst = ticket.Version;

        var changed = ticket.SetTranslation("I cannot log in.");

        changed.Should().BeFalse();
        ticket.Version.Should().Be(versionAfterFirst);
    }

    [Fact]
    public void A_different_translation_still_wins()
    {
        var ticket = NewTicket();
        ticket.SetTranslation("I cannot log in.");

        var changed = ticket.SetTranslation("I am unable to sign in.");

        changed.Should().BeTrue();
        ticket.TranslatedDescription.Should().Be("I am unable to sign in.");
    }

    [Fact]
    public void A_redelivered_translation_after_resolution_is_still_a_no_op()
    {
        var ticket = NewTicket();
        ticket.SetTranslation("I cannot log in.");
        ticket.Resolve("Password reset.");

        var changed = ticket.SetTranslation("I cannot log in.");

        changed.Should().BeFalse();
    }

    private static Ticket NewTicket()
        => new(Guid.NewGuid(), "person-token", "Nie moge sie zalogowac", "Mam problem z logowaniem.",
            TicketCategory.Other, "pl");
}
