using InternetShop_Dynamic.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;

namespace InternetShop_Dynamic.Controllers
{
    [RoutePrefix("api/Goods")]
    public class GoodsController : ApiController
    {

        // GET /api/Goods/Goods
        [Route("Goods")]
        [HttpGet]
        public async Task<IHttpActionResult> Goods()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("select * from Goods", con))
                    {
                        con.Open();

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            List<Good> goods = new List<Good>();

                            while (rdr.Read())
                            {
                                Good g = new Good();

                                g.Id = Convert.ToInt32(rdr["Id"].ToString());
                                g.Title = rdr["Title"].ToString();
                                g.Description = rdr["Description"].ToString();
                                g.Price = Convert.ToDouble(rdr["Price"].ToString());
                                g.Discount = rdr["Discount"] == DBNull.Value ? 0 : Convert.ToDouble(rdr["Discount"].ToString());
                                g.CategoryId = Convert.ToInt32(rdr["CategoryId"].ToString());
                                g.IsInStock = Convert.ToBoolean(rdr["InStock"].ToString());

                                goods.Add(g);
                            }

                            return Ok(goods);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        // POST /api/Goods/AddGood
        [Authorize]
        [Route("AddGood")]
        public async Task<IHttpActionResult> AddGood(Good g)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = "INSERT INTO Goods VALUES(@t,@d,@p,@di,@is,@ci);";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@t", g.Title);
                        cmd.Parameters.AddWithValue("@d", g.Description);
                        cmd.Parameters.AddWithValue("@p", g.Price);
                        cmd.Parameters.AddWithValue("@di", g.Discount);
                        cmd.Parameters.AddWithValue("@is", g.IsInStock);
                        cmd.Parameters.AddWithValue("@ci", g.CategoryId);

                        con.Open();
                        await cmd.ExecuteNonQueryAsync();

                        return Ok();
                    }
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        //POST /api/Goods/Search
        [Route("Search")]
        public async Task<IHttpActionResult> Search(JObject options)
        {
            try
            {
                StringBuilder query = BuildRequestQuery(options);

                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query.ToString(), con))
                    {
                        con.Open();

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            List<Good> toReturn = new List<Good>();

                            while (rdr.Read())
                            {
                                Good g = new Good();

                                g.Id = Convert.ToInt32(rdr["Id"].ToString());
                                g.Title = rdr["Title"].ToString();
                                g.Description = rdr["Description"].ToString();
                                g.Price = Convert.ToDouble(rdr["Price"].ToString());
                                g.Discount = rdr["Discount"] == DBNull.Value ? 0 : Convert.ToDouble(rdr["Discount"].ToString());
                                g.IsInStock = Convert.ToBoolean(rdr["InStock"].ToString());

                                toReturn.Add(g);
                            }

                            return Ok(toReturn);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        //PUT /api/Goods/EditGood
        [Authorize]
        [Route("EditGood")]
        [HttpPut]
        public async Task<IHttpActionResult> EditGood(Good g)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = "Update Goods Set Title=@t, Description=@d, InStock=@is, Price=@p, Discount=@di, CategoryId=@ci Where Id=@id";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@id", g.Id);
                        cmd.Parameters.AddWithValue("@t", g.Title);
                        cmd.Parameters.AddWithValue("@d", g.Description);
                        cmd.Parameters.AddWithValue("@is", g.IsInStock);
                        cmd.Parameters.AddWithValue("@p", g.Price);
                        cmd.Parameters.AddWithValue("@di", g.Discount);
                        cmd.Parameters.AddWithValue("@ci", g.CategoryId);

                        con.Open();

                        await cmd.ExecuteNonQueryAsync();
                        return Ok();
                    }
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Authorize]
        [HttpDelete]
        [Route("DeleteGood")]
        public async Task<IHttpActionResult> DeleteGood(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("delete from Goods Where Id=@id", con))
                    {
                        con.Open();
                        cmd.Parameters.AddWithValue("@id", id);

                        await cmd.ExecuteNonQueryAsync();

                        return Ok();
                    }
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        // GET /api/Goods/Categories
        [Route("Categories")]
        [HttpGet]
        public async Task<IHttpActionResult> Categories()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("select * from Categories", con))
                    {
                        con.Open();

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            List<Category> categories = new List<Category>();

                            while (rdr.Read())
                            {
                                Category c = new Category();

                                c.Id = Convert.ToInt32(rdr["Id"].ToString());
                                c.Title = rdr["Title"].ToString();

                                categories.Add(c);
                            }

                            return Ok(categories);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        //GET /api/Goods/Good/{id}
        [Route("Good/{id}")]
        [HttpGet]
        public async Task<IHttpActionResult> GetGood(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = "select * from Goods Where Id = @id";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        await con.OpenAsync();

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            if (rdr.Read())
                            {
                                Good g = new Good();

                                g.Id = Convert.ToInt32(rdr["Id"].ToString());
                                g.Title = rdr["Title"].ToString();
                                g.Description = rdr["Description"].ToString();
                                g.Price = Convert.ToDouble(rdr["Price"].ToString());
                                g.Discount = rdr["Discount"] == DBNull.Value ? 0 : Convert.ToDouble(rdr["Discount"].ToString());
                                g.CategoryId = Convert.ToInt32(rdr["CategoryId"].ToString());
                                g.IsInStock = Convert.ToBoolean(rdr["InStock"].ToString());

                                return Ok(g);
                            }
                            else
                            {
                                throw new Exception("Good with this Id not found");
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        //PUT /api/Goods/AddPhoto/{id}
        [Route("AddPhoto/{id}")]
        [HttpPut]
        public async Task<IHttpActionResult> AddPhoto(int id, byte[] photo)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = @"If(Exists(Select Id from Goods_Details where GoodId = @id))
                                    Begin
                                    Update Goods_Details Set Photo=@p Where GoodId=@id
                                    End
                                    Else Begin 
                                    Insert Into Goods_Details Values(@id, @p)
                                    End";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@p", photo);

                        con.Open();

                        await cmd.ExecuteNonQueryAsync();

                        return Ok();
                    }
                }
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        //GET /api/Goods/Photo/{id}
        [Route("Photo/{id}")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IHttpActionResult> GetPhoto(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = "Select Photo from Goods_Details Where GoodId=@id";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        con.Open();

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            if (rdr.Read())
                            {
                                byte[] img = (byte[])rdr["Photo"];
                                string toReturn = "data:image/jpg;base64, " + Convert.ToBase64String(img);

                                return Ok(toReturn);
                            }
                            else
                            {
                                throw new Exception("Image not found.");
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        //GET /api/Goods/All
        [Route("All")]
        [HttpGet]
        public async Task<IHttpActionResult> All()
        {
            try
            {
                var goods = await Goods();
                var categories = await Categories();

                List<Good> item1 = new List<Good>();
                List<Category> item2 = new List<Category>();

                if (goods is OkNegotiatedContentResult<List<Good>>)
                {
                    item1 = (goods as OkNegotiatedContentResult<List<Good>>)
                        .Content;
                    
                }

                if (categories is OkNegotiatedContentResult<List<Category>>)
                {
                    item2 =
                        (categories as OkNegotiatedContentResult<List<Category>>)
                        .Content;     
                }

                return Ok(new Tuple<List<Good>, List<Category>>(item1, item2));
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        #region Helper Functions

        private StringBuilder BuildRequestQuery(JObject options)
        {
            StringBuilder query = new StringBuilder();

            int lastId = options["LastId"].Value<int>();
            string title = options["Title"].Value<string>();
            string category = options["Category"].Value<string>();

            if (category == "0"
                && string.IsNullOrEmpty(title))
            {
                query.Append(string.Format("select top 10 * from Goods where (Id>{0})", lastId));
            }
            else if (category == "0"
                && !string.IsNullOrEmpty(title))
            {
                query.Append(string.Format("select top 10 * from Goods where (Id>{0}) and (Title like '%{1}%')", lastId, title));
            }
            else if (category != "0"
                && string.IsNullOrEmpty(title))
            {
                query.Append(string.Format("select top 10 A.Title, A.Description, A.Price, A.Discount, A.Id, A.InStock from Goods A inner join Categories B on A.CategoryId=B.Id where (A.Id>{0}) And (B.Id = {1})", lastId, category));
            }
            else
            {
                query.Append(string.Format("select top 10 A.Title, A.Description, A.Price, A.Discount, A.Id, A.InStock from Goods A inner join Categories B on A.CategoryId=B.Id where (A.Id>{0}) And (B.Id = {1}) And (A.Title like '%{2}%')", lastId, category, title));
            }

            return query;
        }

        #endregion

    }
}
