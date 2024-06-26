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
    internal class ShowCustomModelAnalysis : Button
    {

        private CustomModelAnalysis _custommodelanalysis = null;

        protected override void OnClick()
        {
            //already open?
            if (_custommodelanalysis != null)
                return;
            _custommodelanalysis = new CustomModelAnalysis();
            _custommodelanalysis.Owner = FrameworkApplication.Current.MainWindow;
            _custommodelanalysis.Closed += (o, e) => { _custommodelanalysis = null; };
            _custommodelanalysis.Show();
            //uncomment for modal
            //_custommodelanalysis.ShowDialog();
        }

    }
}
