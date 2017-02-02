#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace SOM.RevitTools.Sections
{
    [Transaction(TransactionMode.Manual)]
    public class CommandUpdate : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            program p = new program();
            p.RejustSectionView(doc, uidoc);

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class CommandCreate : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            program p = new program();
            p.CreateSection(doc, uidoc);

            return Result.Succeeded;
        }
    }
}
