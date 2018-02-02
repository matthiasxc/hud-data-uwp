using System;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;
using Scrapers.IO;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage;
using Newtonsoft.Json;
using Windows.Storage.AccessCache;
using HuderScraper.Models;

namespace HuderScraper.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;
        private readonly INavigationService _navigationService;
        private const string savedFilesLocation = "savedFiles.json";
        private const string previousSearchesLocation = "historicalZipData.json";

        private RelayCommand<string> _navigateCommand;
        private RelayCommand _loadFileCommand;
        private RelayCommand _loadFilesOnStartCommand;
        private RelayCommand _saveLoadedFilesCommand;
        private RelayCommand _loadLoadedFilesCommand;
        private List<string> _fileTokens;
        private List<StorageFile> _loadedStorageFiles;


        #region Load and parse HUD files

        public RelayCommand LoadFileWithPickerCommand
        {
            get
            {
                return _loadFileCommand ?? (_loadFileCommand = new RelayCommand( async () =>
                {
                    var picker = new Windows.Storage.Pickers.FileOpenPicker();
                    picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
                    picker.FileTypeFilter.Add(".csv");

                    var files = await picker.PickMultipleFilesAsync();

                    List<string> fileTokens = new List<string>();

                    foreach(StorageFile file in files)
                    {
                        fileTokens.Add(StorageApplicationPermissions.FutureAccessList.Add(file));
                    }

                    await loadRentFiles(fileTokens);

                    SaveLoadedFilesCommand.Execute(null);                    
                }));
            }
        }

        public RelayCommand LoadFilesOnStartCommand
        {
            get
            {
                return _loadFilesOnStartCommand ?? (_loadFilesOnStartCommand = new RelayCommand(async () =>
                {
                    IsLoadingData = true;
                    IEnumerable<string> listOfFiles = _fileTokens as IEnumerable<string>;

                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    
                    // Load the data files
                    var fileItem = await localFolder.TryGetItemAsync(savedFilesLocation);

                    if (fileItem != null)
                    {
                        var file = await localFolder.GetFileAsync(savedFilesLocation);
                        
                        string fileString = await Windows.Storage.FileIO.ReadTextAsync(file);

                        var tokens = JsonConvert.DeserializeObject<IEnumerable<string>>(fileString);
                        _fileTokens.Clear();

                        await loadRentFiles(tokens);
                    }

                    //load the saved zip codes

                    var historicalSearch = await localFolder.TryGetItemAsync(previousSearchesLocation);

                    if (historicalSearch != null)
                    {
                        var file = await localFolder.GetFileAsync(previousSearchesLocation);

                        string historyFileString = await Windows.Storage.FileIO.ReadTextAsync(file);

                        var savedZip = JsonConvert.DeserializeObject<IEnumerable<ZipSearchResult>>(historyFileString);

                        if (HistoricalZip == null)
                            HistoricalZip = new ObservableCollection<ZipSearchResult>();

                        HistoricalZip.Clear();

                        foreach (ZipSearchResult zsr in savedZip)
                        {
                            HistoricalZip.Add(zsr);
                        }

                    }

                    IsLoadingData = false;

                }));
            }
        }

        private async Task loadRentFiles(IEnumerable<string> tokens)
        {
            foreach (string token in tokens)
            {
                var file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
                CurrentlyLoadingFile = file.DisplayName;
                var stream = await file.OpenReadAsync();
                var inputStream = stream.GetInputStreamAt(0);
                _fileTokens.Add(token);
                using (CsvFileReader csvReader = new CsvFileReader(await file.OpenStreamForReadAsync()))
                {
                    CsvRow row = new CsvRow();
                    List<string[]> zipChunk = new List<string[]>();
                    while (csvReader.ReadRow(row))
                    {
                        // If the stub column contains "bedroom", send the existing chunk for processing
                        //  and then start a new chunck
                        if (row[4].Contains("bedroom"))
                        {
                            var processedZip = ZipCodeMetric.ProcessZipChunk(zipChunk);
                            if (processedZip != null)
                            {
                                ZipCodeData.Add(processedZip);
                                //Debug.WriteLine("Zip code: " + processedZip.ZipCode + ", " + processedZip.UnitType.ToString() + ", " + processedZip.TotalUnits.ToString());
                            }

                            zipChunk.Clear();
                        }

                        zipChunk.Add(row.ToArray());
                    }

                    LoadedFiles.Add(file.DisplayName);
                }
            }
        }

        #endregion
        
        public RelayCommand SaveLoadedFilesCommand
        {
            get
            {
                return _saveLoadedFilesCommand ?? (_saveLoadedFilesCommand = new RelayCommand(async () =>
               {
                   IEnumerable<string> listOfFiles = _fileTokens as IEnumerable<string>;

                   StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                   StorageFile file = await localFolder.CreateFileAsync(savedFilesLocation, CreationCollisionOption.OpenIfExists);
                   await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(listOfFiles));
            }));
            }
        }

        #region Lookup zip code info
        
        private string _zipCodeSearch = "";
        public string ZipCodeSearch
        {
            get
            {
                return _zipCodeSearch;
            }

            set
            {
                Set(ref _zipCodeSearch, value);
            }
        }

        private string _currentlyLoadingFile = "";
        public string CurrentlyLoadingFile
        {
            get
            {
                return _currentlyLoadingFile;
            }

            set
            {
                Set(ref _currentlyLoadingFile, value);
            }
        }
        
        private ObservableCollection<ZipSearchResult> _zipResults = new ObservableCollection<ZipSearchResult>();
        public ObservableCollection<ZipSearchResult>  ZipResults
        {
            get { return _zipResults; }

            set { Set(ref _zipResults, value); }
        }
        
        private RelayCommand _getZipResultsCommand;
        public RelayCommand GetZipResultsCommand
        {
            get
            {
                return _getZipResultsCommand
                    ?? (_getZipResultsCommand = new RelayCommand(async () =>
                    {
                        // Get zip info
                        var zipCodes = from z in ZipCodeData
                                       where z.ZipCode == ZipCodeSearch
                                       select z;

                        if (zipCodes == null)
                            return;

                        ZipSearchResult formattedResult = new ZipSearchResult();
                        formattedResult.ZipCode = ZipCodeSearch;

                        foreach(ZipCodeMetric zcm in zipCodes)
                        {
                            if (zcm.UnitType == Rental.OneBed)
                                formattedResult.OneBedStats = zcm;
                            else if (zcm.UnitType == Rental.TwoBed)
                                formattedResult.TwoBedStats = zcm;
                            else if (zcm.UnitType == Rental.ThreeBed)
                                formattedResult.ThreeBedStats = zcm;
                        }

                        // Add info to display 
                        ZipResults.Add(formattedResult);

                        // Add info to historical search
                        var existingZip = HistoricalZip.FirstOrDefault(z => z.ZipCode == formattedResult.ZipCode);
                        if(existingZip != null)
                            HistoricalZip.Remove(existingZip);
                        HistoricalZip.Insert(0, formattedResult);

                        await SaveZipHistory();
                        
                    }));
            }
        }

        private RelayCommand _getZipResultsFromSelectionCommand;
        public RelayCommand GetZipResultsFromSelectionCommand
        {
            get
            {
                return _getZipResultsFromSelectionCommand
                    ?? (_getZipResultsFromSelectionCommand = new RelayCommand(async () =>
                        {
                            // Get zip info
                            var zipCodes = from z in ZipCodeData
                                           where z.ZipCode == SelectedHistoricalZip.ZipCode
                                           select z;

                            if (zipCodes == null)
                                return;

                            ZipSearchResult formattedResult = new ZipSearchResult();
                            formattedResult.ZipCode = SelectedHistoricalZip.ZipCode;
                            formattedResult.ZipDescription = SelectedHistoricalZip.ZipDescription;

                            foreach (ZipCodeMetric zcm in zipCodes)
                            {
                                if (zcm.UnitType == Rental.OneBed)
                                    formattedResult.OneBedStats = zcm;
                                else if (zcm.UnitType == Rental.TwoBed)
                                    formattedResult.TwoBedStats = zcm;
                                else if (zcm.UnitType == Rental.ThreeBed)
                                    formattedResult.ThreeBedStats = zcm;
                            }

                            // Add info to display 
                            ZipResults.Add(formattedResult);

                        }));
                    }
        }

        private async Task SaveZipHistory()
        {
            // Save historical search
            IEnumerable<ZipSearchResult> listOfZips = HistoricalZip as IEnumerable<ZipSearchResult>;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.CreateFileAsync(previousSearchesLocation, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(listOfZips));
        }

        private RelayCommand _clearZipResultsCommand;
        public RelayCommand ClearZipResultsCommand
        {
            get
            {
                return _clearZipResultsCommand
                    ?? (_clearZipResultsCommand = new RelayCommand(() =>
                    {
                        ZipResults.Clear();
                    }));
            }
        }

        private RelayCommand _changeZipDetailsCommand;
        public RelayCommand ChangeZipDetailsCommand
        {
            get
            {
                return _changeZipDetailsCommand
                    ?? (_changeZipDetailsCommand = new RelayCommand(async () => 
                    {
                        foreach (ZipSearchResult liveZsr in ZipResults) {
                            foreach (ZipSearchResult savedZsr in HistoricalZip)
                            {
                                if (liveZsr.ZipCode == savedZsr.ZipCode &&
                                liveZsr.NewZipDescription != savedZsr.ZipDescription &&
                                !string.IsNullOrEmpty(liveZsr.NewZipDescription))
                                {
                                    savedZsr.ZipDescription = liveZsr.NewZipDescription;
                                }
                            }
                        }

                        await SaveZipHistory();
                    }));
            }
        }

        private RelayCommand _removeSelectedItemCommand;
        public RelayCommand RemoveSelectedItemCommand
        {
            get
            {
                return _removeSelectedItemCommand
                    ?? (_removeSelectedItemCommand = new RelayCommand(async () =>
                    {
                        var zipResult = ZipResults.FirstOrDefault(z => z.ZipCode == SelectedHistoricalZip.ZipCode);
                        if(zipResult != null)
                            ZipResults.Remove(zipResult);
                    }));
            }
        }

        //private RelayCommand<string> _changeZipDetailsCommand;
        //public RelayCommand<string> ChangeZipDetailsCommand
        //{
        //    get
        //    {
        //        return _changeZipDetailsCommand
        //            ?? (_changeZipDetailsCommand = new RelayCommand<string>(async (zip) =>
        //            {
        //                foreach (ZipSearchResult zsr in HistoricalZip)
        //                {
        //                    if (zsr.ZipCode == zip)
        //                }
        //            }));
        //    }
        //}

        private ObservableCollection<ZipSearchResult> _historicalZip = new ObservableCollection<ZipSearchResult>();
        public ObservableCollection<ZipSearchResult> HistoricalZip
        {
            get { return _historicalZip; }
            set { Set(ref _historicalZip, value); }
        }

        private ZipSearchResult _selectedHistoricalZip = null;
        public ZipSearchResult SelectedHistoricalZip
        {
            get { return _selectedHistoricalZip; }
            set { Set(ref _selectedHistoricalZip, value); }
        }

        private ZipSearchResult _selectedLiveZip = null;
        public ZipSearchResult SelectedLiveZip
        {
            get { return _selectedLiveZip; }
            set { Set(ref _selectedLiveZip, value); }
        }

        private bool _isLoadingData = false;
        public bool IsLoadingData
        {
            get { return _isLoadingData; }
            set { Set(ref _isLoadingData, value); }
        }

        #endregion

        private ObservableCollection<ZipCodeMetric> _zipCodeData;
        public ObservableCollection<ZipCodeMetric> ZipCodeData
        {
            get
            {
                return _zipCodeData;
            }

            set
            {
                Set(ref _zipCodeData, value);
            }
        }

        private ObservableCollection<string> _loadedFiles;
        public ObservableCollection<string> LoadedFiles
        {
            get { return _loadedFiles; }
            set { _loadedFiles = value; }
        }

        public MainViewModel(
            IDataService dataService,
            INavigationService navigationService)
        {
            _dataService = dataService;
            _navigationService = navigationService;
            _zipCodeData = new ObservableCollection<ZipCodeMetric>();
            _loadedFiles = new ObservableCollection<string>();
            _fileTokens = new List<string>();
            _loadedStorageFiles = new List<StorageFile>();

            //LoadFilesOnStartCommand.Execute(null);

            Initialize();
        }
        
        private async Task Initialize()
        {
            try
            {
                var item = await _dataService.GetData();

                

            }
            catch (Exception ex)
            {

            }
        }
    }
}