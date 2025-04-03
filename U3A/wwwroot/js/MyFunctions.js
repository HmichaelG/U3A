
// Full screen / normal screen functions

function IsApple() {
    return (/iP(hone|od|ad)/.test(navigator.platform));
}

function toggleFullscreen() {
    var e = document.getElementById('fullscreen-element');
    if (e != null) {
        e.classList.toggle('normalscreen');
        e.classList.toggle('fullscreen');
    }
}

function refreshNormalscreen() {
    var e = document.getElementById('fullscreen-element');
    if (e != null) {
        e.classList.remove('normalscreen');
        e.classList.remove('fullscreen');
        e.classList.add('normalscreen');
    }
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

document.onreadystatechange = function (e) {
    if (document.readyState === 'complete') {
        setTheme();
    }
}

window.onload = function () {
    setTheme();
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
function setTheme() {
    // If we pass a theme in a query string then save it to localStorage
    var qs = getQueryStrings();
    var theme = qs['theme']
    if (theme) {
        localStorage.setItem("theme", theme);
    }
    var link = document.getElementById("theme");
    var href = link.href;
    if (href == null) { href = '_content/DevExpress.Blazor.Themes/office-white.bs5.min.css'; }
    // load from localStorage & replace the default
    theme = localStorage.getItem('theme');
    if (theme) {
        theme = theme.replace('"', '',);
        theme = theme.replace('"', '',);
        if (theme.startsWith("fluent")) {
            theme = theme.replace('.bs5', '',);
            // whenfluent theme is working correctly
            //    theme = 'window.matchMedia('(prefers-color-scheme: dark)').matches ? 'fluent-dark' : 'fluent-light'
        }
        href = href.replace('office-white', theme);
    }
    if (link) {
        link.href = href;
    }
}
function getQueryStrings() {
    var assoc = {};
    var decode = function (s) { return decodeURIComponent(s.replace(/\+/g, " ")); };
    var queryString = location.search.substring(1);
    var keyValues = queryString.split('&');

    for (var i in keyValues) {
        var key = keyValues[i].split('=');
        if (key.length > 1) {
            assoc[decode(key[0])] = decode(key[1]);
        }
    }
    return assoc;
}

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
