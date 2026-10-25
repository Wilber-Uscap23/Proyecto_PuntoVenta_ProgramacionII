
 // wwwroot/js/comprobantes.js
 // Ningún token JWT se almacena en este módulo.
 // Recibe únicamente los bytes del PDF que Blazor descargó de forma autenticada.

let secuencia = 0;
const ventanas = new Map();

function crearUrlPdf(base64) {
    const binario = atob(base64);
    const bytes = new Uint8Array(binario.length);

    for (let i = 0; i < binario.length; i++) {
        bytes[i] = binario.charCodeAt(i);
    }

    return URL.createObjectURL(
        new Blob([bytes], { type: "application/pdf" })
    );
}

export function prepararVistaPdf() {
    const ventana = window.open("about:blank", "_blank");

    if (!ventana) return null;

    const id = String(++secuencia);
    ventanas.set(id, ventana);

    ventana.document.title = "Preparando comprobante...";
    ventana.document.body.textContent =
        "Preparando comprobante PDF...";

    return id;
}

export function mostrarPdf(idVentana, base64) {
    const ventana = ventanas.get(idVentana);

    if (!ventana || ventana.closed) {
        throw new Error(
            "La ventana del comprobante fue cerrada."
        );
    }

    const urlPdf = crearUrlPdf(base64);

    ventanas.delete(idVentana);
    ventana.location.replace(urlPdf);

    // Liberar memoria después de cargar el visor.
    setTimeout(
        () => URL.revokeObjectURL(urlPdf),
        10 * 60 * 1000
    );
}

export function descargarPdf(base64, nombreArchivo) {
    const urlPdf = crearUrlPdf(base64);

    const enlace = document.createElement("a");

    enlace.href = urlPdf;
    enlace.download = nombreArchivo;
    enlace.style.display = "none";

    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();

    setTimeout(
        () => URL.revokeObjectURL(urlPdf),
        60 * 1000
    );
}

export function cerrarVistaPdf(idVentana) {
    const ventana = ventanas.get(idVentana);

    if (ventana && !ventana.closed) {
        ventana.close();
    }

    ventanas.delete(idVentana);
}
