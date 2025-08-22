using Elastic.Clients.Elasticsearch;
using SitefinityWebApp.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Xml;

namespace SitefinityWebApp.Services
{
    public class LDAPUserService
    {
        private string _ldapServer;
        private string _ldapUser;
        private string _ldapPassword;
        private string _usersDN;
        private string _authenticationType;

        public LDAPUserService()
        {
            LoadSitefinityLdapConfig();
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
                            var ldapId = GetProperty(result, "objectGUID");
                            var username = GetProperty(result, "sAMAccountName");
                            var fullName = GetProperty(result, "displayName");
                            var email = GetProperty(result, "mail");
                            var identificacion = GetProperty(result, "postalCode");
                            var zona = GetProperty(result, "streetAddress"); //
                            var cargo = GetProperty(result, "title"); //est 
                            var ext = GetProperty(result, "telephoneNumber");
                            var office = GetProperty(result, "company");
                            var codeOffice = GetProperty(result, "physicalDeliveryOfficeName");
                            var department = GetProperty(result, "department");
                            var manager = GetProperty(result, "manager");
                            var degree = GetProperty(result, "msDS-cloudExtensionAttribute1");
                            var birthDay = GetProperty(result, "homePhone");
                            var hireDay = GetProperty(result, "pager");

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
                            Username = GetProperty(result, "sAMAccountName") ?? "",
                            FirstName = GetProperty(result, "givenName") ?? "",
                            Email = GetProperty(result, "mail") ?? "",
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
                            Role = GetProperty(result, "msDS-cloudExtensionAttribute2") ?? "",  
                            Profile = GetProperty(result, "msDS-cloudExtensionAttribute3") ?? ""
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

        private string GetProperty(SearchResult result, string propertyName)
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