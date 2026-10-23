-- =====================================================================
--  Datos de ejemplo de Marcas y Productos (módulo de Amalia).
--  Se ejecuta DESPUÉS de importar database/PuntoVentaDB.sql.
--  Es seguro ejecutarlo varias veces: INSERT IGNORE respeta los índices
--  únicos (marcas.nombre y productos.codigo).
--  precio_venta = precio_costo + 25 %  (regla del proyecto)
-- =====================================================================
USE `puntoventadb`;

INSERT IGNORE INTO `marcas` (`nombre`, `descripcion`, `estado`) VALUES
('Samsung',  'Electrónica y monitores', 1),
('LG',       'Electrónica y electrodomésticos', 1),
('HP',       'Computadoras e impresoras', 1),
('Dell',     'Computadoras y monitores', 1),
('Logitech', 'Periféricos: teclados y mouse', 1),
('Sony',     'Audio y video', 1);

INSERT IGNORE INTO `productos`
    (`codigo`, `descripcion`, `id_marca`, `precio_costo`, `precio_venta`, `existencia`, `fecha_ingreso`, `imagen_url`, `estado`)
SELECT p.codigo, p.descripcion, m.id_marca, p.costo, ROUND(p.costo * 1.25, 2), p.existencia, p.fecha, NULL, 1
FROM (
    SELECT 'MON-SAM-24' AS codigo, 'Monitor Samsung 24 pulgadas' AS descripcion, 'Samsung'  AS marca, 800.00 AS costo, 20 AS existencia, '2026-10-01' AS fecha UNION ALL
    SELECT 'MON-LG-27',  'Monitor LG 27 pulgadas 4K',        'LG',       1500.00, 10, '2026-10-01' UNION ALL
    SELECT 'LAP-HP-15',  'Laptop HP 15 pulgadas 8GB RAM',    'HP',       4200.00,  8, '2026-10-02' UNION ALL
    SELECT 'LAP-DEL-14', 'Laptop Dell Inspiron 14',          'Dell',     4800.00,  6, '2026-10-02' UNION ALL
    SELECT 'MOU-LOG-01', 'Mouse inalámbrico Logitech M185',  'Logitech',  95.00,  40, '2026-10-03' UNION ALL
    SELECT 'TEC-LOG-01', 'Teclado Logitech K380 Bluetooth',  'Logitech', 260.00,  25, '2026-10-03' UNION ALL
    SELECT 'AUD-SON-01', 'Audífonos Sony WH-CH520',          'Sony',     420.00,  15, '2026-10-04'
) AS p
JOIN `marcas` m ON m.nombre = p.marca;

-- Las imágenes NO se insertan aquí: se cargan desde la pantalla de Productos
-- (la base de datos solo guarda la ruta, ej. /images/productos/xxxx.jpg).
