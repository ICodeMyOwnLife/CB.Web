using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            DeleteCommand = new DelegateCommand<TItem>(Delete);
            DeleteItemCommand =
                new DelegateCommand(DeleteItem, () => CanDeleteItem).ObservesProperty(() => CanDeleteItem);
            EditCommand = new DelegateCommand<TItem>(Edit);
            EditItemCommand = new DelegateCommand(EditItem, () => CanEditItem).ObservesProperty(() => CanEditItem);
            SaveItemAsyncCommand =
                DelegateCommand.FromAsyncHandler(SaveItemAsync, () => CanSaveItem).ObservesProperty(() => CanSaveItem);
        }
        #endregion


        #region  Commands
        public ICommand AddNewCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand LoadAsyncCommand { get; }
        public ICommand SaveItemAsyncCommand { get; }
        #endregion


        #region  Properties & Indexers
        public bool CanDeleteItem
            => CanDelete(SelectedItem);

        public bool CanEditItem => CanEdit(SelectedItem);

        public bool CanSaveItem => CanSave(SelectedItem);

        public ConfirmRequestProvider ConfirmRequestProvider { get; } = new ConfirmRequestProvider();

        public ConfirmRequestProvider EditRequestProvider { get; } = new ConfirmRequestProvider();

        public ObservableCollection<TItem> Items
        {
            get { return _items; }
            private set { SetProperty(ref _items, value); }
        }

        public NotifyRequestProvider NotifyRequestProvider { get; } = new NotifyRequestProvider();

        public TItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != null) _selectedItem.PropertyChanged -= SelectedItem_PropertyChanged;
                if (SetProperty(ref _selectedItem, value))
                    NotifyPropertiesChanged(nameof(CanDeleteItem), nameof(CanEditItem), nameof(CanSaveItem));

                if (_selectedItem != null) _selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
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
            => Edit(new TItem());

        public void Delete(TItem item)
        {
            if (!CanDelete(item)) return;

            ConfirmRequestProvider.Confirm($"Are you sure you want to delete {ConvertItemToString(item)}?",
                async context => await TryAsync(async () =>
                {
                    if (!context.Confirmed) return;
                    var result = await _service.DeleteAsync(item);
                    if (NotifyError(result)) return;

                    if (result.Value) NotifyRequestProvider.Notify("Successful", "Deletion successful!");
                    Items.Remove(item);
                }));
        }

        public void DeleteItem()
            => Delete(SelectedItem);

        public void Edit(TItem item)
        {
            if (!CanEdit(item)) return;

            SelectedItem = item;
            EditRequestProvider.Confirm("Edit Job", this, async context =>
            {
                if (!context.Confirmed) return;
                await SaveAsync(item);
            });
        }

        public void EditItem()
            => Edit(SelectedItem);

        public async Task LoadAsync()
            => await TryAsync(async () =>
            {
                var result = await _service.GetAsync();
                if (!NotifyError(result)) Items = new ObservableCollection<TItem>(result.Value);
            });

        public async Task SaveAsync(TItem item)
        {
            if (!CanSave(item)) return;

            await TryAsync(async () =>
            {
                var result = await _service.PostAsync(item);
                if (!NotifyError(result))
                {
                    item.Id = result.Value.Id;
                    if (Items == null) await LoadAsync();

                    if (Items != null && !Items.Contains(item)) Items.Add(item);
                }
            });
        }

        public async Task SaveItemAsync()
            => await SaveAsync(SelectedItem);
        #endregion


        #region Event Handlers
        private void SelectedItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PrismModelBase.HasErrors)) NotifyPropertyChanged(nameof(CanSaveItem));
        }
        #endregion


        #region Implementation
        private static bool CanDelete(TItem item)
            => item != null && item.Id != null;

        private static bool CanEdit(TItem item)
            => item != null;

        private static bool CanSave(TItem item)
            => item != null && !item.HasErrors;

        protected virtual string ConvertItemToString(TItem item)
            => item.ToString();

        private bool NotifyError(IError result)
        {
            if (string.IsNullOrEmpty(result.Error)) return false;

            NotifyRequestProvider.NotifyError(result.Error);
            return true;
        }

        private async Task TryAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception exception)
            {
                NotifyRequestProvider.NotifyError(exception.Message);
            }
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