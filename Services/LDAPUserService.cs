using Elastic.Clients.Elasticsearch;
using SitefinityWebApp.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Configuration;

namespace SitefinityWebApp.Services
{
    public class LDAPUserService
    {
        private string _ldapServer;
        private string _ldapUser;
        private string _ldapPassword;
        private string _usersDN;
        private string _authenticationType;

        private Dictionary<string, string> _propertyMapping;

        public LDAPUserService()
        {
            LoadSitefinityLdapConfig();
            LoadPropertyMapping();
        }

        private void LoadPropertyMapping()
        {
            _propertyMapping = new Dictionary<string, string>();

            // Cargar el mapeo desde web.config
            _propertyMapping["LdapId"] = ConfigurationManager.AppSettings["LDAP.Properties.LdapId"] ?? "objectGUID";
            _propertyMapping["Username"] = ConfigurationManager.AppSettings["LDAP.Properties.Username"];
            _propertyMapping["FullName"] = ConfigurationManager.AppSettings["LDAP.Properties.FullName"];
            _propertyMapping["Email"] = ConfigurationManager.AppSettings["LDAP.Properties.Email"];
            _propertyMapping["Identificacion"] = ConfigurationManager.AppSettings["LDAP.Properties.Identificacion"] ;
            _propertyMapping["Zone"] = ConfigurationManager.AppSettings["LDAP.Properties.Zone"];
            _propertyMapping["Cargo"] = ConfigurationManager.AppSettings["LDAP.Properties.Cargo"];
            _propertyMapping["Extension"] = ConfigurationManager.AppSettings["LDAP.Properties.Extension"];
            _propertyMapping["Office"] = ConfigurationManager.AppSettings["LDAP.Properties.Office"];
            _propertyMapping["CodeOffice"] = ConfigurationManager.AppSettings["LDAP.Properties.CodeOffice"];
            _propertyMapping["Department"] = ConfigurationManager.AppSettings["LDAP.Properties.Department"];
            _propertyMapping["Manager"] = ConfigurationManager.AppSettings["LDAP.Properties.Manager"];
            _propertyMapping["Grado"] = ConfigurationManager.AppSettings["LDAP.Properties.Grado"];
            _propertyMapping["BirthDay"] = ConfigurationManager.AppSettings["LDAP.Properties.BirthDay"];
            _propertyMapping["HireDay"] = ConfigurationManager.AppSettings["LDAP.Properties.HireDay"];
            _propertyMapping["Role"] = ConfigurationManager.AppSettings["LDAP.Properties.Role"];
            _propertyMapping["Profile"] = ConfigurationManager.AppSettings["LDAP.Properties.Profile"];
            _propertyMapping["Photo"] = ConfigurationManager.AppSettings["LDAP.Properties.Photo"];
        }

        // Método helper para obtener el nombre de la propiedad LDAP mapeada
        private string GetMappedProperty(string propertyName)
        {
            return _propertyMapping.ContainsKey(propertyName) ? _propertyMapping[propertyName] : propertyName;
        }

        public List<LDAPUser> GetAllUsers()
        {
            var users = new List<LDAPUser>();

            try
            {
                var ldapPath = $"LDAP://{_ldapServer}:389/{_usersDN}";

                using (var entry = new DirectoryEntry(ldapPath, _ldapUser, _ldapPassword, AuthenticationTypes.None))
                using (var searcher = new DirectorySearcher(entry))
                {
                    // Búsqueda más específica y simple
                    searcher.Filter = "(objectClass=user)";

                    // Solo propiedades básicas primero
                    searcher.PropertiesToLoad.Clear();

                    // Limitar resultados para testing
                    searcher.SizeLimit = 10;
                    searcher.PageSize = 10;

                    using (var results = searcher.FindAll())
                    {
                        foreach (SearchResult result in results)
                        {
                            DebugPrintAllProperties(result);
                            var ldapId = GetMappedProperty(result, "objectGUID");
                            var username = GetMappedProperty(result, "sAMAccountName");
                            var fullName = GetMappedProperty(result, "displayName");
                            var email = GetMappedProperty(result, "mail");
                            var identificacion = GetMappedProperty(result, "postalCode");
                            var zona = GetMappedProperty(result, "streetAddress"); //
                            var cargo = GetMappedProperty(result, "title"); //est 
                            var ext = GetMappedProperty(result, "telephoneNumber");
                            var office = GetMappedProperty(result, "company");
                            var codeOffice = GetMappedProperty(result, "physicalDeliveryOfficeName");
                            var department = GetMappedProperty(result, "department");
                            var manager = GetMappedProperty(result, "manager");
                            var degree = GetMappedProperty(result, "msDS-cloudExtensionAttribute1");
                            var birthDay = GetMappedProperty(result, "homePhone");
                            var hireDay = GetMappedProperty(result, "pager");

                            // Solo agregar si tiene username y no es cuenta de computadora
                            if (!string.IsNullOrEmpty(username) && !username.EndsWith("$"))
                            {
                                var user = new LDAPUser
                                {
                                    LdapId = ldapId,
                                    Username = username,
                                    FullName = fullName,
                                    Email = email,
                                    Identificacion = identificacion,
                                    Zone = zona,
                                    Extension = ext,
                                    Cargo = cargo,
                                    Office = office,
                                    CodeOffice = codeOffice,
                                    Department = department,
                                    Manager = manager,
                                    Grado = degree,
                                    BirthDay = birthDay,
                                    HireDay = hireDay,
                                    Enabled = true
                                };

                                users.Add(user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error connecting to LDAP: {ex.Message}", ex);
            }

            return users;
        }

        public LDAPUser GetUserByUsername(string username)
        {
            try
            {
                var ldapPath = $"LDAP://{_ldapServer}/{_usersDN}";

                using (var entry = new DirectoryEntry(ldapPath, _ldapUser, _ldapPassword, AuthenticationTypes.Secure))
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(sAMAccountName={username}))";
                    searcher.PropertiesToLoad.AddRange(new[] {
                        "sAMAccountName", "givenName", "mail"
                    });

                    var result = searcher.FindOne();
                    if (result != null)
                    {
                        return new LDAPUser
                        {
                            Username = GetMappedProperty(result, "sAMAccountName") ?? "",
                            FirstName = GetMappedProperty(result, "givenName") ?? "",
                            Email = GetMappedProperty(result, "mail") ?? "",
                            Enabled = !IsAccountDisabled(result)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user from LDAP: {ex.Message}", ex);
            }

            return null;
        }


        public LDAPUser GetProfileAndRol(string email)
        {
            try
            {
                var ldapPath = $"LDAP://{_ldapServer}/{_usersDN}";

                using (var entry = new DirectoryEntry(ldapPath, _ldapUser, _ldapPassword, AuthenticationTypes.Secure))
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(mail={email}))";
                    searcher.PropertiesToLoad.AddRange(new[] {
                        "msDS-cloudExtensionAttribute2", "msDS-cloudExtensionAttribute3"
                    });

                    var result = searcher.FindOne();
                    if (result != null)
                    {
                        return new LDAPUser
                        {
                            Role = GetMappedProperty(result, "msDS-cloudExtensionAttribute2") ?? "",  
                            Profile = GetMappedProperty(result, "msDS-cloudExtensionAttribute3") ?? ""
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user from LDAP: {ex.Message}", ex);
            }

            return null;
        }

        public byte[] GetPhotoByUsername(string email)
        {
            try
            {
                var ldapPath = $"LDAP://{_ldapServer}/{_usersDN}";

                using (var entry = new DirectoryEntry(ldapPath, _ldapUser, _ldapPassword, AuthenticationTypes.Secure))
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(mail={email}))";
                    searcher.PropertiesToLoad.Add("thumbnailPhoto");

                    var result = searcher.FindOne();
                    if (result != null && result.Properties["thumbnailPhoto"] != null && result.Properties["thumbnailPhoto"].Count > 0)
                    {
                        return (byte[])result.Properties["thumbnailPhoto"][0];
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener la foto: {ex.Message}", ex);
            }

            return null;
        }

        private void LoadSitefinityLdapConfig()
        {
            try
            {
                var configPath = HostingEnvironment.MapPath("~/App_Data/Sitefinity/Configuration/SecurityConfig.config");
                var doc = new XmlDocument();
                doc.Load(configPath);

                var connectionNode = doc.SelectSingleNode("//LdapConnection[@name='DefaultLdapConnection']");

                if (connectionNode != null)
                {
                    _ldapServer = connectionNode.Attributes["serverName"]?.Value;
                    _ldapUser = connectionNode.Attributes["connectionUsername"]?.Value;
                    _ldapPassword = connectionNode.Attributes["connectionPassword"]?.Value;
                    _usersDN = connectionNode.Attributes["usersDN"]?.Value;
                    _authenticationType = connectionNode.Attributes["authenticationType"]?.Value;
                }
                else
                {
                    throw new Exception("No se encontró la configuración LDAP 'DefaultLdapConnection'");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading Sitefinity LDAP config: {ex.Message}");
            }
        }

        private void DebugPrintAllProperties(SearchResult result)
        {
            System.Diagnostics.Debug.WriteLine("=== LDAP USER PROPERTIES ===");

            var allProperties = new Dictionary<string, object>();

            foreach (string propertyName in result.Properties.PropertyNames)
            {
                var values = result.Properties[propertyName];
                if (values.Count > 0)
                {
                    allProperties[propertyName] = values[0];
                    System.Diagnostics.Debug.WriteLine($"{propertyName}: {values[0]}");
                }
            }

            System.Diagnostics.Debug.WriteLine("=============================");
        }

        private string GetMappedProperty(SearchResult result, string propertyName)
        {
            if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            {
                return result.Properties[propertyName][0].ToString();
            }
            return string.Empty;
        }

        private bool IsAccountDisabled(SearchResult result)
        {
            if (result.Properties.Contains("userAccountControl") && result.Properties["userAccountControl"].Count > 0)
            {
                var uac = (int)result.Properties["userAccountControl"][0];
                return (uac & 0x2) != 0; // UF_ACCOUNTDISABLE flag
            }
            return false;
        }

        private string ExtractGroupName(string groupDN)
        {
            // Extraer CN del Distinguished Name
            // Ejemplo: "CN=Domain Admins,CN=Users,DC=testdomain,DC=local" -> "Domain Admins"
            if (groupDN.StartsWith("CN="))
            {
                var start = 3; // Después de "CN="
                var end = groupDN.IndexOf(',');
                if (end > start)
                {
                    return groupDN.Substring(start, end - start);
                }
            }
            return groupDN;
        }

        private string GetPrimaryGroupName(string primaryGroupId)
        {
            try
            {
                // Buscar el grupo por RID (Relative ID)
                var ldapPath = $"LDAP://{_ldapServer}:389/{_usersDN}";
                using (var entry = new DirectoryEntry(ldapPath, _ldapUser, _ldapPassword, AuthenticationTypes.None))
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=group)(primaryGroupToken={primaryGroupId}))";
                    searcher.PropertiesToLoad.Add("cn");

                    var result = searcher.FindOne();
                    if (result != null && result.Properties.Contains("cn"))
                    {
                        return result.Properties["cn"][0].ToString();
                    }
                }
            }
            catch
            {
                // Si falla, devolver nombres conocidos
                switch (primaryGroupId)
                {
                    case "513": return "Domain Users";
                    case "512": return "Domain Admins";
                    case "514": return "Domain Guests";
                    default: return $"Group-{primaryGroupId}";
                }
            }

            return null;
        }
    }
}