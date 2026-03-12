using System.Text.Json;
using Xunit.Abstractions;

namespace VoucherService.Tests.Common;

public static class HttpLoggingExtensions
{
    public static async Task<HttpResponseMessage> SendWithLoggingAsync(
        this HttpClient client,
        HttpRequestMessage request,
        ITestOutputHelper output)
    {
        output.WriteLine("");
        output.WriteLine("========== REQUEST ==========");
        output.WriteLine($"{request.Method} {request.RequestUri}");
        
        if (request.Content != null)
        {
            output.WriteLine("");
            output.WriteLine("------ HEADERS ------");
            foreach (var header in request.Headers)
            {
                output.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            
            var requestContent = await request.Content.ReadAsStringAsync();
            output.WriteLine("");
            output.WriteLine("------ PAYLOAD ------");
            
            // Format JSON if content is JSON
            if (request.Content.Headers?.ContentType?.MediaType?.Contains("json") == true)
            {
                try
                {
                    // Parse and reformat with indentation
                    var jsonObj = JsonDocument.Parse(requestContent);
                    var formattedJson = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
                    output.WriteLine(formattedJson);
                }
                catch
                {
                    // If JSON parsing fails, output raw content
                    output.WriteLine(requestContent);
                }
            }
            else
            {
                output.WriteLine(requestContent);
            }
        }
        
        output.WriteLine("========== REQUEST END ==========");
        output.WriteLine("");
        
        // Send request
        var response = await client.SendAsync(request);
        
        // Log response
        output.WriteLine("========== RESPONSE ==========");
        output.WriteLine($"{(int)response.StatusCode} {response.StatusCode}");
        
        output.WriteLine("");
        output.WriteLine("------ HEADERS ------");
        foreach (var header in response.Headers)
        {
            output.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
        
        var responseContent = await response.Content.ReadAsStringAsync();
        output.WriteLine("");
        output.WriteLine("------ CONTENT ------");
        
        // Format JSON if content is JSON
        if (response.Content.Headers?.ContentType?.MediaType?.Contains("json") == true)
        {
            try
            {
                // Parse and reformat with indentation
                var jsonObj = JsonDocument.Parse(responseContent);
                var formattedJson = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
                output.WriteLine(formattedJson);
            }
            catch
            {
                // If JSON parsing fails, output raw content
                output.WriteLine(responseContent);
            }
        }
        else
        {
            output.WriteLine(responseContent);
        }
        
        output.WriteLine("========== RESPONSE END ==========");
        output.WriteLine("");
        
        return response;
    }
}