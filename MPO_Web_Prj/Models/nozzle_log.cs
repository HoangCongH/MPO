using System;
using System.Collections.Generic;

namespace MPO_Web_Prj.Models;

public partial class nozzle_log
{
    public long id { get; set; }

    public long? report_id { get; set; }

    public short? head_num { get; set; }

    public short? nh_add { get; set; }

    public string? nc_add { get; set; }

    public string? nozzle_name { get; set; }

    public int? n_pickup_qty { get; set; }

    public int? n_mount_qty { get; set; }

    public int? n_p_miss_qty { get; set; }

    public int? n_r_miss_qty { get; set; }

    public int? n_d_miss_qty { get; set; }

    public int? n_m_miss_qty { get; set; }

    public int? n_h_miss_qty { get; set; }

    public int? n_trs_miss_qty { get; set; }

    public virtual production_report? report { get; set; }
}
