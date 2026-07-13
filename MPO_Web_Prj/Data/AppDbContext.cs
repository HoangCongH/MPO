using System;
using System.Collections.Generic;
using MPO_Web_Prj.Models;
using Microsoft.EntityFrameworkCore;

namespace MPO_Web_Prj.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<feeder_log> feeder_logs { get; set; }

    public virtual DbSet<master_machine> master_machines { get; set; }

    public virtual DbSet<nozzle_log> nozzle_logs { get; set; }

    public virtual DbSet<production_report> production_reports { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<feeder_log>(entity =>
        {
            entity.HasKey(e => e.id).HasName("feeder_logs_pkey");

            entity.HasIndex(e => new { e.report_id, e.f_add }, "idx_feeder_report_f_add");

            entity.HasIndex(e => e.report_id, "idx_feeder_report_id");

            entity.Property(e => e.blk_code).HasMaxLength(100);
            entity.Property(e => e.blk_serial).HasMaxLength(100);
            entity.Property(e => e.f_add).HasMaxLength(20);
            entity.Property(e => e.part_name).HasMaxLength(100);
            entity.Property(e => e.reel_id).HasMaxLength(100);

            entity.HasOne(d => d.report).WithMany(p => p.feeder_logs)
                .HasForeignKey(d => d.report_id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("feeder_logs_report_id_fkey");
        });

        modelBuilder.Entity<master_machine>(entity =>
        {
            entity.HasKey(e => e.id).HasName("master_machines_pkey");

            entity.Property(e => e.id).HasMaxLength(50);
            entity.Property(e => e.machine_name).HasMaxLength(50);
            entity.Property(e => e.machine_type).HasMaxLength(20);
            entity.Property(e => e.version).HasPrecision(10, 5);
        });

        modelBuilder.Entity<nozzle_log>(entity =>
        {
            entity.HasKey(e => e.id).HasName("nozzle_logs_pkey");

            entity.HasIndex(e => new { e.report_id, e.head_num, e.nh_add }, "idx_nozzle_report_head");

            entity.HasIndex(e => e.report_id, "idx_nozzle_report_id");

            entity.Property(e => e.nc_add).HasMaxLength(100);
            entity.Property(e => e.nozzle_name).HasMaxLength(50);

            entity.HasOne(d => d.report).WithMany(p => p.nozzle_logs)
                .HasForeignKey(d => d.report_id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("nozzle_logs_report_id_fkey");
        });

        modelBuilder.Entity<production_report>(entity =>
        {
            entity.HasKey(e => e.id).HasName("production_reports_pkey");

            entity.HasIndex(e => e.report_date, "idx_report_date");

            entity.HasIndex(e => e.lot_name, "idx_report_lot_name");

            entity.HasIndex(e => e.machine_id, "idx_report_machine_id");

            entity.HasIndex(e => e.mjs_id, "idx_report_mjs_id");

            entity.Property(e => e.cycle_times).HasColumnType("jsonb");
            entity.Property(e => e.lot_name).HasMaxLength(100);
            entity.Property(e => e.machine_id).HasMaxLength(50);
            entity.Property(e => e.mjs_id).HasMaxLength(100);
            entity.Property(e => e.other_count_stats).HasColumnType("jsonb");
            entity.Property(e => e.other_rare_time_stats).HasColumnType("jsonb");
            entity.Property(e => e.product_id).HasMaxLength(50);
            entity.Property(e => e.report_date).HasColumnType("timestamp without time zone");
            entity.Property(e => e.time_actual).HasPrecision(10, 2);
            entity.Property(e => e.time_change).HasPrecision(10, 2);
            entity.Property(e => e.time_cperr).HasPrecision(10, 2);
            entity.Property(e => e.time_fwait).HasPrecision(10, 2);
            entity.Property(e => e.time_load).HasPrecision(10, 2);
            entity.Property(e => e.time_mcrwait).HasPrecision(10, 2);
            entity.Property(e => e.time_mount).HasPrecision(10, 2);
            entity.Property(e => e.time_power_on).HasPrecision(10, 2);
            entity.Property(e => e.time_prdstop).HasPrecision(10, 2);
            entity.Property(e => e.time_prod).HasPrecision(10, 2);
            entity.Property(e => e.time_pwait).HasPrecision(10, 2);
            entity.Property(e => e.time_rwait).HasPrecision(10, 2);
            entity.Property(e => e.time_total_stop).HasPrecision(10, 2);

            entity.HasOne(d => d.machine).WithMany(p => p.production_reports)
                .HasForeignKey(d => d.machine_id)
                .HasConstraintName("production_reports_machine_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
