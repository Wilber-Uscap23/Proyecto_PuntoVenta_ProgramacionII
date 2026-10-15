-- Script MySQL (alternativa a las migraciones de EF Core).
-- 'Productos' es la tabla coordinada con Wilber; debe existir antes de Compras_Detalle.
CREATE TABLE IF NOT EXISTS Proveedores (
  id_proveedor INT AUTO_INCREMENT PRIMARY KEY,
  nombre    VARCHAR(100) NOT NULL,
  nit       VARCHAR(20)  NOT NULL,
  telefono  VARCHAR(20)  NOT NULL,
  direccion VARCHAR(200) NOT NULL,
  correo    VARCHAR(100) NOT NULL,
  UNIQUE KEY uq_proveedores_nit (nit)
);

CREATE TABLE IF NOT EXISTS Compras (
  id_compra    INT AUTO_INCREMENT PRIMARY KEY,
  fecha        DATETIME NOT NULL,
  id_proveedor INT NOT NULL,
  total        DECIMAL(18,2) NOT NULL,
  CONSTRAINT fk_compras_proveedor FOREIGN KEY (id_proveedor) REFERENCES Proveedores(id_proveedor)
);

CREATE TABLE IF NOT EXISTS Compras_Detalle (
  id_compra_detalle INT AUTO_INCREMENT PRIMARY KEY,
  id_compra    INT NOT NULL,
  id_producto  INT NOT NULL,
  cantidad     INT NOT NULL CHECK (cantidad > 0),
  precio_costo DECIMAL(18,2) NOT NULL CHECK (precio_costo > 0),
  subtotal     DECIMAL(18,2) NOT NULL,
  CONSTRAINT fk_cd_compra   FOREIGN KEY (id_compra)   REFERENCES Compras(id_compra) ON DELETE CASCADE,
  CONSTRAINT fk_cd_producto FOREIGN KEY (id_producto) REFERENCES Productos(id_producto)
);
