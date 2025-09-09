
// return true if text overflows the given maxHeight (e.g. "200px" or "50vh") 
window.checkOverflow = (element, maxHeight) => {
    if (!element) return false;

    const computedStyle = getComputedStyle(element);
    const max = parseInt(computedStyle.maxHeight.replace(/[^0-9]/g, ''));
    const actual = element.scrollHeight;

    return actual > max;
};

// Orientation Change

function registerOrientationChange(dotNetHelper) {
    window.addEventListener("orientationchange", () => {
        dotNetHelper.invokeMethodAsync("OnOrientationChanged", screen.orientation.type);
    });
}

// menu button hover click
window.hoverClickMenu = (() => {
    let lastClickTime = 0;
    let hoverTimer = null;
    const CLICK_COOLDOWN = 750;

    function getAdaptiveDelay() {
        const usedBefore = sessionStorage.getItem('menuUsed') === 'true';
        return usedBefore ? 500 : 1200;
    }

    return {
        attachAdaptiveHover: function (elementId) {
            const menu = document.getElementById(elementId);
            if (!menu) return;

            // Prevent manual double-clicks
            menu.addEventListener('click', (e) => {
                const now = Date.now();
                if (now - lastClickTime < CLICK_COOLDOWN) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    return false;
                }
                lastClickTime = now;
                sessionStorage.setItem('menuUsed', 'true');
            });

            menu.addEventListener('mouseenter', () => {
                const delay = getAdaptiveDelay();

                hoverTimer = setTimeout(() => {
                    const now = Date.now();
                    if (now - lastClickTime >= CLICK_COOLDOWN) {
                        menu.click();
                        lastClickTime = now;
                        sessionStorage.setItem('menuUsed', 'true');
                    }
                }, delay);
            });

            menu.addEventListener('mouseleave', () => {
                clearTimeout(hoverTimer);
            });
        }
    };
})();

// activate menu on HOME button click
window.hoverClickMenu = window.hoverClickMenu || {};
window.hoverClickMenu.attachHomeKeyHandler = function (elementId) {
    document.addEventListener('keydown', function (event) {
        if (event.code === 'Escape') {
            const menu = document.getElementById(elementId);
            if (menu) {
                menu.click();
            }
        }
    });
};

function IsApple() {
    return (/iP(hone|od|ad)/.test(navigator.platform));
}

window.getWindowDimensions = function () {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    };
};

function getLocalStorage(key) {
    var result = localStorage.getItem(key);
    return result == 'true';
}

function setFocus(id) {
    let elem = document.getElementById(id);
    if (elem != null) {
        setTimeout(() => elem.focus(), 1000);
    }
}

function ScrollToElementId(id) {
    const element = document.getElementById(id);
    if (element instanceof HTMLElement) {
        element.scrollIntoView({
            behavior: "smooth",
            block: "start",
            inline: "nearest"
        });
    }
}

function getWindowBounds(id) {
    let elem = document.getElementById(id);
    if (!elem) return null;
    const rect = elem.getBoundingClientRect();
    if (!rect == null) return null;
    return {
        top: rect.top,
        left: rect.left,
        bottom: rect.bottom,
        right: rect.right,
        width: rect.width,
        height: rect.height
    };
}


function ScrollToTop(id) {
    var myDiv = document.getElementById(id);
    if (myDiv !== null) {
        window.setTimeout(function () {
            myDiv.scrollTop = 0;
            myDiv.scrollIntoView();
            setTimeout(() => myDiv.scrollBy(0, 1), 0);
        }, 0);
    }
}

function GetSessionState(key) {
    return window.sessionStorage.getItem(key);
}
function GetLocalStorage(key) {
    return window.localStorage.getItem(key);
}

window.onload = function () {
    displayNonInteractive();
}

function displayNonInteractive() {
    setTimeout(function () {
        let element = document.getElementById("notInteractive");
        if (element != null) {
            element.style.display = "block";
        }
    }, 10000);
};

window.cookieInterop = {
    setCookie: function (name, value, days) {
        let expires = "";
        if (days) {
            const date = new Date();
            date.setTime(date.getTime() + (days * 86400000));
            expires = "; expires=" + date.toUTCString();
        }
        document.cookie = name + "=" + value + expires + "; path=/";
    },
    getCookie: function (name) {
        const nameEQ = name + "=";
        const ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i].trim();
            if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length);
        }
        return null;
    }
};

window.clipboardCopy = {
    copyText: function (text) {
        navigator.clipboard.writeText(text).then(function () {
        })
            .catch(function (error) {
                alert(error);
            });
    }
};

function clearQueryString() {
    window.history.pushState({}, document.title, "/");
}

function getGridWidth() {
    const grid = document.querySelector(".dxbl-grid");
    const width = grid ? grid.offsetWidth : 900;
    return width;
}

function getGridColumnWidths() {
    const gridMap = new Map();
    var grid = document.querySelector(".dxbl-grid");
    var headerCells = grid.getElementsByClassName("my-header-cell");
    for (const headerCell of headerCells) {
        var fieldName = headerCell.getAttribute("fieldname");
        if (fieldName) {
            gridMap.set(fieldName, headerCell.offsetWidth);
        }
    }
    return Object.fromEntries(gridMap);
}

function getGridColumnWidths(gridSelector) {
    const gridMap = new Map();
    var grids = document.querySelectorAll(gridSelector);
    for (const grid of grids) {
        var headerCells = grid.getElementsByClassName("my-header-cell");
        for (const headerCell of headerCells) {
            console.log(headerCell);
            var fieldName = headerCell.getAttribute("fieldname");
            if (fieldName) {
                gridMap.set(fieldName, headerCell.offsetWidth);
            }
        }
    }
    return Object.fromEntries(gridMap);
}
