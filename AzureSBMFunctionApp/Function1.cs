using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;

using Newtonsoft.Json;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AzureSBMFunctionApp
{
    public static class Function1
    {
        public static StorageManagementClient StorageManagementClient { get; set; }
        public static string Key1 { get; set; }
        public static string Key2 { get; set; }
        public static string AccountKey1 { get; set; }
        public static string AccountKey2 { get; set; }
        public static string ReturnResult { get; set; }
        
        [FunctionName("Function1")]
        public static async  Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string switchtype = req.Query?["switchtype"];
            string subId = req.Query?["subId"];
            string tenantId = req.Query?["tenantId"];
            string clientId = req.Query?["clientId"];
            string clientPass = req.Query?["clientPass"];
            string resourceGroup = req.Query?["resourceGroup"];
            string storageName = req.Query?["storageName"];
            string containerName = req.Query?["containerName"];

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            switchtype ??= data?.switchtype;
            subId ??= data?.subId;
            tenantId ??= data?.tenantId;
            clientId ??= data?.clientId;
            clientPass ??= data?.clientPass;
            resourceGroup ??= data?.resourceGroup;
            storageName ??= data?.storageName;
            containerName ??= data?.containerName;

            new StorageHelpers();

            if (!string.IsNullOrEmpty(switchtype) && !string.IsNullOrEmpty(subId) && !string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId)
                && !string.IsNullOrEmpty(clientPass) && !string.IsNullOrEmpty(resourceGroup) && !string.IsNullOrEmpty(storageName))
            {
                CustomLoginCredentials cred = null;
                
                try
                {
                    try
                    {
                        cred = new CustomLoginCredentials(tenantId, clientId, clientPass);
                        
                        log.LogInformation("Cred was " + cred);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Cred Error " + e.Message + " " + e.Data);
                        log.LogInformation("Cred was " + e.Message + " " + e.Data);
                    }

                    StorageManagementClient = new StorageManagementClient(cred) { SubscriptionId = subId };
                }
                catch (CloudException e)
                {
                    Debug.WriteLine("Client error " + e.Request + " " + e.Body);
                    log.LogInformation("Error building storage client " + e.Message + " " + e.Data + " " + e.Response);
                }
                catch (Exception ex)
                {
                    log.LogInformation("Exception occurred " + ex.Message + " " + ex.Data);
                }
                
                if (StorageManagementClient != null)
                {   
                    log.LogInformation("In If/Then Logic Statement");

                    if (switchtype == "keys")
                    {
                        try
                        {
                            ReturnResult = StorageHelpers.ConnectionStrings(resourceGroup,
                                storageAccount: storageName, log);
                        }
                        catch (CloudException ex)
                        {
                            log.LogInformation("Cloud Exception " + ex.Message + " " + ex.Data);
                        }
                        catch (Exception e)
                        {
                            log.LogInformation("Something happened in Building the keys " + e.Message + " " + e.Data);
                        }
                    }
                    else if (switchtype == "plainKeys")
                    {
                        try
                        {
                            ReturnResult = StorageHelpers.StorageKeys(resourceGroup,
                                storageAccount: storageName, log);
                        }
                        catch (CloudException ex)
                        {
                            log.LogInformation("Cloud Exception " + ex.Message + " " + ex.Data);
                        }
                        catch (Exception e)
                        {
                            log.LogInformation("Something happened in building the regular keys " + e.Message + " " + e.Data);
                        }
                    }
                    else if (switchtype == "regenKeys")
                    {
                        try
                        {
                            ReturnResult = await StorageHelpers.RegenKeys(resourceGroup, storageName, log);
                        }
                        catch (CloudException e)
                        {
                            log.LogInformation("Cloud Exception " + e.Message + " " + e.Data);
                        }
                        catch (Exception ex)
                        {
                            log.LogInformation("Something Occurred Regenerating Keys " + ex.Message + " " + ex.Data);
                        }
                    }
                    else if (switchtype == "setLegalHold")
                    {
                        try
                        {
                            ReturnResult = await StorageHelpers.SetLegalHold(resourceGroup,
                                storageAccount: storageName, containerName, log);
                        }
                        catch (CloudException ex)
                        {
                            log.LogInformation("Cloud Exception " + ex.Message + " " + ex.Data);
                        }
                        catch (Exception e)
                        {
                            log.LogInformation("Something happened in processing the LegalHold " + e.Message + " " + e.Data);
                        }
                    }
                    else if (switchtype == "clearLegalHold")
                    {
                        try
                        {
                            ReturnResult = StorageHelpers.ClearLegalHold(resourceGroup,
                                storageAccount: storageName, containerName, log);
                        }
                        catch (CloudException ex)
                        {
                            log.LogInformation("Cloud Exception " + ex.Message + " " + ex.Data);
                        }
                        catch (Exception e)
                        {
                            log.LogInformation("Something happened in clearing the LegalHold " + e.Message + " " + e.Data);
                        }
                    }
                }
            }
            else
            {
                ReturnResult = string.Empty;
                log.LogInformation("Something Happened As The Return Result Was Empty!");
            }

            var responseMessage = string.IsNullOrEmpty(ReturnResult)
                ? "Something occurred but the Function returned no response."
                : ReturnResult;

            return new OkObjectResult(responseMessage);
        }
    }
}
