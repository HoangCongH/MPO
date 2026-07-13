using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace MPO_Web_Prj.Controllers
{
    public class DataViewController : Controller
    {
        private readonly IConfiguration _config;
        public DataViewController (IConfiguration config)
        {
            _config = config;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public IActionResult AddMachine()
        {
            string connString = _config.GetConnectionString("Postgres");
            using var conn = new NpgsqlConnection(connString);

            conn.Open();

            string sql =
                @"INSERT INTO machine(name,line,time)
              VALUES(@name,@line,@time)";

            using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("name", "NPM-01");
            cmd.Parameters.AddWithValue("line", "LINE-01");
            cmd.Parameters.AddWithValue("time", DateTime.Now);
            cmd.ExecuteNonQuery();

            return RedirectToAction("DataView", "Dashboard");
        }
    }
}
