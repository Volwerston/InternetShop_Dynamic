using InternetShop_Dynamic.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace InternetShop_Dynamic.Controllers
{
    [RoutePrefix("api/Log")]
    public class LogController : ApiController
    {
        [Route("LogPurchaseItems")]
        public async Task<IHttpActionResult> LogPurchaseItems(List<UserLogItem> items)
        {
            try
            {
                StringBuilder cmdString = new StringBuilder();
                Dictionary<string, object> parameters = new Dictionary<string, object>();

                int counter = 0;

                foreach (var item in items)
                {
                    string toAppend = string.Format("INSERT INTO UserPurchaseLog VALUES(@uid{0}, @t{0}, @i{0}, @p{0}, @pt{0});", counter);

                    parameters.Add("@uid" + counter, User.Identity.GetUserId());
                    parameters.Add("@t" + counter, item.Title);
                    parameters.Add("@i" + counter, item.Items);
                    parameters.Add("@p" + counter, item.Price);
                    parameters.Add("@pt" + counter, item.PurchaseTime);

                    cmdString.Append(toAppend);

                    ++counter;
                }

                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(cmdString.ToString(), con))
                    {
                        foreach (var parameter in parameters)
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }

                        await con.OpenAsync();
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
    }
}