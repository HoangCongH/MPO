using System;
using System.Collections.Generic;

namespace MPO_Web_Prj.Models;

public partial class feeder_log
{
    public long id { get; set; }

    public long? report_id { get; set; }

    public string? blk_code { get; set; }

    public string? blk_serial { get; set; }

    public string? part_name { get; set; }

    public string? f_add { get; set; }

    public short? fs_add { get; set; }

    public string? reel_id { get; set; }

    public int? f_pickup_qty { get; set; }

    public int? f_mount_qty { get; set; }

    public int? f_p_miss_qty { get; set; }

    public int? f_r_miss_qty { get; set; }

    public int? f_d_miss_qty { get; set; }

    public int? f_m_miss_qty { get; set; }

    public int? f_h_miss_qty { get; set; }

    public int? f_trs_miss_qty { get; set; }

    public virtual production_report? report { get; set; }
}
