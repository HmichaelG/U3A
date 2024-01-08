//
// page timeout section

var blazorTimeout = {
    init: function (dotNetReference) {
        let timeoutId;
        DotNetObjectReference = dotNetReference;
        // Set up the event listeners
        document.addEventListener('touched', resetTimer);
        document.addEventListener('keydown', resetTimer);
        document.addEventListener('mousemove', resetTimer);
        // Start the timer
        resetTimer();

        // Reset the timer
        function resetTimer() {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(redirectToExitPage, 10 * 60 * 1000); // 10 minutes in milliseconds
        }

        // Redirect to the page
        function redirectToExitPage() {
            dotNetReference.invokeMethodAsync("OnTimeout");
        }
    }

};

