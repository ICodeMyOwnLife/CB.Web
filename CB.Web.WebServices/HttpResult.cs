using System;
using System.Net.Http;
using System.Threading.Tasks;
using CB.Model.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CB.Web.WebServices
{
    public class HttpResult<TResult>: IError
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
}