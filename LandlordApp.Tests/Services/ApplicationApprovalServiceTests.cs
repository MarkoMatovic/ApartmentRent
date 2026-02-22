using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander;
using Lander.src.Modules.ApartmentApplications.Implementation;
using Lander.src.Modules.ApartmentApplications.Models;

namespace LandlordApp.Tests.Services;

public class ApplicationApprovalServiceTests : IDisposable
{
    private readonly ApplicationsContext _context;
    private readonly ApplicationApprovalService _service;

    public ApplicationApprovalServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationsContext(options);
        _service = new ApplicationApprovalService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task HasApprovedApplicationAsync_ApprovedExists_ShouldReturnTrue()
    {
        _context.ApartmentApplications.Add(new ApartmentApplication { UserId = 1, ApartmentId = 1, Status = "Approved" });
        await _context.SaveChangesAsync();

        var result = await _service.HasApprovedApplicationAsync(1, 1);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasApprovedApplicationAsync_PendingExists_ShouldReturnFalse()
    {
        _context.ApartmentApplications.Add(new ApartmentApplication { UserId = 1, ApartmentId = 1, Status = "Pending" });
        await _context.SaveChangesAsync();

        var result = await _service.HasApprovedApplicationAsync(1, 1);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetApplicationAsync_Exists_ShouldReturnApplication()
    {
        _context.ApartmentApplications.Add(new ApartmentApplication { UserId = 5, ApartmentId = 10 });
        await _context.SaveChangesAsync();

        var result = await _service.GetApplicationAsync(5, 10);
        result.Should().NotBeNull();
        result!.UserId.Should().Be(5);
    }

    [Fact]
    public async Task GetApplicationAsync_NotExists_ShouldReturnNull()
    {
        var result = await _service.GetApplicationAsync(99, 99);
        result.Should().BeNull();
    }
}
