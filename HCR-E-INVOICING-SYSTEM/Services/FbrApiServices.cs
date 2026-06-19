using System.Linq;
//using LiveCharts;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class FbrApiService
{
    private readonly HttpClient _client;

    public FbrApiService()
    {
        // Ensure TLS 1.2 is enabled (helps with older frameworks and some server configs)
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(100)
        };

        // Default headers
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("HCR-E-INVOICING-SYSTEM/1.0");
    }

    /// <summary>
    /// Sanitize input for JSON payloads: remove control characters, collapse whitespace and enforce max length.
    /// Use before placing user-supplied text (product descriptions, notes, addresses) into payload.
    /// Converts multiline text to single line by replacing newlines with spaces.
    /// </summary>
    public static string SanitizeForJson(string input, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        // Step 1: Replace all types of line breaks with space
        string s = input.Replace("\r\n", " ")  // Windows line break
                        .Replace("\r", " ")     // Mac line break
                        .Replace("\n", " ")     // Unix line break
                        .Replace("\t", " ");    // Tab character
        
        // Step 2: Remove all other control characters (including null, bell, etc.)
        s = Regex.Replace(s, @"[\x00-\x1F\x7F]", " ");
        
        // Step 3: Collapse multiple consecutive spaces into single space
        s = Regex.Replace(s, @"\s+", " ").Trim();
        
        // Step 4: Enforce maximum length (trim on word boundary if possible)
        if (s.Length > maxLength)
        {
            s = s.Substring(0, maxLength).Trim();
            // If we cut off in the middle of a word, remove the trailing partial word
            int lastSpace = s.LastIndexOf(' ');
            if (lastSpace > maxLength / 2) // Only apply word boundary if we have enough content before
            {
                s = s.Substring(0, lastSpace).Trim();
            }
        }
        
        return s;
    }

    /// <summary>
    /// Clean JSON object by removing empty strings and null values that FBR API rejects.
    /// This prevents "Requested JSON in Malformed" errors for empty/invalid fields.
    /// Also recursively sanitizes all string values to remove multiline/control characters.
    /// </summary>
    private static JObject CleanJsonObject(JObject obj)
    {
        // Remove properties with empty string or null values
        var emptyProps = obj.Properties()
            .Where(p => p.Value.Type == JTokenType.String && string.IsNullOrWhiteSpace(p.Value.ToString()))
            .Union(obj.Properties().Where(p => p.Value.Type == JTokenType.Null))
            .ToList();

        foreach (var prop in emptyProps)
        {
            prop.Remove();
        }

        // Recursively clean nested objects and arrays, and sanitize string values
        foreach (var prop in obj.Properties().ToList())
        {
            if (prop.Value.Type == JTokenType.String)
            {
                // Sanitize string values to remove newlines and control characters
                string strVal = prop.Value.ToString();
                if (!string.IsNullOrEmpty(strVal))
                {
                    // Field-specific max lengths for FBR API compliance
                    int maxLen = 200;
                    if (prop.Name.Equals("productDescription", StringComparison.OrdinalIgnoreCase))
                        maxLen = 250;  // Product descriptions can be longer
                    else if (prop.Name.Equals("sellerAddress", StringComparison.OrdinalIgnoreCase) || 
                             prop.Name.Equals("buyerAddress", StringComparison.OrdinalIgnoreCase))
                        maxLen = 300;  // Addresses can be longer
                    else if (prop.Name.Equals("sellerBusinessName", StringComparison.OrdinalIgnoreCase) || 
                             prop.Name.Equals("buyerBusinessName", StringComparison.OrdinalIgnoreCase))
                        maxLen = 200;
                    else if (prop.Name.Equals("notes", StringComparison.OrdinalIgnoreCase) || 
                             prop.Name.Equals("saleType", StringComparison.OrdinalIgnoreCase))
                        maxLen = 100;
                    
                    prop.Value = SanitizeForJson(strVal, maxLen);
                }
            }
            else if (prop.Value.Type == JTokenType.Object)
            {
                CleanJsonObject((JObject)prop.Value);
            }
            else if (prop.Value.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)prop.Value)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        CleanJsonObject((JObject)item);
                    }
                    else if (item.Type == JTokenType.String)
                    {
                        // Sanitize strings in arrays with field-specific lengths
                        string strVal = item.ToString();
                        if (!string.IsNullOrEmpty(strVal))
                        {
                            // In arrays, assume these could be descriptions, so use 250
                            ((JValue)item).Value = SanitizeForJson(strVal, 250);
                        }
                    }
                }
            }
        }

        return obj;
    }

    private class SendResult
    {
        public bool Success { get; set; }
        public HttpResponseMessage Response { get; set; }
        public string Error { get; set; }
        public SendResult(bool success, HttpResponseMessage response, string error)
        {
            Success = success;
            Response = response;
            Error = error;
        }
    }

    /// <summary>
    /// Sends an HttpRequestMessage with simple retry and timeout handling.
    /// Returns a SendResult object containing success status, response, and error.
    /// </summary>
    private async Task<SendResult> SendWithRetriesAsync(HttpRequestMessage request, int maxAttempts = 5, int timeoutSeconds = 180)
    {
        int attempt = 0;
        var rand = new Random();

        while (attempt < maxAttempts)
        {
            attempt++;
            using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                try
                {
                    // Use ResponseContentRead to ensure we wait for full content
                    var resp = await _client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token).ConfigureAwait(false);

                    // Log response for debugging
                    try
                    {
                        string respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        SaveLog(string.Format("HTTP {0} {1} - {2}\nResponse:\n{3}", (int)resp.StatusCode, resp.ReasonPhrase, request.RequestUri, respBody));

                        // Save separate response file for deep debugging
                        try
                        {
                            string respPath = Path.Combine(Path.GetTempPath(), string.Format("fbr_response_{0:yyyyMMdd_HHmmss}_attempt{1}.txt", DateTime.Now, attempt));
                            File.WriteAllText(respPath, respBody, Encoding.UTF8);
                        }
                        catch { }
                    }
                    catch { }

                    return new SendResult(true, resp, null);
                }
                catch (TaskCanceledException tce)
                {
                    if (cts.IsCancellationRequested)
                    {
                        // timeout
                        string msg = string.Format("Request timed out after {0} seconds on attempt {1}.", timeoutSeconds, attempt);
                        System.Diagnostics.Debug.WriteLine(msg + " " + tce.Message);
                        SaveLog(msg + " " + tce.Message);
                        if (attempt >= maxAttempts)
                            return new SendResult(false, null, msg + " Consider increasing timeout or retrying later.");
                    }
                    else
                    {
                        // Some other cancellation
                        string msg = "Request was cancelled.";
                        System.Diagnostics.Debug.WriteLine(msg + " " + tce.Message);
                        SaveLog(msg + " " + tce.Message);
                        return new SendResult(false, null, msg);
                    }
                }
                catch (HttpRequestException hre)
                {
                    string logMsg = string.Format("HttpRequestException on attempt {0}: {1}", attempt, hre.Message);
                    System.Diagnostics.Debug.WriteLine(logMsg);
                    SaveLog(logMsg);
                    if (attempt >= maxAttempts)
                        return new SendResult(false, null, "Network error: " + hre.Message);
                }
                catch (Exception ex)
                {
                    string logMsg = string.Format("Unexpected error on attempt {0}: {1}", attempt, ex.Message);
                    System.Diagnostics.Debug.WriteLine(logMsg);
                    SaveLog(logMsg);
                    return new SendResult(false, null, "Unexpected error: " + ex.Message);
                }
            }

            // Exponential backoff with jitter
            int delayMs = (int)(Math.Pow(2, attempt) * 500) + rand.Next(0, 300);
            await Task.Delay(delayMs).ConfigureAwait(false);
        }

        string exhausted = "Retries exhausted";
        SaveLog(exhausted);
        return new SendResult(false, null, exhausted);
    }

    // Persist log entries to temp file for later inspection
    private void SaveLog(string message)
    {
        try
        {
            string logPath = Path.Combine(Path.GetTempPath(), "fbr_api_log.txt");
            string entry = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}{2}{3}", DateTime.Now, message, Environment.NewLine, Environment.NewLine);
            File.AppendAllText(logPath, entry, Encoding.UTF8);
        }
        catch { }
    }

    public async Task<string> PostInvoiceDataAsync(string jsonPayload, string sellerToken)
    {
        try
        {
            // Parse, clean, and re-serialize to remove empty/null values and sanitize strings
            try
            {
                var jsonObj = JObject.Parse(jsonPayload);
                jsonObj = CleanJsonObject(jsonObj);
                jsonPayload = jsonObj.ToString(Formatting.Indented);
            }
            catch (Exception cleanEx)
            {
                // If cleaning fails, log but continue with original payload
                System.Diagnostics.Debug.WriteLine("JSON cleaning warning: " + cleanEx.Message);
                SaveLog("JSON cleaning warning: " + cleanEx.Message);
            }

            // Always save outgoing payload for debugging
            try
            {
                string debugPath = Path.Combine(Path.GetTempPath(), "last_request_payload.json");
                File.WriteAllText(debugPath, jsonPayload, Encoding.UTF8);
            }
            catch { }

            // Quick JSON validity check before sending
            try
            {
                JToken.Parse(jsonPayload);
            }
            catch (Exception ex)
            {
                try
                {
                    string errPath = Path.Combine(Path.GetTempPath(), "invalid_payload_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
                    File.WriteAllText(errPath, jsonPayload, Encoding.UTF8);
                    SaveLog("JSON parse failed before send: " + ex.Message);
                    return string.Format("LOCAL_ERROR: JSON parse failed: {0}. Payload saved to: {1}", ex.Message, errPath);
                }
                catch
                {
                    return string.Format("LOCAL_ERROR: JSON parse failed: {0}. Additionally, failed to save payload to temp file.", ex.Message);
                }
            }

            /*For live*/ //var request = new HttpRequestMessage(HttpMethod.Post, "https://gw.fbr.gov.pk/di_data/v1/di/postinvoicedata");
           var request = new HttpRequestMessage(HttpMethod.Post, "https://gw.fbr.gov.pk/di_data/v1/di/postinvoicedata_sb");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
            request.Headers.Add("Cookie", "key=value; JSESSIONID=6dh2TLgZ6MNrzrPMw2tQonVWS6CdgRt-MgoupnKM.i01-irisdmz55; cookiesession1=678B2A2ED92C48287169612504B199D0");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            SaveLog("Sending POST to validate endpoint: " + request.RequestUri);

            var sendResult = await SendWithRetriesAsync(request, maxAttempts: 5, timeoutSeconds: 180).ConfigureAwait(false);
            if (!sendResult.Success)
            {
                return "REQUEST_ERROR: " + sendResult.Error;
            }

            var response = sendResult.Response;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Save response for debugging
            try
            {
                string respPath = Path.Combine(Path.GetTempPath(), "last_post_response.txt");
                File.WriteAllText(respPath, responseBody, Encoding.UTF8);
            }
            catch { }

            SaveLog(string.Format("Received HTTP {0} {1}", (int)response.StatusCode, response.ReasonPhrase));

            return string.Format("HTTP {0} {1}\nContent-Type: {2}\n\nResponse Body:\n{3}", 
                                 (int)response.StatusCode, 
                                 response.ReasonPhrase, 
                                 response.Content.Headers.ContentType, 
                                 responseBody);
        }
        catch (Exception ex)
        {
            SaveLog("PostInvoiceDataAsync exception: " + ex.Message);
            return ex.ToString();
        }
    }

    public async Task<string> ValidateInvoiceDataAsync(string jsonPayload, string sellerToken)
    {
        try
        {
            // Parse, clean, and re-serialize to remove empty/null values and sanitize strings
            try
            {
                var jsonObj = JObject.Parse(jsonPayload);
                jsonObj = CleanJsonObject(jsonObj);
                jsonPayload = jsonObj.ToString(Formatting.Indented);
            }
            catch (Exception cleanEx)
            {
                // If cleaning fails, log but continue with original payload
                System.Diagnostics.Debug.WriteLine("JSON cleaning warning: " + cleanEx.Message);
                SaveLog("JSON cleaning warning: " + cleanEx.Message);
            }

            // Save outgoing payload for debugging
            try
            {
                string debugPath = Path.Combine(Path.GetTempPath(), "last_validate_payload.json");
                File.WriteAllText(debugPath, jsonPayload, Encoding.UTF8);
            }
            catch { }

            // Quick JSON validity check
            try
            {
                JToken.Parse(jsonPayload);
            }
            catch (Exception ex)
            {
                try
                {
                    string errPath = Path.Combine(Path.GetTempPath(), "invalid_validate_payload_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
                    File.WriteAllText(errPath, jsonPayload, Encoding.UTF8);
                    SaveLog("JSON parse failed before validate: " + ex.Message);
                    return string.Format("LOCAL_ERROR: JSON parse failed: {0}. Payload saved to: {1}", ex.Message, errPath);
                }
                catch
                {
                    return string.Format("LOCAL_ERROR: JSON parse failed: {0}. Additionally, failed to save payload to temp file.", ex.Message);
                }
            }

            /*For live*/ //var request = new HttpRequestMessage(HttpMethod.Post, "https://gw.fbr.gov.pk/di_data/v1/di/validateinvoicedata_sb");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://gw.fbr.gov.pk/di_data/v1/di/validateinvoicedata_sb");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
            request.Headers.Add("Cookie", "key=value; JSESSIONID=6dh2TLgZ6MNrzrPMw2tQonVWS6CdgRt-MgoupnKM.i01-irisdmz55; cookiesession1=678B2A2ED92C48287169612504B199D0");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            SaveLog("Sending VALIDATE to endpoint: " + request.RequestUri);

            var sendResult = await SendWithRetriesAsync(request, maxAttempts: 5, timeoutSeconds: 180).ConfigureAwait(false);
            if (!sendResult.Success)
            {
                return "REQUEST_ERROR: " + sendResult.Error;
            }

            var response = sendResult.Response;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Save response for debugging
            try
            {
                string respPath = Path.Combine(Path.GetTempPath(), "last_validate_response.txt");
                File.WriteAllText(respPath, responseBody, Encoding.UTF8);
            }
            catch { }

            SaveLog(string.Format("Received HTTP {0} {1} on validate", (int)response.StatusCode, response.ReasonPhrase));

            return string.Format("HTTP {0} {1}\nContent-Type: {2}\n\nResponse Body:\n{3}", 
                                 (int)response.StatusCode, 
                                 response.ReasonPhrase, 
                                 response.Content.Headers.ContentType, 
                                 responseBody);
        }
        catch (Exception ex)
        {
            SaveLog("ValidateInvoiceDataAsync exception: " + ex.Message);
            return ex.ToString();
        }
    }

}