using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;
using HuderScraper.ViewModel;

namespace HuderScraper
{
    public sealed partial class MainPage
    {
        public MainViewModel Vm => (MainViewModel)DataContext;

        public MainPage()
        {
            InitializeComponent();

            SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManagerBackRequested;

            Loaded += (s, e) =>
            {
                //Vm.RunClock();
                Vm.LoadFilesOnStartCommand.Execute(null);
            };
        }

        private void SystemNavigationManagerBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                e.Handled = true;
                Frame.GoBack();
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            //Vm.StopClock();
            base.OnNavigatingFrom(e);
        }

        private void ListView_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            Vm.GetZipResultsFromSelectionCommand.Execute(null);
        }

        private void TextBox_FocusDisengaged(Windows.UI.Xaml.Controls.Control sender, Windows.UI.Xaml.Controls.FocusDisengagedEventArgs args)
        {
            Vm.ChangeZipDetailsCommand.Execute(null);
        }

        private void ListView_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Delete)
            {
                Vm.RemoveSelectedItemCommand.Execute(null);
            }
        }

        private void TextBox_LostFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Vm.ChangeZipDetailsCommand.Execute(null);
        }

        private void TextBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                Vm.ChangeZipDetailsCommand.Execute(null);
            }
        }
    }
}
