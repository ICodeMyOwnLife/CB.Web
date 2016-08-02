using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace CB.Web.WebServices
{
    public static class Http
    {
        #region Methods
        public static async Task<HttpResult<TResult>> DeleteAsync<TResult>(string url)
            => await DoAsync<TResult>(async client => await client.DeleteAsync(url));

        public static async Task<HttpResult<TResult>> DeleteAsync<TResult>(string url, object id)
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

        public static async Task<HttpResult<TResult>> GetAsync<TResult>(string url, object id)
            => await GetAsync<TResult>($"url?id={id}");

        public static async Task<HttpResult<TResult>> PostAsync<TParam, TResult>(string url, TParam param)
            => await DoAsync<TResult>(async client => await client.PostAsJsonAsync(url, param));

        public static async Task<HttpResult<TResult>> PutAsync<TParam, TResult>(string url, TParam param)
            => await DoAsync<TResult>(async client => await client.PutAsJsonAsync(url, param));
        #endregion
    }
}