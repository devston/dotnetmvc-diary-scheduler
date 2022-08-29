using DiaryScheduler.Presentation.Services.Scheduler;
using DiaryScheduler.Presentation.Services.Utility;
using DiaryScheduler.ScheduleManagement.Core.Interfaces;
using DiaryScheduler.ScheduleManagement.Core.Models;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiaryScheduler.Presentation.Services.Tests.Scheduler;

/// <summary>
/// A collection of tests for the <see cref="SchedulerPresentationService"/>.
/// </summary>
[TestFixture]
public class SchedulerPresentationServiceTests
{
    private Mock<IScheduleRepository> _scheduleRepo;
    private Mock<IDateTimeService> _dateTimeService;
    private SchedulerPresentationService _schedulerPresentationService;

    [SetUp]
    public void SetUp()
    {
        _scheduleRepo = new Mock<IScheduleRepository>();
        _dateTimeService = new Mock<IDateTimeService>();
        _schedulerPresentationService = new SchedulerPresentationService(
            _scheduleRepo.Object, _dateTimeService.Object);
    }

    [Test]
    public void CreateSchedulerCreateViewModel_GivenTimeAs1210_ReturnsDateRangeWith1215And1230()
    {
        // Arrange
        var fromDate = new DateTime(2022, 1, 1, 12, 10, 0);
        var expectedFromDate = new DateTime(2022, 1, 1, 12, 15, 0);
        var expectedToDate = new DateTime(2022, 1, 1, 12, 30, 0);
        _dateTimeService.Setup(x => x.GetDateTimeUtcNow()).Returns(fromDate);

        // Act
        var result = _schedulerPresentationService.CreateSchedulerCreateViewModel();

        // Assert
        result.DateFrom.Should().Be(expectedFromDate);
        result.DateTo.Should().Be(expectedToDate);
    }

    [Test]
    public void CreateSchedulerCreateViewModel_GivenProvidedParameters_ReturnsModelWithSetParameters()
    {
        // Arrange
        var expectedTitle = "Test";
        var expectedFromDate = new DateTime(2022, 1, 1, 12, 15, 0);
        var expectedToDate = new DateTime(2022, 1, 1, 12, 30, 0);

        // Act
        var result = _schedulerPresentationService.CreateSchedulerCreateViewModel(expectedTitle, expectedFromDate, expectedToDate);

        // Assert
        result.Title.Should().Be(expectedTitle);
        result.DateFrom.Should().Be(expectedFromDate);
        result.DateTo.Should().Be(expectedToDate);
    }

    [Test]
    public async Task CreateSchedulerEditViewModel_GivenInvalidGuid_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        _scheduleRepo.Setup(x => x.GetCalendarEventByIdAsync(eventId, cancellationToken)).ReturnsAsync(() => null);

        // Act
        var result = await _schedulerPresentationService.CreateSchedulerEditViewModelAsync(eventId, cancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetCalendarEventsForUserBetweenDateRange_GivenRequiredParameters_ReturnsConvertedEventCollection()
    {
        // Arrange
        var start = new DateTime(2022, 1, 1, 12, 15, 0);
        var end = new DateTime(2022, 3, 3, 12, 30, 0);
        var userId = "MrTest";
        var cancellationToken = new CancellationToken();
        var userEvents = new List<CalEventDm>()
        {
            new CalEventDm()
            {
                Title = "Event 1",
                DateFrom = new DateTime(2022, 1, 1, 12, 15, 0),
                DateTo = new DateTime(2022, 1, 1, 12, 30, 0),
                CalendarEntryId = Guid.NewGuid(),
                AllDay = true
            },
            new CalEventDm()
            {
                Title = "Event 2",
                DateFrom = new DateTime(2022, 2, 2, 12, 15, 0),
                DateTo = new DateTime(2022, 2, 2, 12, 30, 0),
                CalendarEntryId = Guid.NewGuid(),
                AllDay = true
            },
            new CalEventDm()
            {
                Title = "Event 3",
                DateFrom = new DateTime(2022, 3, 3, 12, 15, 0),
                DateTo = new DateTime(2022, 3, 3, 12, 30, 0),
                CalendarEntryId = Guid.NewGuid(),
                AllDay = false
            }
        };
        var expectedResult = userEvents.Select(x => new
        {
            title = x.Title,
            start = x.DateFrom.ToString("o"),
            end = x.DateTo.ToString("o"),
            id = x.CalendarEntryId,
            allDay = x.AllDay
        });
        var convertedExpectedResult = JsonConvert.SerializeObject(expectedResult);
        _scheduleRepo.Setup(x => x.GetAllUserEventsAsync(userId, start, end, cancellationToken)).ReturnsAsync(userEvents);

        // Act
        var result = await _schedulerPresentationService.GetCalendarEventsForUserBetweenDateRangeAsync(start, end, userId, cancellationToken);
        // Convert the result to json so we can compare the contents match.
        var convertedResult = JsonConvert.SerializeObject(result);

        // Assert
        convertedResult.Should().Be(convertedExpectedResult);
    }

    [Test]
    public async Task GenerateIcalForCalendarEvent_GivenInvalidEventId_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        _scheduleRepo.Setup(x => x.GetCalendarEventByIdAsync(eventId, cancellationToken)).ReturnsAsync(() => null);

        // Act
        var result = await _schedulerPresentationService.GenerateIcalForCalendarEventAsync(eventId, cancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GenerateIcalForCalendarEvent_GivenValidEventId_ReturnsModelWithIcalData()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        var eventData = new CalEventDm()
        {
            CalendarEntryId = eventId,
            UserId = "MrTestId",
            Title = "Event 1",
            Description = "This is a test event.",
            DateFrom = new DateTime(2022, 1, 1, 12, 15, 0),
            DateTo = new DateTime(2022, 1, 1, 12, 30, 0),
            AllDay = false
        };
        _scheduleRepo.Setup(x => x.GetCalendarEventByIdAsync(eventId, cancellationToken)).ReturnsAsync(eventData);
        var expectedContentType = "text/calendar";

        // Act
        var result = await _schedulerPresentationService.GenerateIcalForCalendarEventAsync(eventId, cancellationToken);

        // Assert
        result.ContentType.Should().Be(expectedContentType);
        result.Data.Should().NotBeNull();
        result.FileName.Contains(".ics").Should().BeTrue();
    }

    [Test]
    public async Task GenerateIcalBetweenDateRange_GivenInvalidParameters_ReturnsNull()
    {
        // Arrange
        var start = new DateTime(2022, 1, 1, 12, 15, 0);
        var end = new DateTime(2022, 1, 1, 12, 30, 0);
        var cancellationToken = new CancellationToken();
        var userId = "MrTestId";
        _scheduleRepo.Setup(x => x.GetAllUserEventsAsync(userId, start, end, cancellationToken)).ReturnsAsync(() => null);

        // Act
        var result = await _schedulerPresentationService.GenerateIcalBetweenDateRangeAsync(start, end, userId, cancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GenerateIcalBetweenDateRange_GivenValidParameters_ReturnsModelWithIcalData()
    {
        // Arrange
        var start = new DateTime(2022, 1, 1, 12, 15, 0);
        var end = new DateTime(2022, 1, 1, 12, 30, 0);
        var userId = "MrTestId";
        var cancellationToken = new CancellationToken();
        var eventData = new List<CalEventDm>()
        {
            new CalEventDm()
            {
                CalendarEntryId = Guid.NewGuid(),
                UserId = userId,
                Title = "Event 1",
                Description = "This is a test event.",
                DateFrom = new DateTime(2022, 1, 1, 12, 15, 0),
                DateTo = new DateTime(2022, 1, 1, 12, 30, 0),
                AllDay = false
            },
            new CalEventDm()
            {
                CalendarEntryId = Guid.NewGuid(),
                UserId = userId,
                Title = "Event 2",
                Description = "This is a test event.",
                DateFrom = new DateTime(2022, 1, 1, 12, 15, 0),
                DateTo = new DateTime(2022, 1, 1, 12, 30, 0),
                AllDay = false
            },
            new CalEventDm()
            {
                CalendarEntryId = Guid.NewGuid(),
                UserId = userId,
                Title = "Event 3",
                Description = "This is a test event.",
                DateFrom = new DateTime(2022, 1, 1, 12, 15, 0),
                DateTo = new DateTime(2022, 1, 1, 12, 30, 0),
                AllDay = false
            }
        };
        var expectedContentType = "text/calendar";
        _scheduleRepo.Setup(x => x.GetAllUserEventsAsync(userId, start, end, cancellationToken)).ReturnsAsync(eventData);

        // Act
        var result = await _schedulerPresentationService.GenerateIcalBetweenDateRangeAsync(start, end, userId, cancellationToken);

        // Assert
        result.ContentType.Should().Be(expectedContentType);
        result.Data.Should().NotBeNull();
        result.FileName.Contains(".ics").Should().BeTrue();
    }
}
