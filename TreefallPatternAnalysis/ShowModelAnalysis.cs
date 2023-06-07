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
    internal class ShowModelAnalysis : Button
    {

        private ModelAnalysis _modelanalysis = null;

        protected override void OnClick()
        {
            //already open?
            if (_modelanalysis != null)
                return;
            _modelanalysis = new ModelAnalysis();
            _modelanalysis.Owner = FrameworkApplication.Current.MainWindow;
            _modelanalysis.Closed += (o, e) => { _modelanalysis = null; };
            _modelanalysis.Show();
            //uncomment for modal
            //_modelanalysis.ShowDialog();
        }

    }
}
