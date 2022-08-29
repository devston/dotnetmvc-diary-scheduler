using DiaryScheduler.Presentation.Models.Scheduler;
using DiaryScheduler.Presentation.Services.Scheduler;
using DiaryScheduler.Presentation.Web.Common.Services.Scheduler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DiaryScheduler.Presentation.Web.Controllers;

[Authorize]
public class SchedulerController : Controller
{
    private readonly ISchedulerPresentationService _schedulerPresentationService;
    private readonly ISchedulerUrlGenerationService _schedulerUrlGenerationService;

    public SchedulerController(
        ISchedulerPresentationService schedulerPresentationService,
        ISchedulerUrlGenerationService schedulerUrlGenerationService)
    {
        _schedulerPresentationService = schedulerPresentationService;
        _schedulerUrlGenerationService = schedulerUrlGenerationService;
    }

    #region Views

    // GET: Scheduler
    public IActionResult Index()
    {
        var vm = _schedulerPresentationService.CreateSchedulerIndexViewModel();
        vm = _schedulerUrlGenerationService.SetIndexUrls(vm);
        return View(vm);
    }

    // GET: Create view.
    public IActionResult Create()
    {
        var vm = _schedulerPresentationService.CreateSchedulerCreateViewModel();
        vm = _schedulerUrlGenerationService.SetCreateUrls(vm);
        return View("Edit", vm);
    }

    // GET: Create view with filled in options.
    public IActionResult CreateMoreOptions(string title, DateTime start, DateTime end)
    {
        var vm = _schedulerPresentationService.CreateSchedulerCreateViewModel(title, start, end);
        vm = _schedulerUrlGenerationService.SetCreateUrls(vm);
        return View("Edit", vm);
    }

    // GET: Edit event view.
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        // Check if an id was sent.
        if (id == Guid.Empty)
        {
            return BadRequest("<strong>Error:</strong> Invalid calendar event id.");
        }

        var vm = await _schedulerPresentationService.CreateSchedulerEditViewModelAsync(id, cancellationToken);

        if (vm == null)
        {
            return BadRequest("<strong>Error:</strong> The calendar event could not be found.");
        }

        vm = _schedulerUrlGenerationService.SetEditUrls(vm);
        return View(vm);
    }

    // GET: Quick create view.
    public IActionResult _QuickCreate(DateTime start, DateTime end)
    {
        var vm = new CalendarEventViewModel()
        {
            DateFrom = start,
            DateTo = end
        };
        return PartialView(vm);
    }

    #endregion

    #region Posts

    // Create calendar event.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(CalendarEventViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("<strong>Error:</strong> Input is invalid.");
        }

        // Date range check.
        if (vm.DateFrom > vm.DateTo)
        {
            return BadRequest("<strong>Error:</strong> Start date cannot be after the end date.");
        }

        // Save event.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var id = await _schedulerPresentationService.CreateCalendarEventAsync(vm, userId, cancellationToken);

        // Return calendar record for fullCalendar.js.
        return Json(new
        {
            message = "<strong>Success:</strong> Calendar event created.",
            calEntry = new
            {
                id,
                title = vm.Title,
                start = vm.DateFrom,
                end = vm.DateTo,
                allDay = vm.AllDay
            },
            backUrl = Url.Action(nameof(Index), "Scheduler", null)
        });
    }

    // Edit calendar event.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(CalendarEventViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("<strong>Error:</strong> Input is invalid.");
        }

        // Date range check.
        if (vm.DateFrom > vm.DateTo)
        {
            return BadRequest("<strong>Error:</strong> Start date cannot be after the end date.");
        }

        // Check if the calendar entry exists.
        if (!await _schedulerPresentationService.CheckCalendarEventExistsAsync(vm.CalendarEventId, cancellationToken))
        {
            return BadRequest("<strong>Error:</strong> The calendar event could not be found.");
        }

        // Save event.
        await _schedulerPresentationService.UpdateCalendarEventAsync(vm, cancellationToken);

        return Json(new
        {
            message = "<strong>Success:</strong> Calendar event saved.",
            backUrl = Url.Action(nameof(Index), "Scheduler", null)
        });
    }

    // Delete calendar event.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken)
    {
        // Check if the calendar entry exists.
        if (!await _schedulerPresentationService.CheckCalendarEventExistsAsync(id, cancellationToken))
        {
            return BadRequest("<strong>Error:</strong> The calendar event could not be found.");
        }

        // Delete event.
        await _schedulerPresentationService.DeleteCalendarEventAsync(id, cancellationToken);

        return Json(new
        {
            message = "<strong>Success:</strong> Calendar entry deleted.",
            backUrl = Url.Action(nameof(Index), "Scheduler", null)
        });
    }

    #endregion

    #region Calendar Sources

    // GET: User calendar events.
    public async Task<IActionResult> UserEvents(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _schedulerPresentationService.GetCalendarEventsForUserBetweenDateRangeAsync(start, end, userId, cancellationToken);
        return Json(result);
    }

    #endregion

    #region Export

    // Create a .ics file for a calendar event.
    public async Task<IActionResult> ExportEventToIcal(Guid id, CancellationToken cancellationToken)
    {
        // Check if an id was sent.
        if (id == Guid.Empty)
        {
            return BadRequest("<strong>Error:</strong> Invalid calendar event id.");
        }

        var fileData = await _schedulerPresentationService.GenerateIcalForCalendarEventAsync(id, cancellationToken);

        if (fileData == null)
        {
            return BadRequest("<strong>Error:</strong> The calendar event could not be found.");
        }

        return File(fileData.Data, fileData.ContentType, fileData.FileName);
    }

    // Create a .ics file for calendar events from a date range.
    public async Task<IActionResult> ExportEventsToIcal(DateTime? start, DateTime? end, CancellationToken cancellationToken)
    {
        // Check if dates are null before doing anything.
        if (!start.HasValue || !end.HasValue)
        {
            return BadRequest("<strong>Error:</strong> No calendar events to sync.");
        }

        var userId = User.Identity.Name;
        var fileData = await _schedulerPresentationService.GenerateIcalBetweenDateRangeAsync(start.Value, end.Value, userId, cancellationToken);

        // Check if there are any diary entries to sync.
        if (fileData == null)
        {
            return BadRequest("<strong>Error:</strong> No calendar events to sync.");
        }

        return File(fileData.Data, fileData.ContentType, fileData.FileName);
    }

    #endregion
}
