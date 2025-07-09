//using Microsoft.Identity.Client;
//using System.Net.Http.Headers;

//var clientId = Environment.GetEnvironmentVariable("ClientId");
//var tenantId = Environment.GetEnvironmentVariable("TenantId");
//var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");


//var app = ConfidentialClientApplicationBuilder.Create(clientId)
//    .WithClientSecret(clientSecret)
//    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
//    .Build();

//var result = await app.AcquireTokenForClient(new[] { "api://<api-client-id>/.default" })
//    .ExecuteAsync();

//var httpClient = new HttpClient();
//httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

