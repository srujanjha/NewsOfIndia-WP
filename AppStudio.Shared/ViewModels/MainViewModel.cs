using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Net.NetworkInformation;

using Windows.UI.Xaml;

using AppStudio.Services;
using AppStudio.Data;

namespace AppStudio.ViewModels
{
    public class MainViewModel : BindableBase
    {
       private QuestionablecontentViewModel _questionablecontentModel;
       private PennyArcadeViewModel _pennyArcadeModel;
       private PvponlineViewModel _pvponlineModel;
       private CatandgirlViewModel _catandgirlModel;
        private PrivacyViewModel _privacyModel;

        private ViewModelBase _selectedItem = null;

        public MainViewModel()
        {
            _selectedItem = QuestionablecontentModel;
            _privacyModel = new PrivacyViewModel();

        }
 
        public QuestionablecontentViewModel QuestionablecontentModel
        {
            get { return _questionablecontentModel ?? (_questionablecontentModel = new QuestionablecontentViewModel()); }
        }
 
        public PennyArcadeViewModel PennyArcadeModel
        {
            get { return _pennyArcadeModel ?? (_pennyArcadeModel = new PennyArcadeViewModel()); }
        }
 
        public PvponlineViewModel PvponlineModel
        {
            get { return _pvponlineModel ?? (_pvponlineModel = new PvponlineViewModel()); }
        }
 
        public CatandgirlViewModel CatandgirlModel
        {
            get { return _catandgirlModel ?? (_catandgirlModel = new CatandgirlViewModel()); }
        }

        public void SetViewType(ViewTypes viewType)
        {
            QuestionablecontentModel.ViewType = viewType;
            PennyArcadeModel.ViewType = viewType;
            PvponlineModel.ViewType = viewType;
            CatandgirlModel.ViewType = viewType;
        }

        public ViewModelBase SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetProperty(ref _selectedItem, value);
                UpdateAppBar();
            }
        }

        public Visibility AppBarVisibility
        {
            get
            {
                return SelectedItem == null ? AboutVisibility : SelectedItem.AppBarVisibility;
            }
        }

        public Visibility AboutVisibility
        {

         get { return Visibility.Visible; }
        }

        public void UpdateAppBar()
        {
            OnPropertyChanged("AppBarVisibility");
            OnPropertyChanged("AboutVisibility");
        }

        /// <summary>
        /// Load ViewModel items asynchronous
        /// </summary>
        public async Task LoadData(bool isNetworkAvailable)
        {
            var loadTasks = new Task[]
            { 
                QuestionablecontentModel.LoadItems(isNetworkAvailable),
                PennyArcadeModel.LoadItems(isNetworkAvailable),
                PvponlineModel.LoadItems(isNetworkAvailable),
                CatandgirlModel.LoadItems(isNetworkAvailable),
            };
            await Task.WhenAll(loadTasks);
        }

        /// <summary>
        /// Refresh ViewModel items asynchronous
        /// </summary>
        public async Task RefreshData(bool isNetworkAvailable)
        {
            var refreshTasks = new Task[]
            { 
                QuestionablecontentModel.RefreshItems(isNetworkAvailable),
                PennyArcadeModel.RefreshItems(isNetworkAvailable),
                PvponlineModel.RefreshItems(isNetworkAvailable),
                CatandgirlModel.RefreshItems(isNetworkAvailable),
            };
            await Task.WhenAll(refreshTasks);
        }

        //
        //  ViewModel command implementation
        //
        public ICommand RefreshCommand
        {
            get
            {
                return new DelegateCommand(async () =>
                {
                    await RefreshData(NetworkInterface.GetIsNetworkAvailable());
                });
            }
        }

        public ICommand AboutCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    NavigationServices.NavigateToPage("AboutThisAppPage");
                });
            }
        }

        public ICommand PrivacyCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    NavigationServices.NavigateTo(_privacyModel.Url);
                });
            }
        }
    }
}
