using System;
using System.Collections.Generic;

namespace MPO_Web_Prj.Models;

public partial class production_report
{
    public long id { get; set; }

    public string? machine_id { get; set; }

    public DateTime? report_date { get; set; }

    public string? mjs_id { get; set; }

    public string? product_id { get; set; }

    public string? lot_name { get; set; }

    public int? output_qty { get; set; }

    public decimal? time_power_on { get; set; }

    public decimal? time_change { get; set; }

    public decimal? time_prod { get; set; }

    public decimal? time_actual { get; set; }

    public decimal? time_load { get; set; }

    public decimal? time_mount { get; set; }

    public decimal? time_total_stop { get; set; }

    public decimal? time_fwait { get; set; }

    public decimal? time_rwait { get; set; }

    public decimal? time_pwait { get; set; }

    public decimal? time_cperr { get; set; }

    public decimal? time_prdstop { get; set; }

    public decimal? time_mcrwait { get; set; }

    public int? count_board { get; set; }

    public int? count_pickup { get; set; }

    public int? count_mount { get; set; }

    public int? count_p_miss { get; set; }

    public int? count_m_miss { get; set; }

    public string? other_rare_time_stats { get; set; }

    public string? cycle_times { get; set; }

    public string? other_count_stats { get; set; }

    public virtual ICollection<feeder_log> feeder_logs { get; set; } = new List<feeder_log>();

    public virtual master_machine? machine { get; set; }

    public virtual ICollection<nozzle_log> nozzle_logs { get; set; } = new List<nozzle_log>();
}
