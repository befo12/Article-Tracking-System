using System;
using System.Data.SqlClient;
using System.Web.Http;

namespace WebApplication1.Controllers
{
    public class UsersController : ApiController
    {
        private string Cs = System.Configuration.ConfigurationManager
            .ConnectionStrings["Db"].ConnectionString;

        // POST api/users/register
        [HttpPost]
        public IHttpActionResult Register(UserDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest("E-posta ve şifre zorunludur.");
            }

            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand(
                @"IF EXISTS(SELECT 1 FROM Users WHERE Email=@e)
                    SELECT -1;
                  ELSE
                  BEGIN
                    INSERT INTO Users(Email,Password,Name)
                    VALUES (@e,@p,@n);
                    SELECT SCOPE_IDENTITY();
                  END", con))
            {
                cmd.Parameters.AddWithValue("@e", model.Email);
                cmd.Parameters.AddWithValue("@p", model.Password);
                cmd.Parameters.AddWithValue("@n", (object)model.Name ?? DBNull.Value);

                con.Open();
                var res = cmd.ExecuteScalar();

                if (Convert.ToInt32(res) == -1)
                    return BadRequest("Bu e-posta zaten kayıtlı.");

                return Ok(new { message = "Kayıt başarılı", userId = Convert.ToInt32(res) });
            }
        }

        // POST api/users/login
        [HttpPost]
        public IHttpActionResult Login(LoginDto model)
        {
            if (model == null)
                return BadRequest("MODEL NULL GELİYOR");

            if (string.IsNullOrWhiteSpace(model.Email))
                return BadRequest("EMAIL NULL GELİYOR");

            if (string.IsNullOrWhiteSpace(model.Password))
                return BadRequest("PASSWORD NULL GELİYOR");

            using (var con = new SqlConnection(Cs))
            using (var cmd = new SqlCommand(
                @"SELECT Id, Name FROM Users 
                  WHERE Email=@e AND Password=@p", con))
            {
                cmd.Parameters.AddWithValue("@e", model.Email);
                cmd.Parameters.AddWithValue("@p", model.Password);

                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read())
                        return BadRequest("Email veya şifre hatalı.");

                    int uid = Convert.ToInt32(r["Id"]);
                    string name = Convert.ToString(r["Name"]);

                    return Ok(new { userId = uid, displayName = name });
                }
            }
        }
    }

    public class UserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
