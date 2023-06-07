using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreefallPatternAnalysis
{
    internal class ShowMainPage : Button
    {

        private MainPage _mainpage = null;

        protected override void OnClick()
        {
            //already open?
            if (_mainpage != null)
                return;
            _mainpage = new MainPage();
            _mainpage.Owner = FrameworkApplication.Current.MainWindow;
            _mainpage.Closed += (o, e) => { _mainpage = null; };
            _mainpage.Show();
            //uncomment for modal
            //_mainpage.ShowDialog();
        }

    }
}
