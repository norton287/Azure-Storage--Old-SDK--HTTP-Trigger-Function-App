using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSBMFunctionApp;

public class StorageHelpers
{
    public static string TempKey1 { get; set; }
    public static string TempKey2 { get; set; }

    public static async Task<string> SetLegalHold(string resourceGroup, string storageAccount, string containerName, ILogger log)
    {
        if (Function1.StorageManagementClient == null) return null;

        var result = await Function1.StorageManagementClient.BlobContainers.SetLegalHoldAsync(
            resourceGroup, storageAccount, containerName, new List<string>() { "tag1" },
            cancellationToken: CancellationToken.None);

        log.LogInformation("Made it Past Set Legal Hold " + result.HasLegalHold.HasValue);

        return result.HasLegalHold.HasValue.ToString();
    }

    public static string ClearLegalHold(string resourceGroup, string storageAccount, string containerName, ILogger log)
    {
        if (Function1.StorageManagementClient == null) return "Storage Client Was Empty";

        var result = Function1.StorageManagementClient.BlobContainers.ClearLegalHoldAsync(
            resourceGroup, storageAccount, containerName, new List<string>() { "tag1" },
            cancellationToken: CancellationToken.None);

        log.LogInformation("Made it past Clear Legal Hold " + result.Result.HasLegalHold);


        return result.Result.HasLegalHold.ToString();
    }

    public static string ConnectionStrings(string resourceGroup, string storageAccount, ILogger log)
    {
        if (Function1.StorageManagementClient == null) return "Storage Client Was Empty";

        var keyResponse = Function1.StorageManagementClient.StorageAccounts.ListKeys(resourceGroup, storageAccount)
            .Keys;

        log.LogInformation("In ConnectionStrings and Made It Past KeyResponse");

        foreach (var key in keyResponse)
        {
            if (key.KeyName == "key1")
            {
                Function1.AccountKey1 =
                    $"DefaultEndpointsProtocol=https;AccountName={storageAccount};AccountKey={key.Value}";
            }
            else
            {
                Function1.AccountKey2 = $"DefaultEndpointsProtocol=https;AccountName={storageAccount};AccountKey={key.Value}";
            }
        }

        var combine = Function1.AccountKey1 + ',' + Function1.AccountKey2;

        return combine;
    }

    public static string StorageKeys(string resourceGroup, string storageAccount, ILogger log)
    {
        Function1.Key1 = string.Empty;
        Function1.Key2 = string.Empty;

        if (Function1.StorageManagementClient == null) return "Storage Client Was Empty";

        var keyResponse = Function1.StorageManagementClient.StorageAccounts.ListKeys(resourceGroup, storageAccount)
            .Keys;

        log.LogInformation("In StorageKeys and Made It Past KeyResponse");

        foreach (var key in keyResponse)
        {
            if (key.KeyName == "key1")
            {
                Function1.Key1 = key.Value;
            }
            else
            {
                Function1.Key2 = key.Value;
            }
        }

        TempKey1 = Function1.Key1;
        TempKey2 = Function1.Key2;

        var combine = Function1.Key1 + ',' + Function1.Key2;

        return combine;
    }

    public static void JustBuildKeys(string resourceGroup, string storageAccount)
    {
        TempKey1 = string.Empty;
        TempKey2 = string.Empty;

        var keyResponse = Function1.StorageManagementClient.StorageAccounts.ListKeys(resourceGroup, storageAccount)
            .Keys;

        foreach (var key in keyResponse)
        {
            if (key.KeyName == "key1")
            {
                TempKey1 = key.Value;
            }
            else
            {
                TempKey2 = key.Value;
            }
        }
    }

    public static async Task<string> RegenKeys(string resourceGroup, string storageName, ILogger log)
    {
        IList<StorageAccountKey> keys = new List<StorageAccountKey>();
        IList<StorageAccountKey> keys2 = new List<StorageAccountKey>();

        try
        {
            keys = (await Function1.StorageManagementClient.StorageAccounts
                .RegenerateKeyAsync(resourceGroup, storageName, "key1")).Keys;
            keys2 = (await Function1.StorageManagementClient.StorageAccounts.RegenerateKeyAsync(resourceGroup,
                    storageName, "key2"))
                .Keys;
        }
        catch (CloudException e)
        {
            log.LogInformation("CloudException in RegenKeys Method " + e.Body + " " + e.Response);
        }

        JustBuildKeys(resourceGroup, storageName);

        do
        {
            await Task.Delay(100).ConfigureAwait(false);
        } while (string.IsNullOrEmpty(TempKey2));

        var combine = string.Empty;

        if (!string.IsNullOrEmpty(TempKey1) && !string.IsNullOrEmpty(TempKey2))
        {
            combine = TempKey1 + ":" + TempKey2;
        }

        return combine;
    }
}