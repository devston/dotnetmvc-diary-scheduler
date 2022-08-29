using DiaryScheduler.Presentation.Models.Scheduler;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiaryScheduler.Presentation.Services.Scheduler;

/// <summary>
/// The interface for the scheduler presentation service.
/// </summary>
public interface ISchedulerPresentationService
{
    /// <summary>
    /// Create the <see cref="SchedulerIndexViewModel"/>.
    /// </summary>
    /// <returns>The <see cref="SchedulerIndexViewModel"/>.</returns>
    SchedulerIndexViewModel CreateSchedulerIndexViewModel();

    /// <summary>
    /// Create the create variant of the <see cref="SchedulerModifyViewModel"/>.
    /// </summary>
    /// <returns>The <see cref="SchedulerModifyViewModel"/>.</returns>
    SchedulerModifyViewModel CreateSchedulerCreateViewModel();

    /// <summary>
    /// Create the create variant of the <see cref="SchedulerModifyViewModel"/>.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="start">The event start date.</param>
    /// <param name="end">The event end date.</param>
    /// <returns>The <see cref="SchedulerModifyViewModel"/>.</returns>
    SchedulerModifyViewModel CreateSchedulerCreateViewModel(string title, DateTime start, DateTime end);

    /// <summary>
    /// Create the edit variant of the <see cref="SchedulerModifyViewModel"/>.
    /// </summary>
    /// <param name="id">The calendar entry id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="SchedulerModifyViewModel"/>.</returns>
    Task<SchedulerModifyViewModel> CreateSchedulerEditViewModelAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Check if a calendar event exists by event id.
    /// </summary>
    /// <param name="eventId">The calendar event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether the calendar event exists.</returns>
    Task<bool> CheckCalendarEventExistsAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>
    /// Get the calendar events for a user between a date range.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The calendar events as an object.</returns>
    Task<object> GetCalendarEventsForUserBetweenDateRangeAsync(DateTime start, DateTime end, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Generate an ical file for a calendar event.
    /// </summary>
    /// <param name="id">The event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file data stored in <see cref="CalendarIcalViewModel"/>.</returns>
    Task<CalendarIcalViewModel> GenerateIcalForCalendarEventAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Generate an ical file between a date range.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <param name="userId">The user id to check events for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file data stored in <see cref="CalendarIcalViewModel"/>.</returns>
    Task<CalendarIcalViewModel> GenerateIcalBetweenDateRangeAsync(DateTime start, DateTime end, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Create a calendar event.
    /// </summary>
    /// <param name="eventVm">The calendar entry to event.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The event id.</returns>
    Task<Guid> CreateCalendarEventAsync(CalendarEventViewModel eventVm, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Update a calendar event.
    /// </summary>
    /// <param name="eventVm">The event to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    Task UpdateCalendarEventAsync(CalendarEventViewModel eventVm, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a calendar event.
    /// </summary>
    /// <param name="id">The calendar event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    Task DeleteCalendarEventAsync(Guid id, CancellationToken cancellationToken);
}
