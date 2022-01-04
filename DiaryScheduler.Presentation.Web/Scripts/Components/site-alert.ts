/*----------------------------------------------------------------------------*\
    
    Site alert scripts
    ------------------

    All js used for the site alerts can be found here.
    
\*----------------------------------------------------------------------------*/

/**
 *  This module contains all the logic required for the site alert component.
 * */
export namespace SiteAlert {
    /**
     *  The alert container id.
     * */
    const alertContainerId = "alert-container";

    /**
     *  The timeout in milliseconds.
     * */
    const timeoutMs = 8000;

    /**
     * Show an alert inside the site alert container.
     * @param {string} type Alert type - "danger", "success", "info", "warning".
     * @param {string} message The alert message
     * @param {boolean} timeout Whether to auto dismiss the alert after 8 seconds (Defaults to false if not passed).
     */
    export function show(type: string, message: string, timeout?: boolean) {
        // Create the alert.
        const alertContainer = document.getElementById(alertContainerId);
        const alertId = "alert-" + Math.random().toString(16).slice(2);
        const alertString = `<div class="alert alert-${type} alert-dismissible fade show" id="${alertId}" role="alert">
                                ${message}
                                <button aria-label="Close" class="btn-close" data-bs-dismiss="alert" type="button"></button>
                            </div>`;
        const alertEl = new DOMParser().parseFromString(alertString, "text/html");

        // If the alert has been set to hide, then automatically dismiss it after 8 seconds.
        alertContainer.appendChild(alertEl.body);
        if (timeout) {
            setTimeout(() => { hide(alertId); }, timeoutMs);
        }
    }

    /**
     * Helper function to hide alerts.
     * @param {string} alertId The alert id.
     */
    export function hide(alertId: string) {
        document.getElementById(alertId).remove();
    }
};