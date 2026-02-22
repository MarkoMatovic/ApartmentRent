using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander;
using Lander.src.Modules.Communication.Implementation;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

namespace LandlordApp.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly CommunicationsContext _context;
    private readonly UsersContext _usersContext;
    private readonly ReportService _service;

    // A valid MessageId that exists in the InMemory Messages table
    private const int SeedMessageId = 100;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<CommunicationsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var usersOptions = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new CommunicationsContext(options);
        _usersContext = new UsersContext(usersOptions);
        _service = new ReportService(_context, _usersContext);

        SeedRequiredData();
    }

    private void SeedRequiredData()
    {
        // Seed 1 Message so ReportedMessage.MessageId FK is satisfied
        _context.Messages.Add(new Message
        {
            MessageId   = SeedMessageId,
            MessageText = "Reported message content",
            SenderId    = 1,
            ReceiverId  = 2
        });

        // Seed users referenced by ReportedMessages (for username resolution)
        _usersContext.Users.AddRange(
            new User { UserId = 1, FirstName = "Reporter", LastName = "A", Email = "a@t.com", Password = "x" },
            new User { UserId = 2, FirstName = "Reported", LastName = "B", Email = "b@t.com", Password = "x" }
        );

        _context.SaveChanges();
        _usersContext.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _usersContext.Database.EnsureDeleted();
        _context.Dispose();
        _usersContext.Dispose();
    }

    // Helper: create a valid ReportedMessage referencing the seeded MessageId
    private static ReportedMessage MakeReport(int id, string status = "Pending") =>
        new ReportedMessage
        {
            ReportId         = id,
            MessageId        = SeedMessageId,
            ReportedByUserId = 1,
            ReportedUserId   = 2,
            Reason           = "Spam",
            Status           = status,
            CreatedDate      = DateTime.UtcNow
        };

    [Fact]
    public async Task GetAllReportsAsync_NoFilter_ShouldReturnAll()
    {
        _context.ReportedMessages.AddRange(MakeReport(1, "Pending"), MakeReport(2, "Resolved"));
        await _context.SaveChangesAsync();

        var all = await _service.GetAllReportsAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllReportsAsync_FilterByStatus_ShouldReturnCorrectSubset()
    {
        _context.ReportedMessages.AddRange(MakeReport(1, "Pending"), MakeReport(2, "Resolved"));
        await _context.SaveChangesAsync();

        var pending = await _service.GetAllReportsAsync("Pending");
        pending.Should().HaveCount(1);
        pending[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task ReviewReportAsync_Existent_ShouldUpdateStatus()
    {
        _context.ReportedMessages.Add(MakeReport(10, "Pending"));
        await _context.SaveChangesAsync();

        var success = await _service.ReviewReportAsync(10, new UpdateReportStatusDto { AdminNotes = "Looks bad" }, 99);
        success.Should().BeTrue();

        var report = await _context.ReportedMessages.FindAsync(10);
        report!.Status.Should().Be("Reviewed");
        report.ReviewedByAdminId.Should().Be(99);
        report.AdminNotes.Should().Be("Looks bad");
    }

    [Fact]
    public async Task ResolveReportAsync_Existent_ShouldSetResolved()
    {
        _context.ReportedMessages.Add(MakeReport(20, "Reviewed"));
        await _context.SaveChangesAsync();

        var success = await _service.ResolveReportAsync(20, new UpdateReportStatusDto { AdminNotes = "Fixed" }, 99);
        success.Should().BeTrue();

        var report = await _context.ReportedMessages.FindAsync(20);
        report!.Status.Should().Be("Resolved");
    }

    [Fact]
    public async Task DeleteReportAsync_Existent_ShouldRemove()
    {
        _context.ReportedMessages.Add(MakeReport(30));
        await _context.SaveChangesAsync();

        var success = await _service.DeleteReportAsync(30);
        success.Should().BeTrue();
        (await _context.ReportedMessages.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ReviewReportAsync_NonExistent_ShouldReturnFalse()
    {
        var success = await _service.ReviewReportAsync(999, new UpdateReportStatusDto(), 1);
        success.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveReportAsync_NonExistent_ShouldReturnFalse()
    {
        var success = await _service.ResolveReportAsync(999, new UpdateReportStatusDto(), 1);
        success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteReportAsync_NonExistent_ShouldReturnFalse()
    {
        var success = await _service.DeleteReportAsync(999);
        success.Should().BeFalse();
    }
}
