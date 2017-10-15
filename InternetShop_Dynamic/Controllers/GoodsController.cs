using InternetShop_Dynamic.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
            catch(Exception e)
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
            catch(Exception e)
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
                using(SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
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
            catch(Exception e)
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
            catch(Exception e)
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

    }
}
