using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Services;

namespace LandlordApp.Tests.Services;

public class ApartmentCacheWarmupServiceTests
{
    private readonly Mock<IApartmentService> _mockApartmentService;
    private readonly Mock<ILogger<ApartmentCacheWarmupService>> _mockLogger;
    private readonly ApartmentCacheWarmupService _service;

    public ApartmentCacheWarmupServiceTests()
    {
        _mockApartmentService = new Mock<IApartmentService>();
        _mockLogger = new Mock<ILogger<ApartmentCacheWarmupService>>();

        _mockApartmentService
            .Setup(s => s.GetAllApartmentsAsync(It.IsAny<ApartmentFilterDto>()))
            .ReturnsAsync(new PagedResult<ApartmentDto> { Items = new List<ApartmentDto>(), TotalCount = 0 });

        var services = new ServiceCollection();
        services.AddSingleton(_mockApartmentService.Object);

        var serviceProvider = services.BuildServiceProvider();
        _service = new ApartmentCacheWarmupService(serviceProvider, _mockLogger.Object);
    }

    [Fact]
    public async Task StartAsync_CallsGetAllApartmentsForEachDefaultFilter()
    {
        await _service.StartAsync(CancellationToken.None);

        // 3 default filter combinations are pre-loaded (no filter, date desc, price asc)
        _mockApartmentService.Verify(
            s => s.GetAllApartmentsAsync(It.IsAny<ApartmentFilterDto>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task StartAsync_DefaultFiltersHavePageOne()
    {
        var capturedFilters = new List<ApartmentFilterDto>();
        _mockApartmentService
            .Setup(s => s.GetAllApartmentsAsync(It.IsAny<ApartmentFilterDto>()))
            .Callback<ApartmentFilterDto>(f => capturedFilters.Add(f))
            .ReturnsAsync(new PagedResult<ApartmentDto> { Items = new List<ApartmentDto>(), TotalCount = 0 });

        await _service.StartAsync(CancellationToken.None);

        capturedFilters.Should().AllSatisfy(f => f.Page.Should().Be(1));
        capturedFilters.Should().AllSatisfy(f => f.PageSize.Should().Be(20));
    }

    [Fact]
    public async Task StartAsync_CancellationRequested_StopsEarly()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // cancel immediately

        await _service.StartAsync(cts.Token);

        // With immediate cancellation, no queries should run
        _mockApartmentService.Verify(
            s => s.GetAllApartmentsAsync(It.IsAny<ApartmentFilterDto>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_ServiceThrows_DoesNotPropagateException()
    {
        _mockApartmentService
            .Setup(s => s.GetAllApartmentsAsync(It.IsAny<ApartmentFilterDto>()))
            .ThrowsAsync(new Exception("DB connection failed"));

        // Warmup failures must never prevent the app from starting
        var act = async () => await _service.StartAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        var act = async () => await _service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
