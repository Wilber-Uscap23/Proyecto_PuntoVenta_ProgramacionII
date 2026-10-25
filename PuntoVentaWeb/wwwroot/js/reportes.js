// ============================================
// DESCARGAR REPORTES CSV
// ============================================

// Recibe el archivo que Blazor obtuvo
// mediante una petición autenticada.
//
// No recibe ni almacena tokens JWT.

export async function descargarCsv(nombreArchivo, referenciaArchivo) {

    // Obtener los bytes del archivo.

    const contenido = await referenciaArchivo.arrayBuffer();

    // Crear el archivo CSV.

    const archivo = new Blob(
        [contenido],
        {
            type: "text/csv;charset=utf-8"
        }
    );

    // Crear una dirección temporal.

    const url = URL.createObjectURL(archivo);

    // Crear el enlace de descarga.

    const enlace = document.createElement("a");

    enlace.href = url;

    enlace.download = nombreArchivo;

    enlace.style.display = "none";

    // Descargar el archivo.

    document.body.appendChild(enlace);

    enlace.click();

    // Eliminar el enlace temporal.

    enlace.remove();

    // Liberar la dirección temporal.

    setTimeout(() => {

        URL.revokeObjectURL(url);

    }, 60000);
}