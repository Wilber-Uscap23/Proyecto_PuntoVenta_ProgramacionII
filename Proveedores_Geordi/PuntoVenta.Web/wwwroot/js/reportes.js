// Módulo: Reportes — Rodrigo Rodríguez
// Se carga como módulo desde Blazor (IJSRuntime "import"), no hace falta
// agregar <script> en App.razor.

/** Descarga un archivo generado en Blazor (por ejemplo el CSV del reporte). */
export function descargarArchivo(nombreArchivo, contenido, tipo) {
    const blob = new Blob([contenido], { type: tipo || "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = nombreArchivo;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/** Abre el cuadro de impresión del navegador (desde ahí también se puede "Guardar como PDF"). */
export function imprimir() {
    window.print();
}
