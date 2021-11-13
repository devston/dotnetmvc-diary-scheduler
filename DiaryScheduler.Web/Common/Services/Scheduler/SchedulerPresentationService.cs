﻿using DiaryScheduler.ScheduleManagement.Core.Interfaces;
using DiaryScheduler.ScheduleManagement.Core.Models;
using DiaryScheduler.Web.Common.Utilities.UrlHelper;
using DiaryScheduler.Web.Models.Scheduler;
using System;

namespace DiaryScheduler.Web.Common.Services.Scheduler
{
    /// <summary>
    /// The mvc implementation of the <see cref="ISchedulerPresentationService"/>.
    /// </summary>
    public class SchedulerPresentationService : ISchedulerPresentationService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IUrlHelperService _urlHelperService;

        public SchedulerPresentationService(
            IScheduleRepository scheduleRepository,
            IUrlHelperService urlHelperService)
        {
            _scheduleRepository = scheduleRepository;
            _urlHelperService = urlHelperService;
        }

        public SchedulerIndexViewModel CreateSchedulerIndexViewModel()
        {
            var urlHelper = _urlHelperService.GetUrlHelper();
            var vm = new SchedulerIndexViewModel();
            vm.CreateEventUrl = urlHelper.Action(nameof(Controllers.SchedulerController.Create), "Scheduler", null);
            vm.CreateEventMoreOptionsUrl = urlHelper.Action(nameof(Controllers.SchedulerController.CreateMoreOptions), "Scheduler", new { title = "title_placeholder", start = "start_placeholder", end = "end_placeholder" });
            vm.EditEventUrl = urlHelper.Action(nameof(Controllers.SchedulerController.Edit), "Scheduler", new { id = "id_placeholder" });
            vm.CalendarSourceUrl = urlHelper.Action(nameof(Controllers.SchedulerController.UserEntries), "Scheduler", null);
            return vm;
        }

        public SchedulerModifyViewModel CreateSchedulerCreateViewModel()
        {
            var vm = CreateBaseSchedulerCreateViewModel();
            var today = DateTime.UtcNow;
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

        public SchedulerModifyViewModel CreateSchedulerEditViewModel(Guid id)
        {
            var entry = _scheduleRepository.GetCalendarEntry(id);

            if (entry == null)
            {
                return null;
            }

            var urlHelper = _urlHelperService.GetUrlHelper();
            var vm = new SchedulerModifyViewModel();
            vm.AllDay = entry.AllDay;
            vm.CalendarEntryId = entry.CalendarEntryId;
            vm.DateFrom = entry.DateFrom;
            vm.DateTo = entry.DateTo;
            vm.Description = entry.Description;
            vm.Title = entry.Title;
            vm.UserId = entry.UserId;
            vm.SaveUrl = urlHelper.Action(nameof(Controllers.SchedulerController.EditEntry), "Scheduler", null);
            vm.ShowDeleteBtn = true;
            vm.ShowExportBtn = true;
            vm.PageTitle = "Edit calendar event";
            return vm;
        }

        public Guid CreateCalendarEvent(CalendarEventViewModel eventVm, string userId)
        {
            var entry = new CalEntryDm()
            {
                Title = eventVm.Title.Trim(),
                Description = eventVm.Description == null ? null : eventVm.Description.Trim(),
                DateFrom = eventVm.DateFrom,
                DateTo = eventVm.DateTo,
                AllDay = eventVm.AllDay,
                UserId = userId
            };

            // Save event.
            var id = _scheduleRepository.CreateCalendarEntry(entry);
            return id;
        }

        public void UpdateCalendarEvent(CalendarEventViewModel eventVm)
        {
            var entry = new CalEntryDm()
            {
                CalendarEntryId = eventVm.CalendarEntryId,
                Title = eventVm.Title.Trim(),
                Description = eventVm.Description == null ? null : eventVm.Description.Trim(),
                DateFrom = eventVm.DateFrom,
                DateTo = eventVm.DateTo,
                AllDay = eventVm.AllDay,
                UserId = eventVm.UserId
            };

            // Save event.
            _scheduleRepository.EditCalendarEntry(entry);
        }

        public void DeleteCalendarEvent(Guid id)
        {
            _scheduleRepository.DeleteCalendarEntry(id);
        }

        /// <summary>
        /// Create a prepopulated <see cref="SchedulerModifyViewModel"/> for the create variant.
        /// </summary>
        /// <returns>The <see cref="SchedulerModifyViewModel"/>.</returns>
        private SchedulerModifyViewModel CreateBaseSchedulerCreateViewModel()
        {
            var urlHelper = _urlHelperService.GetUrlHelper();
            var vm = new SchedulerModifyViewModel();
            vm.SaveUrl = urlHelper.Action(nameof(Controllers.SchedulerController.CreateEntry), "Scheduler", null);
            vm.PageTitle = "Create calendar event";
            return vm;
        }
    }
}