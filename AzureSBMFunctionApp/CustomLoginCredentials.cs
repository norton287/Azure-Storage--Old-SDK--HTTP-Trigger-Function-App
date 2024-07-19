using Microsoft.Identity.Client;
using Microsoft.Rest;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSBMFunctionApp;

public class CustomLoginCredentials : ServiceClientCredentials
{
    private string tenantId { get; }
    private string clientId { get; }
    private string password { get; }

    public CustomLoginCredentials(string tenant, string client, string pass)
    {
        tenantId = tenant;
        clientId = client;
        password = pass;
    }

    private string AuthenticationToken { get; set; }

    public override void InitializeServiceClient<T>(ServiceClient<T> client)
    {
        var cc = ConfidentialClientApplicationBuilder.Create(clientId).WithClientSecret(password)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}").Build();

        var atc = cc.AcquireTokenForClient(new List<string> { "https://management.core.windows.net/.default" });
        var accessToken = atc.ExecuteAsync().Result.AccessToken;

        AuthenticationToken = accessToken;
    }

    public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (AuthenticationToken == null) throw new InvalidOperationException("Token Provider Cannot Be Null");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthenticationToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //request.Version = new Version(apiVersion);
        await base.ProcessHttpRequestAsync(request, cancellationToken);
    }
}