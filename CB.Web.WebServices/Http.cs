using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CB.Web.WebServices
{
    public class HttpResult<TResult>
    {
        #region  Constructors & Destructor
        private HttpResult() { }
        #endregion


        #region  Properties & Indexers
        public string Error { get; private set; }
        public TResult Value { get; private set; }
        #endregion


        #region Implementation
        internal static async Task<HttpResult<TResult>> FromHttpContentAsync(HttpContent content)
        {
            var contentString = await content.ReadAsStringAsync();
            try
            {
                var value = JsonConvert.DeserializeObject<TResult>(contentString);
                return new HttpResult<TResult> { Value = value };
            }
            catch (Exception e1)
            {
                try
                {
                    var jObj = JObject.Parse(contentString);
                    return new HttpResult<TResult> { Error = jObj.GetValue("ExceptionMessage").Value<string>() };
                }
                catch
                {
                    return new HttpResult<TResult> { Error = e1.Message };
                }
            }
        }
        #endregion
    }

    public static class Http
    {
        #region Methods
        public static async Task<HttpResult<TResult>> DeleteAsync<TResult>(string url)
            => await DoAsync<TResult>(async client => await client.DeleteAsync(url));

        public static async Task<HttpResult<TResult>> DeleteAsync<TResult>(string url, int id)
            => await DeleteAsync<TResult>($"{url}?id={id}");

        public static async Task<HttpResult<TResult>> DoAsync<TResult>(
            Func<HttpClient, Task<HttpResponseMessage>> getResponseFunc)
        {
            using (var client = new HttpClient())
            {
                var response = await getResponseFunc(client);
                return await HttpResult<TResult>.FromHttpContentAsync(response.Content);
            }
        }

        public static async Task<HttpResult<TResult>> GetAsync<TResult>(string url)
            => await DoAsync<TResult>(async client => await client.GetAsync(url));

        public static async Task<HttpResult<TResult>> GetAsync<TResult>(string url, int id)
            => await GetAsync<TResult>($"url?id={id}");

        public static async Task<HttpResult<TResult>> PostAsync<TParam, TResult>(string url, TParam param)
            => await DoAsync<TResult>(async client => await client.PostAsJsonAsync(url, param));

        public static async Task<HttpResult<TResult>> PutAsync<TParam, TResult>(string url, TParam param)
            => await DoAsync<TResult>(async client => await client.PutAsJsonAsync(url, param));
        #endregion
    }
}