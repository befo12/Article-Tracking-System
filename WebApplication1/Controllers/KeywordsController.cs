using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Http;

namespace WebApplication1.Controllers
{
    [RoutePrefix("api/keywords")]
    public class KeywordsController : ApiController
    {
        private string Cs = System.Configuration.ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        [HttpGet]
        [Route("get")]
        public IHttpActionResult GetKeywords(int userId)
        {
            var list = new List<object>();
            using (var con = new SqlConnection(Cs))
            {
                var cmd = new SqlCommand("SELECT Id, Keyword FROM UserKeywords WHERE UserId=@u", con);
                cmd.Parameters.AddWithValue("@u", userId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(new { Id = r["Id"], Keyword = r["Keyword"].ToString() });
            }
            return Ok(list);
        }

        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddKeyword([FromUri] int userId, [FromBody] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return BadRequest();
            using (var con = new SqlConnection(Cs))
            {
                var cmd = new SqlCommand("IF NOT EXISTS(SELECT 1 FROM UserKeywords WHERE UserId=@u AND Keyword=@k) INSERT INTO UserKeywords(UserId,Keyword) VALUES(@u,@k)", con);
                cmd.Parameters.AddWithValue("@u", userId);
                cmd.Parameters.AddWithValue("@k", keyword);
                con.Open(); cmd.ExecuteNonQuery();
            }
            return Ok();
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public IHttpActionResult DeleteKeyword(int id, int userId)
        {
            using (var con = new SqlConnection(Cs))
            {
                var cmd = new SqlCommand("DELETE FROM UserKeywords WHERE Id=@id AND UserId=@u", con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@u", userId);
                con.Open();
                return cmd.ExecuteNonQuery() > 0 ? (IHttpActionResult)Ok() : NotFound();
            }
        }
    }
}