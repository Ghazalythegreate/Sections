using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DBLibrary;

namespace SOM.RevitTools.Sections
{
    class program
    {
        //*****************************CreateSection()*****************************
        public void CreateSection(Document doc, UIDocument uidoc)
        {
            LibraryGetItems libGet = new LibraryGetItems();
            LibraryCreate libCr = new LibraryCreate();
            LibraryGeometry libGeo = new LibraryGeometry();
            Element e = libGet.SelectElement(uidoc, doc);

            Wall wall = null;
            if (e != null)
                wall = e as Wall;

            // Create a BoundingBoxXYZ instance centered on wall
            BoundingBoxXYZ bb = wall.get_BoundingBox(null);
            double minZ = bb.Min.Z;
            double maxZ = bb.Max.Z;

            LocationCurve lc = wall.Location as LocationCurve;
            Line line = lc.Curve as Line;

            XYZ p = line.GetEndPoint(0);
            XYZ q = line.GetEndPoint(1);
            XYZ v = p - q; // p point 0 - q point 1 - view direction up. 
            //XYZ v = q - p; // q point 1 - p point 0 - view direction down.

            double halfLength = v.GetLength() / 2;
            double offset = 3; // offset by 3 feet. 

            //Max/Min X = Section line Length, Max/Min Y is the height of the section box, Max/Min Z far clip
            XYZ min = new XYZ(-halfLength, minZ - offset, -offset);
            XYZ max = new XYZ( halfLength, maxZ + offset, offset);

            XYZ midpoint = q + 0.5 * v; // q get lower midpoint. 
            //XYZ midpoint = q - 0.5 * v; // q get upper midpoint.
            XYZ walldir = v.Normalize();
            XYZ up = XYZ.BasisZ;
            XYZ viewdir = walldir.CrossProduct(up);

            Transform t = Transform.Identity;
            t.Origin = midpoint;
            t.BasisX = walldir;
            t.BasisY = up;
            t.BasisZ = viewdir;

            BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
            sectionBox.Transform = t;
            sectionBox.Min = min; // scope box start 
            sectionBox.Max = max; // scope box end

            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault<ViewFamilyType>(x => ViewFamily.Section == x.ViewFamily);

            //ViewFamilyType vt = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
            //    .Where(z => z.Name == "Detail Views (Beam Top Detail Views)")
            //    .First() as ViewFamilyType;

            //Create wall section view 
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Create Section");
                ViewSection.CreateSection(doc, vft.Id, sectionBox);
                tx.Commit();
            }
        }

        //*****************************RejustSectionView()*****************************
        public void RejustSectionView(Document doc, UIDocument uidoc)
        {
            //My library
            LibraryGetItems lib = new LibraryGetItems();
            LibraryGeometry libGeo = new LibraryGeometry();
            // Select wall or object. 
            Element wallElement = lib.SelectElement(uidoc, doc);

            // collect all ViewSections
            FilteredElementCollector SectionCollector = new FilteredElementCollector(doc);
            SectionCollector.OfClass(typeof(ViewSection)).WhereElementIsNotElementType();
            List<ViewSection> sections = SectionCollector.Cast<ViewSection>().Where(sh => sh.ViewType == ViewType.Section).ToList();
            // Select section from active view.
            Element sectionelem = lib.SelectElement(uidoc, doc);
            // Compare element by name to find the SectionView. 
            ViewSection section = sections.Find(t => t.ViewName == sectionelem.Name);
     
            // OFFSET
            // element to wall. 
            Wall wall = wallElement as Wall;
            LocationCurve locationCurve = wall.Location as LocationCurve;
            // Move the section element
            Line origWallLine = locationCurve.Curve as Line;
            Curve offsetWallLine = origWallLine.CreateOffset(3, XYZ.BasisZ);
            // Midpoint of the offset wall line. 
            XYZ offsetWallMid = libGeo.Midpoint(offsetWallLine as Line);
            // selected section orign point. 
            XYZ sectionOrigin = new XYZ(section.Origin.X, section.Origin.Y, 0);
            // Move distance between selected and offset. 
            XYZ transaction = offsetWallMid - sectionOrigin;
            // TEST LINES TO BE DELETED. 
            Line line = Line.CreateBound(offsetWallMid, sectionOrigin);
            Line line1 = Line.CreateBound(offsetWallLine.GetEndPoint(0), offsetWallLine.GetEndPoint(1));

            //ANGLE
            XYZ p = origWallLine.GetEndPoint(0);
            XYZ q = origWallLine.GetEndPoint(1);
            XYZ v = q - p;
            double radians = XYZ.BasisX.AngleTo(v); // Angle is using radians. 
            double Angle = radians * (180 / Math.PI); // Convert Radians to degrees.                       

            // ROTATE SECTION FIRST 
            using (Transaction tr = new Transaction(doc, "Rotate"))
            {
                tr.Start("rotate");
                try
                {
                    if (Angle > 0)
                    {
                        Line axis = Line.CreateBound(sectionOrigin, sectionOrigin + XYZ.BasisZ);
                        doc.Regenerate(); // Document regenerate to refresh document.
                        ElementTransformUtils.RotateElement(doc, sectionelem.Id, axis, radians);
                    }
                }
                catch { }
                tr.Commit();
            }

            // MOVE SECTION 
            using (Transaction t = new Transaction(doc, "Update Section"))
            {
                t.Start("Update Section");
                try
                {
                    ElementTransformUtils.MoveElement(doc, sectionelem.Id, transaction);
                }
                catch { }
                t.Commit();
            }            
        }
    }
}
