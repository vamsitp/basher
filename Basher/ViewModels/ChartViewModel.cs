using System;
using System.Collections.ObjectModel;

using Basher.Models;
using Basher.Services;

using GalaSoft.MvvmLight;

namespace Basher.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        public ChartViewModel()
        {
        }

        public ObservableCollection<DataPoint> Source
        {
            get
            {
                // TODO WTS: Replace this with your actual data
                return SampleDataService.GetChartSampleData();
            }
        }
    }
}
