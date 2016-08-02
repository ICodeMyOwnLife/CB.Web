using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CB.Model.Common;
using CB.Model.Prism;
using CB.Prism.Interactivity;
using Prism.Commands;


namespace CB.Web.WebServices
{
    public abstract class WebServiceClientViewModelBase<TItem, TId>: PrismViewModelBase
        where TItem: PrismModelBase, IIdentification<TId>, new()
    {
        #region Fields
        private ObservableCollection<TItem> _items;
        private TItem _selectedItem;
        private readonly WebServiceClient<TItem, TId> _service;
        private string _status;
        #endregion


        #region  Constructors & Destructor
        protected WebServiceClientViewModelBase(WebServiceClient<TItem, TId> service)
        {
            _service = service;
            AddNewCommand = new DelegateCommand(AddNew);
            LoadAsyncCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            SaveAsyncCommand = DelegateCommand.FromAsyncHandler(SaveAsync, () => CanSave).ObservesProperty(() => CanSave);
            DeleteAsyncCommand = new DelegateCommand(Delete, () => CanDelete).ObservesProperty(() => CanDelete);
        }
        #endregion


        #region  Commands
        public ICommand AddNewCommand { get; }
        public ICommand DeleteAsyncCommand { get; }
        public ICommand LoadAsyncCommand { get; }
        public ICommand SaveAsyncCommand { get; }
        #endregion


        #region  Properties & Indexers
        public bool CanDelete => SelectedItem != null && SelectedItem.Id != null;

        public bool CanSave => SelectedItem != null && !SelectedItem.HasErrors;

        public ConfirmRequestProvider ConfirmRequestProvider { get; } = new ConfirmRequestProvider();

        public ObservableCollection<TItem> Items
        {
            get { return _items; }
            private set { SetProperty(ref _items, value); }
        }

        public TItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (SetProperty(ref _selectedItem, value)) NotifyPropertiesChanged(nameof(CanDelete), nameof(CanSave));
            }
        }

        public string Status
        {
            get { return _status; }
            private set { SetProperty(ref _status, value); }
        }
        #endregion


        #region Methods
        public void AddNew()
            => SelectedItem = new TItem();

        public void Delete()
        {
            ConfirmRequestProvider.Confirm($"Are you sure you want to delete {ConvertItemToString(SelectedItem)}?",
                async context =>
                {
                    if (!context.Confirmed) return;
                    var result = await _service.DeleteAsync(SelectedItem);
                    if (SetError(result)) return;

                    if (result.Value) Status = "Deletion successful!";
                    Items.Remove(SelectedItem);
                });
        }

        public async Task LoadAsync()
        {
            var result = await _service.GetAsync();
            if (!SetError(result)) Items = new ObservableCollection<TItem>(result.Value);
        }

        public async Task SaveAsync()
        {
            var result = await _service.PostAsync(SelectedItem);
            if (!SetError(result))
            {
                SelectedItem.Id = result.Value.Id;
                if (Items == null) await LoadAsync();

                if (Items != null && !Items.Contains(SelectedItem)) Items.Add(SelectedItem);
            }
        }
        #endregion


        #region Implementation
        protected virtual string ConvertItemToString(TItem item)
            => item.ToString();

        private bool SetError(IError result)
        {
            if (string.IsNullOrEmpty(result.Error)) return false;

            Status = result.Error;
            return true;
        }
        #endregion
    }

    public abstract class WebServiceClientViewModelBase<TItem, TId, TService>: WebServiceClientViewModelBase<TItem, TId>
        where TItem: PrismModelBase, IIdentification<TId>, new() where TService: WebServiceClient<TItem, TId>, new()
    {
        #region  Constructors & Destructor
        protected WebServiceClientViewModelBase(TService service): base(service) { }

        protected WebServiceClientViewModelBase(): this(new TService()) { }
        #endregion
    }
}