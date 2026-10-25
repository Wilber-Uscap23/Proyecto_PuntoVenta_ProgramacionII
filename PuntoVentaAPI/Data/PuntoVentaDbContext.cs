using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Models;

namespace PuntoVentaAPI.Data;

public partial class PuntoVentaDbContext : DbContext
{
    public PuntoVentaDbContext(DbContextOptions<PuntoVentaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Compra> Compras { get; set; }

    public virtual DbSet<ComprasDetalle> ComprasDetalles { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<Marca> Marcas { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Proveedore> Proveedores { get; set; }

    public virtual DbSet<Puesto> Puestos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Venta> Ventas { get; set; }

    public virtual DbSet<VentasDetalle> VentasDetalles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente).HasName("PRIMARY");

            entity.ToTable("clientes");

            entity.HasIndex(e => e.Correo, "correo").IsUnique();

            entity.HasIndex(e => e.Nit, "nit").IsUnique();

            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .HasColumnName("apellido");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .HasColumnName("direccion");
            entity.Property(e => e.Estado)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .HasColumnName("nit");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.IdCompra).HasName("PRIMARY");

            entity.ToTable("compras");

            entity.HasIndex(e => e.IdProveedor, "fk_compras_proveedores");

            entity.Property(e => e.IdCompra).HasColumnName("id_compra");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'REGISTRADA'")
                .HasColumnName("estado");
            entity.Property(e => e.FechaCompra)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_compra");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(255)
                .HasColumnName("observaciones");
            entity.Property(e => e.Total)
                .HasPrecision(12, 2)
                .HasColumnName("total");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_compras_proveedores");
        });

        modelBuilder.Entity<ComprasDetalle>(entity =>
        {
            entity.HasKey(e => e.IdDetalleCompra).HasName("PRIMARY");

            entity.ToTable("compras_detalle");

            entity.HasIndex(e => e.IdCompra, "fk_detalle_compra");

            entity.HasIndex(e => e.IdProducto, "fk_detalle_compra_producto");

            entity.Property(e => e.IdDetalleCompra).HasColumnName("id_detalle_compra");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.IdCompra).HasColumnName("id_compra");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.PrecioCosto)
                .HasPrecision(12, 2)
                .HasColumnName("precio_costo");
            entity.Property(e => e.Subtotal)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal");

            entity.HasOne(d => d.IdCompraNavigation).WithMany(p => p.ComprasDetalles)
                .HasForeignKey(d => d.IdCompra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detalle_compra");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.ComprasDetalles)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detalle_compra_producto");
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.IdEmpleado).HasName("PRIMARY");

            entity.ToTable("empleados");

            entity.HasIndex(e => e.Correo, "correo").IsUnique();

            entity.HasIndex(e => e.IdPuesto, "fk_empleados_puestos");

            entity.Property(e => e.IdEmpleado).HasColumnName("id_empleado");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .HasColumnName("apellido");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .HasColumnName("direccion");
            entity.Property(e => e.Estado)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.FechaIngreso).HasColumnName("fecha_ingreso");
            entity.Property(e => e.IdPuesto).HasColumnName("id_puesto");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");

            entity.HasOne(d => d.IdPuestoNavigation).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.IdPuesto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_empleados_puestos");
        });

        modelBuilder.Entity<Marca>(entity =>
        {
            entity.HasKey(e => e.IdMarca).HasName("PRIMARY");

            entity.ToTable("marcas");

            entity.HasIndex(e => e.Nombre, "nombre").IsUnique();

            entity.Property(e => e.IdMarca).HasColumnName("id_marca");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.IdMenu).HasName("PRIMARY");

            entity.ToTable("menu");

            entity.HasIndex(e => e.IdMenuPadre, "fk_menu_padre");

            entity.Property(e => e.IdMenu).HasColumnName("id_menu");
            entity.Property(e => e.Estado)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.Icono)
                .HasMaxLength(100)
                .HasColumnName("icono");
            entity.Property(e => e.IdMenuPadre).HasColumnName("id_menu_padre");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Orden).HasColumnName("orden");
            entity.Property(e => e.Ruta)
                .HasMaxLength(255)
                .HasColumnName("ruta");

            entity.HasOne(d => d.IdMenuPadreNavigation).WithMany(p => p.InverseIdMenuPadreNavigation)
                .HasForeignKey(d => d.IdMenuPadre)
                .HasConstraintName("fk_menu_padre");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PRIMARY");

            entity.ToTable("productos");

            entity.HasIndex(e => e.Codigo, "codigo").IsUnique();

            entity.HasIndex(e => e.IdMarca, "fk_productos_marcas");

            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .HasColumnName("codigo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.Existencia).HasColumnName("existencia");
            entity.Property(e => e.FechaIngreso)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_ingreso");
            entity.Property(e => e.IdMarca).HasColumnName("id_marca");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .HasColumnName("imagen_url");
            entity.Property(e => e.PrecioCosto)
                .HasPrecision(12, 2)
                .HasColumnName("precio_costo");
            entity.Property(e => e.PrecioVenta)
                .HasPrecision(12, 2)
                .HasColumnName("precio_venta");

            entity.HasOne(d => d.IdMarcaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdMarca)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_productos_marcas");
        });

        modelBuilder.Entity<Proveedore>(entity =>
        {
            entity.HasKey(e => e.IdProveedor).HasName("PRIMARY");

            entity.ToTable("proveedores");

            entity.HasIndex(e => e.Nit, "nit").IsUnique();

            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Contacto)
                .HasMaxLength(100)
                .HasColumnName("contacto");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .HasColumnName("direccion");
            entity.Property(e => e.Estado)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .HasColumnName("nit");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Puesto>(entity =>
        {
            entity.HasKey(e => e.IdPuesto).HasName("PRIMARY");

            entity.ToTable("puestos");

            entity.HasIndex(e => e.Nombre, "nombre").IsUnique();

            entity.Property(e => e.IdPuesto).HasColumnName("id_puesto");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PRIMARY");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.IdEmpleado, "uq_usuarios_empleado").IsUnique();

            entity.HasIndex(e => e.Usuario1, "usuario").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Estado)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.IdEmpleado).HasColumnName("id_empleado");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UltimoAcceso)
                .HasColumnType("datetime")
                .HasColumnName("ultimo_acceso");
            entity.Property(e => e.Usuario1)
                .HasMaxLength(50)
                .HasColumnName("usuario");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithOne(p => p.Usuario)
                .HasForeignKey<Usuario>(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_usuarios_empleados");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.IdVenta).HasName("PRIMARY");

            entity.ToTable("ventas");

            entity.HasIndex(e => e.IdCliente, "fk_ventas_clientes");

            entity.HasIndex(e => e.IdEmpleado, "fk_ventas_empleados");

            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'REGISTRADA'")
                .HasColumnName("estado");
            entity.Property(e => e.FechaVenta)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_venta");
            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdEmpleado).HasColumnName("id_empleado");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(255)
                .HasColumnName("observaciones");
            entity.Property(e => e.Total)
                .HasPrecision(12, 2)
                .HasColumnName("total");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ventas_clientes");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ventas_empleados");
        });

        modelBuilder.Entity<VentasDetalle>(entity =>
        {
            entity.HasKey(e => e.IdDetalleVenta).HasName("PRIMARY");

            entity.ToTable("ventas_detalle");

            entity.HasIndex(e => e.IdVenta, "fk_detalle_venta");

            entity.HasIndex(e => e.IdProducto, "fk_detalle_venta_producto");

            entity.Property(e => e.IdDetalleVenta).HasColumnName("id_detalle_venta");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.PrecioVenta)
                .HasPrecision(12, 2)
                .HasColumnName("precio_venta");
            entity.Property(e => e.Subtotal)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.VentasDetalles)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detalle_venta_producto");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.VentasDetalles)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detalle_venta");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
