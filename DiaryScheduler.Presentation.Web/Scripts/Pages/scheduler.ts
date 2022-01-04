/*----------------------------------------------------------------------------*\
    
    Scheduler
    ---------

    All js related to the scheduler area is found here.

    Contents
    --------

    1. Initialisation functions
    2. Create, update and delete functions
    3. Export functions
    4. Helper functions
    5. Navigation functions
    
\*----------------------------------------------------------------------------*/

import * as bootstrap from "bootstrap";
import { validationHandlers } from "unobtrusive-validation";
import moment from "moment";
import { SiteCalendar } from "./../Components/site-calendar";
import { DateTimePicker } from "./../Components/site-datetimepicker";
import { SiteAlert } from "./../Components/site-alert";
import { SiteLoader } from "./../Components/site-loader";

/**
 * A module containing all the logic for the scheduler area.
 */
export namespace Scheduler {

    /*------------------------------------------------------------------------*\
        1. Initialisation functions
    \*------------------------------------------------------------------------*/

    const calendarSelector = "#calendar";
    const mainContainer = "scheduler-main-container";

    /**
     * Initialise scheduler area.
     */
    export function init() {
        const pageToLoad = document.getElementById(mainContainer).getAttribute("data-page-name");

        switch (pageToLoad) {
            case "modify":
                initModify();
                break;

            default:
                initIndex();
                break;
        }
    }

    /**
     * Initialise the index page.
     */
    function initIndex() {
        const sourceUrl = (document.getElementById("CalendarSourceUrl") as HTMLInputElement).value;
        SiteCalendar.init(calendarSelector, sourceUrl, showQuickCreateModal, Navigate.toEdit);

        document.getElementById("export-events-btn").onclick = function (e) {
            e.preventDefault();
            initExportModal();
        };
    }

    /**
     *  Initialise the edit event page.
     * */
    function initModify() {
        const eventId = (document.getElementById("CalendarEventId") as HTMLInputElement).value;
        const startPickerSelector = "#DateFrom";
        const endPickerSelector = "#DateTo";

        // Initialise the datetime pickers.
        DateTimePicker.initDateTimeRange(startPickerSelector, endPickerSelector);

        // Form submit.
        const formEl = document.getElementById("edit-cal-entry-form") as HTMLFormElement;
        formEl.onsubmit = function (e) {
            e.preventDefault();
            saveEvent(formEl);
        };

        const exportBtnEl = document.getElementById("export-event-btn");
        exportBtnEl.onclick = function (e) {
            e.preventDefault();
            const url = exportBtnEl.dataset.url;
            exportEventToIcal(eventId, url);
        };

        document.getElementById("delete-entry-btn").onclick = function (e) {
            e.preventDefault();
            initDeleteModal(eventId);
        };
    }

    /**
     *  Initialise the export calendar event modal.
     * */
    function initExportModal() {
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById("export-events-modal"));
        const startPickerSelector = "#SyncFrom";
        const endPickerSelector = "#SyncTo";
        let radioValue = "0";

        // Initialise the datetime pickers.
        DateTimePicker.initDateTimeRange(startPickerSelector, endPickerSelector);

        // Initialise the radio controls.
        const radioControl = document.getElementById("calendar-sync-options-container").querySelectorAll("input[name=\"calsync\"]");
        radioControl.forEach((element: HTMLInputElement) => {
            element.onchange = function () {
                if (element.value == "1") {
                    document.getElementById("date-sync-container").classList.add("hidden");
                    document.getElementById("calendar-sync-container").classList.remove("hidden");
                }
                else {
                    document.getElementById("calendar-sync-container").classList.add("hidden");
                    document.getElementById("date-sync-container").classList.remove("hidden");
                }

                radioValue = element.value;
            }
        });

        // Initialise the confirm button.
        const confirmBtnElement = document.getElementById("confirm-export-btn");
        confirmBtnElement.onclick = function initConfirmExportBtn(e) {
            e.preventDefault();
            // No value selected.
            if (radioValue == "0") {
                return;
            }
            else {
                modal.hide();

                if (radioValue == "1") {
                    exportVisibleEventsToIcal();
                }
                else if (radioValue == "2") {
                    const start = (document.getElementById("SyncFrom") as HTMLInputElement).value;
                    const end = (document.getElementById("SyncTo") as HTMLInputElement).value;
                    exportEventsFromDateRangeToIcal(start, end);
                }
            }
        }
        modal.show();
    }

    /**
     *  Initialise the delete event modal.
     * */
    function initDeleteModal(eventId: string) {
        const modalEl = document.getElementById("confirm-delete-modal");
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        const deleteBtnEl = document.getElementById("confirm-delete-btn");
        deleteBtnEl.onclick = function (event) {
            event.preventDefault();

            // Stop the modal from getting 'stuck'.
            modalEl.addEventListener("hidden.bs.modal", function () {
                deleteEvent(eventId);
            });

            modal.hide();
        };

        modal.show();
    }

    function initDeleteModalDeleteBtn(event: MouseEvent, eventId: string, modalEl: HTMLElement, modal: bootstrap.Modal) {
        event.preventDefault();

        // Stop the modal from getting 'stuck'.
        modalEl.addEventListener("hidden.bs.modal", function () {
            deleteEvent(eventId);
        });

        modal.hide();
    }

    /*------------------------------------------------------------------------*\
        2. Create, update and delete functions
    \*------------------------------------------------------------------------*/

    /**
     *  Create a new calendar entry from the quick create modal.
     * */
    function quickCreate() {
        const form = document.getElementById("quick-create-form") as HTMLFormElement;
        const createUrl = (document.getElementById("PostCreateEventUrl") as HTMLInputElement).value;
        const editBtnEl = document.getElementById("edit-entry-btn");
        const saveBtnEl = document.getElementById("quick-create-btn");
        SiteLoader.toggleGlobalLoader(true);
        editBtnEl.setAttribute("disabled", "");
        saveBtnEl.setAttribute("disabled", "");

        fetch(createUrl, {
            method: "post",
            body: new FormData(form)
        })
        .then((response) => response.json())
        .then(function (data: any) {
            SiteLoader.toggleGlobalLoader(false);
            editBtnEl.removeAttribute("disabled");
            saveBtnEl.removeAttribute("disabled");
            SiteCalendar.addEvent(data.calEntry, calendarSelector);
            SiteAlert.show("success", data.message, true);
            const modalEl = document.getElementById("quick-create-modal");
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

            modalEl.addEventListener("hidden.bs.modal", function () {
                $("#quick-create-container").empty();
            });

            // Close and empty the modal.
            modal.hide();
        })
        .catch((err) => {
            SiteLoader.toggleGlobalLoader(false);
            editBtnEl.removeAttribute("disabled");
            saveBtnEl.removeAttribute("disabled");
            SiteAlert.show("danger", err, true);
        });
    }

    /**
     * Save a calendar event.
     * @param form
     */
    function saveEvent(form: HTMLFormElement) {
        const url = form.dataset.url;
        const saveBtnEl = document.getElementById("save-entry-btn");
        const deleteBtnEl = document.getElementById("delete-entry-btn");
        const backBtnEl = document.getElementById("back-to-cal-btn");
        SiteLoader.toggleGlobalLoader(true);
        saveBtnEl.setAttribute("disabled", "");
        deleteBtnEl.setAttribute("disabled", "");
        backBtnEl.setAttribute("disabled", "");

        fetch(url, {
            method: "post",
            body: new FormData(form)
        })
        .then((response) => response.json())
        .then(function (data: any) {
            SiteLoader.toggleGlobalLoader(false);
            saveBtnEl.removeAttribute("disabled");
            deleteBtnEl.removeAttribute("disabled");
            backBtnEl.removeAttribute("disabled");
            SiteAlert.show("success", data.message, true);
            window.location.href = data.backUrl;
        })
        .catch((err) => {
            SiteLoader.toggleGlobalLoader(false);
            saveBtnEl.removeAttribute("disabled");
            deleteBtnEl.removeAttribute("disabled");
            backBtnEl.removeAttribute("disabled");
            SiteAlert.show("danger", err, true);
        });
    }

    /**
     *  Delete a calendar event.
     * */
    function deleteEvent(eventId: string) {
        const deleteUrl = document.getElementById("delete-entry-btn").dataset.url;
        const token = (document.getElementById("edit-cal-entry-form").querySelector("input[name=__RequestVerificationToken]") as HTMLInputElement).value;
        const saveBtnEl = document.getElementById("save-entry-btn");
        const deleteBtnEl = document.getElementById("delete-entry-btn");
        const backBtnEl = document.getElementById("back-to-cal-btn");
        const dataToSend = {
            "id": eventId,
            "__RequestVerificationToken": token
        };

        SiteLoader.toggleGlobalLoader(true);
        saveBtnEl.setAttribute("disabled", "");
        deleteBtnEl.setAttribute("disabled", "");
        backBtnEl.setAttribute("disabled", "");

        fetch(deleteUrl, {
            method: "post",
            body: JSON.stringify(dataToSend),
            headers: {
                "Content-Type": "application/json"
            }
        })
        .then((response) => response.json())
        .then(function (data: any) {
            SiteLoader.toggleGlobalLoader(false);
            saveBtnEl.removeAttribute("disabled");
            deleteBtnEl.removeAttribute("disabled");
            backBtnEl.removeAttribute("disabled");
            SiteAlert.show("success", data.message, true);
            window.location.href = data.backUrl;
        })
        .catch((err) => {
            SiteLoader.toggleGlobalLoader(false);
            saveBtnEl.removeAttribute("disabled");
            deleteBtnEl.removeAttribute("disabled");
            backBtnEl.removeAttribute("disabled");
            SiteAlert.show("danger", err, true);
        });
    }

    /*------------------------------------------------------------------------*\
        3. Export functions
    \*------------------------------------------------------------------------*/

    /**
     * Export a calendar event to Ical.
     * @param eventId
     */
    function exportEventToIcal(eventId: string, exportUrl: string) {
        window.location.href = `${exportUrl}/${eventId}`;
    }

    /**
     *  Export visible events on the calendar to ical.
     * */
    function exportVisibleEventsToIcal() {
        // Check if there are any events before going to the controller.
        if (!document.querySelector(".fc-view").classList.contains("fc-event")) {
            SiteAlert.show("info", "There are no events to sync.", true);
            return;
        }

        const dates = SiteCalendar.getVisibleDates(calendarSelector);
        let url = (document.getElementById("ExportIcalUrl") as HTMLInputElement).value;
        url = url.replace("start_placeholder", dates.start.toISOString()).replace("end_placeholder", dates.end.toISOString());
        window.location.href = url;
    }

    /**
     * Export calendar events to ics from a date range.
     * @param start
     * @param end
     */
    function exportEventsFromDateRangeToIcal(start: string, end: string) {
        // Check if dates were entered before going to the server.
        if (start == null || end == null) {
            SiteAlert.show("danger", "<strong>Error</strong>: No dates provided.", true);
            return;
        }

        let url = (document.getElementById("ExportIcalUrl") as HTMLInputElement).value;
        url = url.replace("start_placeholder", start).replace("end_placeholder", end);
        window.location.href = url;
    }

    /*------------------------------------------------------------------------*\
        4. Helper functions
    \*------------------------------------------------------------------------*/

    // Show quick create modal.
    function showQuickCreateModal(start: Date, end: Date) {
        SiteLoader.toggleGlobalLoader(true);

        // Format moment object to ISO 8601 so no date conversion errors are made on model bind.
        const startFormatted = start.toISOString();
        const endFormatted = end.toISOString();

        // Open create panel.
        fetch("/Scheduler/_QuickCreate/")
            .then(function (response) {
                return response.text();
            })
            .then(function (body) {
                document.querySelector("#quick-create-container").innerHTML = body;

                // Format date so it's human readable.
                (document.getElementById("DateStarting") as HTMLInputElement).value = moment(start).local().format("LLL");
                (document.getElementById("DateEnding") as HTMLInputElement).value = moment(end).local().format("LLL");
                const modalEl = document.getElementById("quick-create-modal");
                const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
                const formEl = document.getElementById("quick-create-form") as HTMLFormElement;

                // Show the modal.
                modal.show();

                // Show full create view.
                const editBtnEl = document.getElementById("edit-entry-btn");
                editBtnEl.onclick = function (event) {
                    event.preventDefault();

                    SiteLoader.toggleGlobalLoader(true);
                    const title = (document.getElementById("Title") as HTMLInputElement).value;

                    // Stop the modal from getting 'stuck'.
                    modalEl.addEventListener("hidden.bs.modal", function () {
                        Navigate.toCreateMoreOptions(title, startFormatted, endFormatted);
                        SiteLoader.toggleGlobalLoader(false);
                    });

                    // Close and empty the modal.
                    modal.hide();
                };

                // Submit form.
                formEl.onsubmit = function (event) {
                    event.preventDefault();
                    quickCreate();
                };

                SiteLoader.toggleGlobalLoader(false);
            });
    }

    /*------------------------------------------------------------------------*\
        5. Navigation functions
    \*------------------------------------------------------------------------*/

    /**
     * Contains navigation based functionality for the scheduler area.
     */
    namespace Navigate {
        /**
         * Navigate to the quick entry create page.
         * @param title
         * @param start
         * @param end
         */
        export function toCreateMoreOptions(title: string, start: string, end: string) {
            let url = (document.getElementById("CreateEventMoreOptionsUrl") as HTMLInputElement).value;
            url = url.replace("title_placeholder", title).replace("start_placeholder", start).replace("end_placeholder", end);
            window.location.href = url;
        }

        /**
         * Navigate to the edit event page.
         * @param id
         */
        export function toEdit(id: string) {
            let url = (document.getElementById("EditEventUrl") as HTMLInputElement).value;
            url = url.replace("id_placeholder", id);
            window.location.href = url;
        }
    }
}

// Initialise the scheduler module on page load.
document.addEventListener("DOMContentLoaded", function (event) {
    Scheduler.init();
});