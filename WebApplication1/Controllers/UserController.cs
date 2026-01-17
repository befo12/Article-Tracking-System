using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Http;

namespace WebApplication1.Controllers
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private string Cs = System.Configuration.ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        // ==========================================
        // 1. GİRİŞ (LOGIN)
        // ==========================================
        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(LoginDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return BadRequest("Email veya şifre eksik.");

            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand("SELECT Id, Name FROM Users WHERE Email=@e AND Password=@p", con))
            {
                cmd.Parameters.AddWithValue("@e", model.Email);
                cmd.Parameters.AddWithValue("@p", model.Password);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return BadRequest("Hatalı bilgiler.");

                    int uid = Convert.ToInt32(r["Id"]);
                    string name = r["Name"].ToString();

                    if (HttpContext.Current.Session != null)
                    {
                        HttpContext.Current.Session["UserId"] = uid;
                        HttpContext.Current.Session["displayName"] = name;
                        HttpContext.Current.Session["Email"] = model.Email;
                    }

                    return Ok(new { userId = uid, displayName = name });
                }
            }
        }

     
        // ==========================================
        // 5. KAYIT OL (REGISTER)
        // ==========================================
        [HttpPost]
        [Route("register")]
        public IHttpActionResult Register(UserDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email)) return BadRequest("Eksik bilgi.");
            try
            {
                using (var con = new SqlConnection(Cs))
                using (var cmd = new SqlCommand("INSERT INTO Users(Email,Password,Name) VALUES(@e,@p,@n)", con))
                {
                    cmd.Parameters.AddWithValue("@e", model.Email);
                    cmd.Parameters.AddWithValue("@p", model.Password);
                    cmd.Parameters.AddWithValue("@n", (object)model.Name ?? DBNull.Value);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }
    }

    // --- DTO MODELLERİ ---
    public class UserDto { public string Email { get; set; } public string Password { get; set; } public string Name { get; set; } }
    public class LoginDto { public string Email { get; set; } public string Password { get; set; } }
}