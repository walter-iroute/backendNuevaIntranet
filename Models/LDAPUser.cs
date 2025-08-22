using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SitefinityWebApp.Models
{
    public class LDAPUser
    {
        public string LdapId { get; set; }
        public string FirstName { get; set; }
        public string FullName {  get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Identificacion { get; set; }
        public string Zone { get; set; }
        public string PhoneNumber { get; set; }
        public string Extension { get; set; }
        public string Cargo { get; set; }
        public string Office { get; set; }
        public string CodeOffice { get; set; }
        public string Department { get; set; }
        public string Manager { get; set; }
        public string Grado { get; set; }
        public string BirthDay { get; set; }
        public string HireDay { get; set; }
        public bool Enabled { get; set; }
        public string Profile { get; set; }
        public string Role { get; set; }
    }
}