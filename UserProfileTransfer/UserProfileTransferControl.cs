using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Tooling.Connector;
using System.IO;
using UserProfileTransfer.Helpers;
using Newtonsoft.Json;
using UserProfileTransfer.Models;
using System.Reflection;

namespace UserProfileTransfer
{
    public partial class UserProfileTransferControl : PluginControlBase
    {
        private Settings mySettings;

        public UserProfileTransferControl()
        {
            InitializeComponent();
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));

            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tsbSample_Click(object sender, EventArgs e)
        {
            // The ExecuteMethod method handles connecting to an
            // organization if XrmToolBox is not yet connected
            ExecuteMethod(GetAccounts);
        }

        private void GetAccounts()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting accounts",
                Work = (worker, args) =>
                {
                    args.Result = Service.RetrieveMultiple(new QueryExpression("account")
                    {
                        TopCount = 50
                    });
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as EntityCollection;
                    if (result != null)
                    {
                        MessageBox.Show($"Found {result.Entities.Count} accounts");
                    }
                }
            });
        }

        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }

        private void btnImportUserProfiles_Click(object sender, EventArgs e)
        {
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            try
            {
                CrmServiceClient crmServiceClient = (CrmServiceClient)Service;

                var env = crmServiceClient.CrmConnectOrgUriActual.Host.Substring(0, crmServiceClient.CrmConnectOrgUriActual.Host.IndexOf('.'));
                var fileName = Directory.GetCurrentDirectory() + @"\Export\" + env + "_AssignRolesToUsers.json";
                
                if (crmServiceClient.CrmConnectOrgUriActual.ToString().Contains(env))
                {
                    var repo = new SecurityManager(crmServiceClient);

                    //change to go up 3 folders in deploymentpackage
                    using (StreamReader file = File.OpenText(fileName))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        UserSecurityProfileList jsonConfig = (UserSecurityProfileList)serializer.Deserialize(file, typeof(UserSecurityProfileList));

                        foreach (UserSecurityProfile payload in jsonConfig)
                        {
                            repo.ConfigureUser(payload);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failure to process Team security roles: {ex.Message}");
            }
        }

        private void btnExportUserProfiles_Click(object sender, EventArgs e)
        {
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            try
            {
                CrmServiceClient crmServiceClient = (CrmServiceClient)Service;
                
                var env = crmServiceClient.CrmConnectOrgUriActual.Host.Substring(0, crmServiceClient.CrmConnectOrgUriActual.Host.IndexOf('.'));
                var fileName = Directory.GetCurrentDirectory() + @"\Export\" + env + "_AssignRolesToUsers.json";

                if (crmServiceClient.CrmConnectOrgUriActual.ToString().Contains(env))
                {
                    var repo = new SecurityManager(crmServiceClient);
                    var jsonPayload = repo.ExportUserSecurity();
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                    using (var tw = new StreamWriter(fileName, true))
                    {
                        tw.WriteLine(jsonPayload);
                        tw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failure to process Team security roles: {ex.Message}");
            }
        }
    }
}