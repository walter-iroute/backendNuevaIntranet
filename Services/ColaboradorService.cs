using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.DynamicModules.Model;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.Localization;
using Telerik.Sitefinity.Utilities.TypeConverters;
using Telerik.Sitefinity.Versioning;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity;
using SitefinityWebApp.Models;
using Telerik.Sitefinity.Workflow;
using System.Activities;

namespace SitefinityWebApp.Services
{
    public class ColaboradorService
    {
        public void CreateColaborador(LDAPUser user)
        {
            // Set the provider name for the DynamicModuleManager here. All available providers are listed in
            // Administration -> Settings -> Advanced -> DynamicModules -> Providers
            var providerName = String.Empty;

            // Set a transaction name and get the version manager
            var transactionName = "createColaboradores";
            var versionManager = VersionManager.GetManager(null, transactionName);

            DynamicModuleManager dynamicModuleManager = DynamicModuleManager.GetManager(providerName, transactionName);
            Type colaboradorType = TypeResolutionService.ResolveType("Telerik.Sitefinity.DynamicTypes.Model.Colaboradores.Colaborador");
            DynamicContent colaboradorItem = dynamicModuleManager.CreateDataItem(colaboradorType);

            // Set the culture name for the item fields
            var cultureName = "en";
            var culture = new CultureInfo(cultureName);

            // Wrap the following methods in a using statement using the culture you want to assign to the item
            using (new CultureRegion(culture))
            {
                // This is how values for the properties are set
                colaboradorItem.SetString("Title", user.FullName);
                colaboradorItem.SetValue("LdapId", user.LdapId);
                colaboradorItem.SetString("Username", user.Username);
                colaboradorItem.SetString("Identificacion", user.Identificacion);
                colaboradorItem.SetString("Email", user.Email);
                colaboradorItem.SetString("Zona", user.Zone);
                colaboradorItem.SetString("NumeroTelefono", user.PhoneNumber);
                colaboradorItem.SetString("Extension", user.Extension);
                colaboradorItem.SetString("Cargo", user.Cargo);
                colaboradorItem.SetString("Oficina", user.Office);
                colaboradorItem.SetString("Area", user.Department);
                colaboradorItem.SetString("JefeDirecto", user.Manager);
                colaboradorItem.SetString("Grado", user.Grado);
                colaboradorItem.SetString("FechaNacimiento", user.BirthDay);
                colaboradorItem.SetString("FechaIngreso", user.HireDay);
                colaboradorItem.SetString("UrlName", "");
                colaboradorItem.SetValue("Owner", SecurityManager.GetCurrentUserId());
                colaboradorItem.SetValue("PublicationDate", DateTime.UtcNow);
                colaboradorItem.SetWorkflowStatus(dynamicModuleManager.Provider.ApplicationName, "Published");

                // Create a version and commit the transaction in order changes to be persisted to data store
                versionManager.CreateVersion(colaboradorItem, false);
                TransactionManager.CommitTransaction(transactionName);

                // Use lifecycle so that LanguageData and other Multilingual related values are correctly created
                DynamicContent checkOutColaboradorItem = dynamicModuleManager.Lifecycle.CheckOut(colaboradorItem) as DynamicContent;
                DynamicContent checkInColaboradorItem = dynamicModuleManager.Lifecycle.CheckIn(checkOutColaboradorItem) as DynamicContent;
                versionManager.CreateVersion(checkInColaboradorItem, false);
                TransactionManager.CommitTransaction(transactionName);
            }

        }
    }
}