﻿using DiaryScheduler.Presentation.Models.Scheduler;
using DiaryScheduler.Presentation.Services.Utility;
using DiaryScheduler.ScheduleManagement.Core.Interfaces;
using DiaryScheduler.ScheduleManagement.Core.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiaryScheduler.Presentation.Services.Scheduler;

/// <summary>
/// The mvc implementation of the <see cref="ISchedulerPresentationService"/>.
/// </summary>
public class SchedulerPresentationService : ISchedulerPresentationService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IDateTimeService _dateTimeService;

    public SchedulerPresentationService(
        IScheduleRepository scheduleRepository,
        IDateTimeService dateTimeService)
    {
        _scheduleRepository = scheduleRepository;
        _dateTimeService = dateTimeService;
    }

    public SchedulerIndexViewModel CreateSchedulerIndexViewModel()
    {
        var vm = new SchedulerIndexViewModel();
        return vm;
    }

    public SchedulerModifyViewModel CreateSchedulerCreateViewModel()
    {
        var vm = CreateBaseSchedulerCreateViewModel();
        var today = _dateTimeService.GetDateTimeUtcNow();
        var amountToRound = TimeSpan.FromMinutes(15);

        // Rounds date to the nearest specified minute.
        // "+ amountToRound.Ticks - 1" makes sure today will round up if necessary. E.g. (12 + 5 - 1) = 16, 16 / 5 = 3,  3 * 5 = 15.
        var laterToday = new DateTime(((today.Ticks + amountToRound.Ticks - 1) / amountToRound.Ticks) * amountToRound.Ticks);

        vm.DateFrom = laterToday;
        vm.DateTo = laterToday.AddMinutes(15);
        return vm;
    }

    public SchedulerModifyViewModel CreateSchedulerCreateViewModel(string title, DateTime start, DateTime end)
    {
        var vm = CreateBaseSchedulerCreateViewModel();
        vm.Title = title;
        vm.DateFrom = start;
        vm.DateTo = end;
        return vm;
    }

    public async Task<SchedulerModifyViewModel> CreateSchedulerEditViewModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _scheduleRepository.GetCalendarEventByIdAsync(id, cancellationToken);

        if (entry == null)
        {
            return null;
        }

        var vm = new SchedulerModifyViewModel();
        vm.AllDay = entry.AllDay;
        vm.CalendarEventId = entry.CalendarEntryId;
        vm.DateFrom = entry.DateFrom;
        vm.DateTo = entry.DateTo;
        vm.Description = entry.Description;
        vm.Title = entry.Title;
        vm.UserId = entry.UserId;
        vm.ShowDeleteBtn = true;
        vm.ShowExportBtn = true;
        vm.PageTitle = "Edit calendar event";
        return vm;
    }

    public async Task<bool> CheckCalendarEventExistsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _scheduleRepository.DoesEventExistAsync(eventId, cancellationToken);
    }

    public async Task<object> GetCalendarEventsForUserBetweenDateRangeAsync(DateTime start, DateTime end, string userId, CancellationToken cancellationToken)
    {
        var userEvents = await _scheduleRepository.GetAllUserEventsAsync(userId, start, end, cancellationToken);

        object result = userEvents.Select(x => new
        {
            title = x.Title,
            start = x.DateFrom.ToString("o"),
            end = x.DateTo.ToString("o"),
            id = x.CalendarEntryId,
            allDay = x.AllDay
        });

        return result;
    }

    public async Task<CalendarIcalViewModel> GenerateIcalForCalendarEventAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _scheduleRepository.GetCalendarEventByIdAsync(id, cancellationToken);

        if (entry == null)
        {
            return null;
        }

        // Create iCal.
        var iCal = new Ical.Net.Calendar()
        {
            ProductId = "ASP.Net Diary Scheduler",
            Version = "2.0"
        };

        // Create event.
        CreateCalendarIcalEventFromCalendarEvent(iCal, entry);

        // Build the .ics file.
        return CreateCalendarIcalViewModelFromIcal(iCal);
    }

    public async Task<CalendarIcalViewModel> GenerateIcalBetweenDateRangeAsync(DateTime start, DateTime end, string userId, CancellationToken cancellationToken)
    {
        // Get user's calendar entries within the date range.
        var userEntries = await _scheduleRepository.GetAllUserEventsAsync(userId, start, end, cancellationToken);

        // Check if there are any diary entries to sync.
        if (userEntries == null)
        {
            return null;
        }

        // Create iCal.
        var iCal = new Ical.Net.Calendar()
        {
            ProductId = "ASP.Net Diary Scheduler",
            Version = "2.0"
        };

        // Create a new event for each calendar entry.
        foreach (CalEventDm entry in userEntries)
        {
            CreateCalendarIcalEventFromCalendarEvent(iCal, entry);
        }

        // Build the .ics file.
        return CreateCalendarIcalViewModelFromIcal(iCal);
    }

    public async Task<Guid> CreateCalendarEventAsync(CalendarEventViewModel eventVm, string userId, CancellationToken cancellationToken)
    {
        eventVm.UserId = userId;
        var calEvent = ConvertCalendarEventViewModelToDomainModel(eventVm);

        // Save event.
        var id = await _scheduleRepository.CreateCalendarEventAsync(calEvent, cancellationToken);
        return id;
    }

    public async Task UpdateCalendarEventAsync(CalendarEventViewModel eventVm, CancellationToken cancellationToken)
    {
        var calEvent = ConvertCalendarEventViewModelToDomainModel(eventVm);

        // Save event.
        await _scheduleRepository.EditCalendarEventAsync(calEvent, cancellationToken);
    }

    public async Task DeleteCalendarEventAsync(Guid id, CancellationToken cancellationToken)
    {
        await _scheduleRepository.DeleteCalendarEventAsync(id, cancellationToken);
    }

    /// <summary>
    /// Create a prepopulated <see cref="SchedulerModifyViewModel"/> for the create variant.
    /// </summary>
    /// <returns>The <see cref="SchedulerModifyViewModel"/>.</returns>
    private SchedulerModifyViewModel CreateBaseSchedulerCreateViewModel()
    {
        var vm = new SchedulerModifyViewModel();
        vm.PageTitle = "Create calendar event";
        return vm;
    }

    /// <summary>
    /// Create a calendar event in the <see cref="Ical.Net.Calendar"/>.
    /// </summary>
    /// <param name="iCal">The calendar to add the event to.</param>
    /// <param name="entry">The event to add.</param>
    private void CreateCalendarIcalEventFromCalendarEvent(Ical.Net.Calendar iCal, CalEventDm entry)
    {
        // Create event.
        var evt = iCal.Create<Ical.Net.CalendarComponents.CalendarEvent>();

        // Prepare ical event.
        evt.Uid = entry.CalendarEntryId.ToString();
        evt.Start = new Ical.Net.DataTypes.CalDateTime(entry.DateFrom);
        evt.End = new Ical.Net.DataTypes.CalDateTime(entry.DateTo);
        evt.Description = entry.Description;
        evt.Summary = entry.Title;
        evt.IsAllDay = entry.AllDay;
    }

    /// <summary>
    /// Convert a <see cref="CalendarEventViewModel"/> to a <see cref="CalEventDm"/>.
    /// </summary>
    /// <param name="eventVm">The model to convert.</param>
    /// <returns>The converted <see cref="CalEventDm"/>.</returns>
    private CalEventDm ConvertCalendarEventViewModelToDomainModel(CalendarEventViewModel eventVm)
    {
        var calEvent = new CalEventDm()
        {
            CalendarEntryId = eventVm.CalendarEventId,
            Title = eventVm.Title.Trim(),
            Description = string.IsNullOrEmpty(eventVm.Description) ? null : eventVm.Description.Trim(),
            DateFrom = eventVm.DateFrom,
            DateTo = eventVm.DateTo,
            AllDay = eventVm.AllDay,
            UserId = eventVm.UserId
        };
        return calEvent;
    }

    /// <summary>
    /// Create the <see cref="CalendarIcalViewModel"/> using the <see cref="Ical.Net.Calendar"/>.
    /// </summary>
    /// <param name="iCal">The calendar to generate an ics from.</param>
    /// <returns>THe <see cref="CalendarIcalViewModel"/> holding the ics data.</returns>
    private CalendarIcalViewModel CreateCalendarIcalViewModelFromIcal(Ical.Net.Calendar iCal)
    {
        var fileData = new CalendarIcalViewModel();
        Ical.Net.Serialization.SerializationContext ctx = new Ical.Net.Serialization.SerializationContext();
        var serialiser = new Ical.Net.Serialization.CalendarSerializer(ctx);

        string output = serialiser.SerializeToString(iCal);
        fileData.ContentType = "text/calendar";
        fileData.Data = System.Text.Encoding.UTF8.GetBytes(output);

        // Create a name.
        Guid fileId = Guid.NewGuid();
        fileData.FileName = fileId.ToString() + ".ics";
        return fileData;
    }
}
