window.addEventListener("load", () => {
    var element = document.getElementById("qrCodeData");
    if (element != null) {
        const uri = document.getElementById("qrCodeData").getAttribute('data-url');
        new QRCode(document.getElementById("qrCode"),
            {
                text: uri,
                width: 150,
                height: 150
            });
    }
});

function setQRCode(requiredWidth) {
    var element = document.getElementById("qrCodeData");
    if (element != null) {
        const uri = document.getElementById("qrCodeData").getAttribute('data-url');
        document.getElementById("qrCode").innerText = '';
        new QRCode(document.getElementById("qrCode"),
            {
                text: uri,
                width: requiredWidth,
                height: requiredWidth
            });
    }
}