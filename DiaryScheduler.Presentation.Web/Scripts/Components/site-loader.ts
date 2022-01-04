/*----------------------------------------------------------------------------*\
    
    Site loader scripts
    --------------------

    All js used for the site loader can be found here.
    
\*----------------------------------------------------------------------------*/

export namespace SiteLoader {

    /**
     * Show the loader in the given container.
     * @param selector
     */
    export function show(selector: string) {
        const loaderElement = document.getElementById(`site-loader-${selector.substr(1)}`);
        if (typeof (loaderElement) != "undefined" && loaderElement != null) {
            return;
        }

        const selectorElement = document.querySelector(selector);
        selectorElement.insertAdjacentHTML("beforeend",
            "<div class=\"site-loader-backdrop load-in-container\" id=\"site-loader-" + selector.substr(1) + "\">" +
                "<div class=\"site-loader-container\">" +
                    "<div class=\"site-loader\"></div>" +
                "</div>" +
            "</div>");

        selectorElement.classList.add("overflow-hidden position-relative");
    }

    /**
     * Remove the loader.
     * @param selector
     */
    export function remove(selector: string) {
        const loaderElement = document.getElementById(`site-loader-${selector.substr(1)}`);
        if (loaderElement) {
            loaderElement.remove();
        }

        // Remove .overflow-hidden regardless as the loader maybe lost by the parents html being replaced.
        document.querySelector(selector).classList.remove("overflow-hidden position-relative");
    }

    /**
     * Toggle visibility of the site loader.
     * @param show true = show the loader / false = hide the loader.
     */
    export function toggleGlobalLoader(show: boolean) {
        if (show) {
            document.getElementById("loader-wrapper").classList.remove("hidden");
        }
        else {
            document.getElementById("loader-wrapper").classList.add("hidden");
        }
    }
}