using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using InternetShop_Dynamic.Models;
using InternetShop_Dynamic.Providers;
using InternetShop_Dynamic.Results;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Text;
using System.Net;

namespace InternetShop_Dynamic.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo()
        {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            return new UserInfoViewModel
            {
                Email = User.Identity.GetUserName(),
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin != null ? externalLogin.LoginProvider : null
            };
        }

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        //GET /api/Account/FindUsers
        [Route("FindUsers")]
        [HttpGet]
        public async Task<HttpResponseMessage> FindUsers([FromUri] string id)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                Dictionary<string, object> cmdParameters = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(id) &&  id!="all")
                {
                    sb.Append("select * from dbo.AspNetUsers where Id=@id");
                    cmdParameters.Add("@id", id);
                }
                else
                {
                    sb.Append("select * from dbo.AspNetUsers");
                }

                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), con))
                    {

                        foreach(var kvp in cmdParameters)
                        {
                            cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);
                        }

                        con.Open();

                        List<UserDto> users = new List<UserDto>();

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (rdr.Read())
                            {
                                UserDto u = new UserDto();

                                u.Id = rdr["Id"].ToString();
                                u.Email = rdr["Email"].ToString();

                                users.Add(u);
                            }

                            return Request.CreateResponse(HttpStatusCode.OK, users);
                        }
                    }
                }

            }
            catch(Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [Route("AddPasswordRestorationEntry")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Add(string email)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = "Insert into RestorePasswordLog Values(@uid, @em, @d)";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {

                        Guid guid = Guid.NewGuid();

                        cmd.Parameters.AddWithValue("@uid", guid.ToString());
                        cmd.Parameters.AddWithValue("@em", email);
                        cmd.Parameters.AddWithValue("@d", DateTime.Now);

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();

                        return Request.CreateResponse(HttpStatusCode.OK, guid.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("EmailExists")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> EmailExists([FromUri] string email)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = "Select Count(Id) from AspNetUsers Where Email=@mail";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@mail", email);

                        await con.OpenAsync();

                        bool res = (int)(await cmd.ExecuteScalarAsync()) > 0;

                        return Request.CreateResponse(HttpStatusCode.OK, res);
                    }
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        //GET /api/Account/GetPasswordRestoreEntry
        [Route("GetPasswordRestoreEntry")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetPasswordRestoreEntry([FromUri] string guid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = @"select A.Guid, A.Email, A.AddingTime, B.Id as UserId
                                        from RestorePasswordLog A
                                        inner join AspNetUsers B
                                        On A.Email = B.Email
                                        where A.Guid=@guid";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@guid", guid);

                        await con.OpenAsync();

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            RestorePasswordEntry en = new RestorePasswordEntry();

                            if (rdr.Read())
                            {
                                en.UserId = rdr["UserId"].ToString();
                                en.AddingTime = Convert.ToDateTime(rdr["AddingTime"].ToString());
                                en.Email = rdr["Email"].ToString();
                                en.Guid = rdr["Guid"].ToString();
                            }


                            return Request.CreateResponse(HttpStatusCode.OK, en);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        // POST api/Account/ChangePassword
        [Route("ChangePassword")]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ChangePassword(ChangePasswordBindingModel model)
        {
            try
            {
                IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                    model.NewPassword);

                if (!result.Succeeded)
                {
                    return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest, new List<string>(result.Errors));
                }

                return Request.CreateResponse(System.Net.HttpStatusCode.OK, "OK");
            }
            catch (Exception e)
            {
                List<string> res = new List<string>();
                res.Add(e.Message);
                return Request.CreateResponse(System.Net.HttpStatusCode.InternalServerError, res);
            }
        }

        // POST api/Account/SetPassword
        [Route("SetPassword")]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> SetPassword(RestorePasswordEntry model)
        {

            bool res = await SetNullPassword(model.UserId);
            if (res)
            {

                IdentityResult result = await UserManager.AddPasswordAsync(model.UserId, model.NewPassword);

                if (!result.Succeeded)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "Internal server error occured.Please try again");
                }

                return Request.CreateResponse(HttpStatusCode.OK, "OK");
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Internal server error occured.Please try again");
            }
        }

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        //GET api/Account/UserRoles
        [Route("UserRoles")]
        [HttpGet]
        public async Task<IHttpActionResult> UserRoles()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = "Select B.Name from dbo.AspNetUserRoles A inner join dbo.AspNetRoles B On A.RoleId = B.Id Where A.UserId=@uid";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@uid", User.Identity.GetUserId());
                        await con.OpenAsync();

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            List<string> roles = new List<string>();

                            while (rdr.Read())
                            {
                                roles.Add(rdr["Name"].ToString());
                            }

                            return Ok(roles);
                        }
                    }
                }
            }
            catch
            {
                return InternalServerError();
            }
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                   OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<HttpResponseMessage> Register(RegisterBindingModel model)
        {
            try
            {
                var user = new ApplicationUser()
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    Surname = model.Surname
                };

                IdentityResult result = await UserManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest, new List<string>(result.Errors));
                }

            }
            catch (Exception e)
            {
                List<string> toReturn = new List<string>();
                toReturn.Add(e.Message);

                return Request.CreateResponse(System.Net.HttpStatusCode.InternalServerError, toReturn);
            }

            return Request.CreateResponse(System.Net.HttpStatusCode.OK, "OK");
        }

        // POST api/Account/RegisterExternal
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return InternalServerError();
            }


            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result != null && !result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }

            return BadRequest(ModelState);
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        private async Task<bool> SetNullPassword(string userId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    string cmdString = "Update AspNetUsers Set PasswordHash=NULL Where Id=@id";

                    using (SqlCommand cmd = new SqlCommand(cmdString, con))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();

                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
