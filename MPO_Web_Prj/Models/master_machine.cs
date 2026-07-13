using System;
using System.Collections.Generic;

namespace MPO_Web_Prj.Models;

public partial class master_machine
{
    public string id { get; set; } = null!;

    public string? machine_name { get; set; }

    public string? machine_type { get; set; }

    public decimal? version { get; set; }

    public short? lane { get; set; }

    public short? stage { get; set; }

    public virtual ICollection<production_report> production_reports { get; set; } = new List<production_report>();
}
