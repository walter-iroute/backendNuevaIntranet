using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Activation;
using System.ServiceModel;
using System.Web;
using Newtonsoft.Json;
using SitefinityWebApp.Services;
using System.ServiceModel.Web;
using System.IO;
using Telerik.Sitefinity.Security.Claims;
using Azure;
using DocumentFormat.OpenXml.Spreadsheet;
using Elastic.Clients.Elasticsearch;
using SitefinityWebApp.Models;
using Telerik.Sitefinity.Abstractions;

namespace SitefinityWebApp.Controllers.LDAPUserRest
{
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class LDAPUserRestService
    {
        private readonly LDAPUserService _ldapUserService;
        private readonly ColaboradorService _colaboradorService;

        public LDAPUserRestService() 
        { 
            _ldapUserService = new LDAPUserService();
            _colaboradorService = new ColaboradorService();
        }

        [OperationContract]
        [WebGet(UriTemplate = "users")]
        public Stream GetUsers()
        {
            try
            {
                List<LDAPUser> users = _ldapUserService.GetAllUsers();

                var response = new
                {
                    success = true,
                    data = users.Select(user => new
                    {
                        ldapId = user.LdapId,
                        fullName = user.FullName ?? "",
                        username = user.Username ?? "",
                        email = user.Email ?? "",
                        identificacion = user.Identificacion ?? "",
                        zona = user.Zone ?? "",
                        phoneNumber = user.PhoneNumber ?? "",
                        extesion = user.Extension ?? "",
                        cargo = user.Cargo ?? "",
                        codeOffice = user.CodeOffice ?? "",
                        office = user.Office ?? "",
                        department = user.Department ?? "",
                        manager = user.Manager ?? "",
                        grado = user.Grado ?? "",
                        birthDay = user.BirthDay ?? "",
                        hireDay = user.HireDay ?? "",
                        enabled = user.Enabled
                    }),
                    count = users.Count,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonConvert.SerializeObject(response);

                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.ContentLength = bytes.Length;

                return new MemoryStream(bytes);
            }
            catch (Exception ex)
            {
                var errorResponse = new { success = false, message = ex.Message, timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };

                var json = JsonConvert.SerializeObject(errorResponse);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.ContentLength = bytes.Length;

                return new MemoryStream(bytes);
            }
        }

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "get-photo/{email}")]
        public Stream GetPhotoByUsername(string email)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

            try
            {
                var photoBytes = _ldapUserService.GetPhotoByUsername(email);

                var response = new
                {
                    mail = email,
                    PhotoBase64 = photoBytes != null ? Convert.ToBase64String(photoBytes) : null,
                    HasPhoto = photoBytes != null
                };

                var json = JsonConvert.SerializeObject(response);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.ContentLength = bytes.Length;

                return new MemoryStream(bytes);
            }
            catch (Exception ex)
            {
                var errorResponse = new { success = false, message = ex.Message, timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };

                var json = JsonConvert.SerializeObject(errorResponse);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.ContentLength = bytes.Length;

                return new MemoryStream(bytes);
            }
        }

        //[OperationContract]
        //[WebGet(ResponseFormat = WebMessageFormat.Xml, UriTemplate = "user-groups/{username}")]
        //public Stream GetUserGroups(string username)
        //{
        //    WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        //    try
        //    {
        //        var groups = _ldapUserService.GetUserGroups(username);

        //        var response = new
        //        {
        //            success = true,
        //            username = username,
        //            groups = groups,
        //            groupCount = groups.Count,
        //            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        //        };

        //        var json = JsonConvert.SerializeObject(response);
        //            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        //            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
        //            WebOperationContext.Current.OutgoingResponse.ContentLength = bytes.Length;

        //            return new MemoryStream(bytes);
        //    }
        //    catch (Exception ex)
        //    {
        //        var errorResponse = new { success = false, message = ex.Message, timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
        //        var json = JsonConvert.SerializeObject(errorResponse);
        //        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        //        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
        //        WebOperationContext.Current.OutgoingResponse.ContentLength = bytes.Length;

        //        return new MemoryStream(bytes);
        //    }
        //}



        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "profile-rol/{email}")]
        public Stream GetProfileAndRol(string email)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

            try
            {

                var userAuthenticated = _ldapUserService.GetProfileAndRol(email);

                if (userAuthenticated != null)
                {
                    var response = new
                    {
                        success = true,
                        profile = userAuthenticated.Profile,
                        role = userAuthenticated.Role
                    };

                    var json = JsonConvert.SerializeObject(response);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                    WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                    WebOperationContext.Current.OutgoingResponse.ContentLength = bytes.Length;

                    return new MemoryStream(bytes);
                }

                // Usuario no autenticado
                string jsonNotAuthenticated = JsonConvert.SerializeObject(new
                {
                    success = false,
                    message = "User not authenticated",
                    isAuthenticated = false,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                var bytesNotAuthenticated = System.Text.Encoding.UTF8.GetBytes(jsonNotAuthenticated);

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.ContentLength = bytesNotAuthenticated.Length;

                return new MemoryStream(bytesNotAuthenticated);
            }
            catch (Exception ex)
            {
                string errorResponse = JsonConvert.SerializeObject(new
                {
                    success = false,
                    message = ex.Message,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                var byteserrorResponse = System.Text.Encoding.UTF8.GetBytes(errorResponse);

                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.ContentLength = byteserrorResponse.Length;

                return new MemoryStream(byteserrorResponse);
            }
        }
    }
}