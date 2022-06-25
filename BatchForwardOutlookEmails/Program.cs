using System.Net.Http.Headers;
using System.Text;

//common
const string messagesApiUrl = "https://graph.microsoft.com/v1.0/me/messages";
const string accessToken = "";
const string forwardToEmail = "";

//conf httpClient
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
httpClient.DefaultRequestHeaders.Add("User-Agent", "helper");
httpClient.BaseAddress = new Uri("https://api.github.com");

if (ForwardEmails(messagesApiUrl).Result)
{
    Console.WriteLine("All ok");
}

async Task<bool> ForwardEmails(string url)
{
    var listOfEmails = GetRawData(url);
    var nextBatchOfEmails = NextBatchUrl(await listOfEmails);
    await ForwardEmailsByIdList(GiveIdList(await listOfEmails));
    if (nextBatchOfEmails != "")
    {
        Console.WriteLine(nextBatchOfEmails);
        // await ForwardEmails(nextBatchOfEmails);
    }
    return true;
}

string NextBatchUrl(string content)
{
    var rawHeaderData = content.Split("\\\"\",\"id\":\"");
    if (rawHeaderData[0].Contains("@odata.nextLink\":\"https://graph.microsoft.com/v1.0/me/messages?$skip="))
    {
        return rawHeaderData[0].Split("\"@odata.nextLink\":\"")[1].Split("\",\"")[0];
    }

    return "";
}

async Task<string> GetRawData(string url)
{
    var result = await httpClient.GetAsync(url);
    if (!result.IsSuccessStatusCode)
    {
        Console.WriteLine(result.StatusCode);
        Console.WriteLine(await result.Content.ReadAsStringAsync());
    }

    return result.Content.ReadAsStringAsync().Result;
}

List<string> GiveIdList(string content)
{
    var idList = new List<string>();
    var rawHeaderData = content.Split("\\\"\",\"id\":\"");
    
    for (var i = 1; i < rawHeaderData.Length; i++)
    {
        idList.Add(rawHeaderData[i].Split("\",\"")[0]);
    }

    return idList;
}

async Task ForwardEmailsByIdList(List<string> idList)
{
    var postData = new StringContent("{\n    \"comment\": \"FYI\",\n    \"toRecipients\": [\n        {\n            \"emailAddress\": {\n                \"address\": \"" + forwardToEmail + "\",\n                \"name\": \"my name\"\n            }\n        }\n    ]\n}", Encoding.UTF8, "application/json");
    foreach (var id in idList)
    {
        var result = await httpClient.PostAsync(messagesApiUrl + "/" + id + "/forward", postData);
        if (!result.IsSuccessStatusCode)
        {
            Console.WriteLine("error for id: " + id);
            Console.WriteLine(result.StatusCode);
            Console.WriteLine(await result.Content.ReadAsStringAsync());
        }
    }
}