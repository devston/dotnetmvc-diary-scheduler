using DiaryScheduler.Data.Data;
using DiaryScheduler.Data.Models;
using DiaryScheduler.ScheduleManagement.Core.Interfaces;
using DiaryScheduler.ScheduleManagement.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiaryScheduler.ScheduleManagement.Data.Repositories;

/// <summary>
/// The entity framework implementation of the <see cref="IScheduleRepository"/>.
/// </summary>
public class EFScheduleRepository : IScheduleRepository
{
    private readonly ApplicationDbContext _context;

    public EFScheduleRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    #region Gets

    public async Task<List<CalEventDm>> GetAllUserEventsAsync(string id, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        return await _context.CalendarEvents.AsNoTracking()
            .Where(x => x.UserId == id && x.DateFrom >= start && x.DateTo <= end)
            .Select(x => new CalEventDm
            {
                AllDay = x.AllDay,
                CalendarEntryId = x.CalendarEventId,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
                Description = x.Description,
                Title = x.Title,
                UserId = x.UserId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CalEventDm> GetCalendarEventByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.CalendarEvents.AsNoTracking()
            .Where(x => x.CalendarEventId == id)
            .Select(x => new CalEventDm
            {
                AllDay = x.AllDay,
                CalendarEntryId = x.CalendarEventId,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
                Description = x.Description,
                Title = x.Title,
                UserId = x.UserId
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region Checks

    public async Task<bool> DoesEventExistAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.CalendarEvents.AnyAsync(x => x.CalendarEventId == id, cancellationToken);
    }

    #endregion

    #region Create, update and delete

    public async Task<Guid> CreateCalendarEventAsync(CalEventDm entry, CancellationToken cancellationToken)
    {
        var mappedEntry = ConvertCalendarEventDomainModelToEntity(entry);
        _context.CalendarEvents.Attach(mappedEntry);
        _context.Entry(mappedEntry).State = EntityState.Added;
        await _context.SaveChangesAsync(cancellationToken);
        return mappedEntry.CalendarEventId;
    }

    public async Task EditCalendarEventAsync(CalEventDm entry, CancellationToken cancellationToken)
    {
        // Get the original event.
        var originalEntry = await _context.CalendarEvents.FirstOrDefaultAsync(x => x.CalendarEventId == entry.CalendarEntryId, cancellationToken);

        // Double check the event exists.
        if (originalEntry == null)
        {
            throw new Exception("The calendar event could not be found.");
        }

        // Update values.
        originalEntry.Title = entry.Title;
        originalEntry.Description = entry.Description;
        originalEntry.DateFrom = entry.DateFrom;
        originalEntry.DateTo = entry.DateTo;
        originalEntry.AllDay = entry.AllDay;

        // Save changes.
        _context.Entry(originalEntry).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCalendarEventAsync(Guid id, CancellationToken cancellationToken)
    {
        // Get the original event.
        var originalEntry = await _context.CalendarEvents.FirstOrDefaultAsync(x => x.CalendarEventId == id, cancellationToken);

        // Double check the event exists.
        if (originalEntry == null)
        {
            throw new Exception("The calendar event could not be found.");
        }

        // Save changes.
        _context.CalendarEvents.Remove(originalEntry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Convert a <see cref="CalEventDm"/> to a <see cref="CalendarEvent"/>.
    /// </summary>
    /// <param name="entry">The domain model to convert.</param>
    /// <returns>The converted <see cref="CalendarEvent"/>.</returns>
    private CalendarEvent ConvertCalendarEventDomainModelToEntity(CalEventDm entry)
    {
        var newEntity = new CalendarEvent();
        newEntity.AllDay = entry.AllDay;
        newEntity.CalendarEventId = entry.CalendarEntryId;
        newEntity.DateFrom = entry.DateFrom;
        newEntity.DateTo = entry.DateTo;
        newEntity.Description = entry.Description;
        newEntity.Title = entry.Title;
        newEntity.UserId = entry.UserId;
        return newEntity;
    }

    #endregion
}
