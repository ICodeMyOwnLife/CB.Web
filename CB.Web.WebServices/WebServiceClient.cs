using System.Threading.Tasks;
using CB.Model.Common;


namespace CB.Web.WebServices
{
    public abstract class WebServiceClient<TItem, TId> where TItem: IIdentification<TId>
    {
        #region Fields
        private readonly string _url;
        #endregion


        #region  Constructors & Destructor
        protected WebServiceClient(string url)
        {
            _url = url;
        }
        #endregion


        #region Methods
        public virtual async Task<HttpResult<bool>> DeleteAsync(TId id)
            => await Http.DeleteAsync<bool>(_url, id);

        public virtual async Task<HttpResult<bool>> DeleteAsync(TItem item)
            => await DeleteAsync(item.Id);

        public virtual async Task<HttpResult<TItem[]>> GetAsync()
            => await Http.GetAsync<TItem[]>(_url);

        public virtual async Task<HttpResult<TItem>> GetAsync(TId id)
            => await Http.GetAsync<TItem>(_url, id);

        public virtual async Task<HttpResult<TItem>> PostAsync(TItem item)
            => await Http.PostAsync<TItem, TItem>(_url, item);

        public virtual async Task<HttpResult<TItem>> PutAsync(TItem item)
            => await Http.PutAsync<TItem, TItem>(_url, item);
        #endregion
    }
}