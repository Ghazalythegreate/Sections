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
        //*****************************CreateSectionStart()*****************************
        public void CreateSectionStart(Document doc, UIDocument uidoc)
        {
            // My library 
            LibraryGetItems libGet = new LibraryGetItems();

            Element e = libGet.SelectElement(uidoc, doc);

            Wall wall = null;
            if (e != null)
                wall = e as Wall;

            // Create a BoundingBoxXYZ instance centered on wall
            BoundingBoxXYZ bb = wall.get_BoundingBox(null);
            double minZ = bb.Min.Z;
            double maxZ = bb.Max.Z;
            double h = maxZ - minZ;

            LocationCurve lc = wall.Location as LocationCurve;
            Line line = lc.Curve as Line;

            XYZ p = line.GetEndPoint(0);
            XYZ q = line.GetEndPoint(1);
            XYZ v = p - q; // p point 0 - q point 1 - view direction up. 

            double halfLength = v.GetLength() / 2;
            double offset = 3; // offset by 3 feet. 

            //Max/Min X = Section line Length, Max/Min Y is the height of the section box, Max/Min Z far clip
            XYZ min = new XYZ(-halfLength, -h - 1, -offset);
            XYZ max = new XYZ(halfLength, h + 1, offset);

            XYZ midpoint = q + 0.5 * v; // q get lower midpoint. 
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

            ViewFamilyType vft = viewFamilyType(doc);
            View viewTemplate = libGet.GetViewTemplate(doc, "Z-ELEV ARCH OFF");
            ViewSection vs = null;

            //Create wall section view 
            using (Transaction tx = new Transaction(doc))
            {
                try
                {
                    tx.Start("Create Section");
                    vs = ViewSection.CreateSection(doc, vft.Id, sectionBox);
                    tx.Commit();
                }
                catch { }
            }
            
            using (Transaction tr = new Transaction(doc))
            {
                try
                {
                    tr.Start("Apply Structural View Template");
                    vs.ViewTemplateId = viewTemplate.Id;
                    vs.Scale = 24;
                    tr.Commit();
                }
                catch { }
            }
        }

        //*****************************CreateSectionEnd()*****************************
        public void CreateSectionEnd(Document doc, UIDocument uidoc)
        {
            // My library 
            LibraryGetItems libGet = new LibraryGetItems();

            Element e = libGet.SelectElement(uidoc, doc);

            Wall wall = null;
            if (e != null)
                wall = e as Wall;

            // Create a BoundingBoxXYZ instance centered on wall
            BoundingBoxXYZ bb = wall.get_BoundingBox(null);
            double minZ = bb.Min.Z;
            double maxZ = bb.Max.Z;
            double h = maxZ - minZ;

            LocationCurve lc = wall.Location as LocationCurve;
            Line line = lc.Curve as Line;

            XYZ p = line.GetEndPoint(0);
            XYZ q = line.GetEndPoint(1);
            double dist = p.DistanceTo(q);
            XYZ v = q - p; // q point 1 - p point 0 - view direction down.

            double halfLength = v.GetLength() / 2;
            double offset = 3; // offset by 3 feet. 

            //Max/Min X = Section line Length, Max/Min Y is the height of the section box, Max/Min Z far clip
            XYZ min = new XYZ(-halfLength, -h - 1, -offset);
            XYZ max = new XYZ(halfLength, h + 1, offset);

            XYZ midpoint = q - 0.5 * v; // q get upper midpoint.
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

            ViewFamilyType vft = viewFamilyType(doc);
            View viewTemplate = libGet.GetViewTemplate(doc, "Z-ELEV ARCH OFF");
            ViewSection vs = null;

            //Create wall section view 
            using (Transaction tx = new Transaction(doc))
            {
                try
                {
                    tx.Start("Create Section");
                    vs = ViewSection.CreateSection(doc, vft.Id, sectionBox);
                    doc.Regenerate();
                    tx.Commit();
                }
                catch { }
            }

            using (Transaction tr = new Transaction(doc))
            {
                try
                {
                    tr.Start("Apply Structural View Template");
                    vs.ViewTemplateId = viewTemplate.Id;
                    vs.Scale = 24;
                    tr.Commit();
                }
                catch { }
            }
        }

        //*****************************CreateSectionPerpendicular()*****************************
        public void CreateSectionPerpendicular(Document doc, UIDocument uidoc)
        {
            // My library 
            LibraryGetItems libGet = new LibraryGetItems();

            Element e = libGet.SelectElement(uidoc, doc);

            Wall wall = null;
            if (e != null)
                wall = e as Wall;
            LocationCurve lc = wall.Location as LocationCurve;
            Transform curveTransform = lc.Curve.ComputeDerivatives(0.5, true);

            // The transform contains the location curve
            // mid-point and tangent, and we can obtain
            // its normal in the XY plane:

            XYZ origin = curveTransform.Origin;
            XYZ viewdir = curveTransform.BasisX.Normalize();
            XYZ up = XYZ.BasisZ;
            XYZ right = up.CrossProduct(viewdir);

            // Set up view transform, assuming wall's "up" 
            // is vertical. For a non-vertical situation 
            // such as section through a sloped floor, the 
            // surface normal would be needed

            Transform transform = Transform.Identity;
            transform.Origin = origin;
            transform.BasisX = right;
            transform.BasisY = up;
            transform.BasisZ = viewdir;

            BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
            sectionBox.Transform = transform;

            // Min & Max X values define the section
            // line length on each side of the wall.
            // Max Y is the height of the section box.
            // Max Z (5) is the far clip offset.

            double d = wall.WallType.Width;
            BoundingBoxXYZ bb = wall.get_BoundingBox(null);
            double minZ = bb.Min.Z;
            double maxZ = bb.Max.Z;
            double h = maxZ - minZ;

            sectionBox.Min = new XYZ(-2 * d,-h -1, 0);
            sectionBox.Max = new XYZ(2 * d, h + 1, 5);

            ViewFamilyType vft = viewFamilyType(doc);
            View viewTemplate = libGet.GetViewTemplate(doc, "Z-ELEV ARCH OFF");
            ViewSection vs = null;
            //Create wall section view 
            using (Transaction tx = new Transaction(doc))
            {
                try
                {
                    tx.Start("Create Section");
                    vs = ViewSection.CreateSection(doc, vft.Id, sectionBox);
                    doc.Regenerate();
                    tx.Commit();
                }
                catch { }
            }
            using (Transaction t = new Transaction(doc))
            {
                try
                {
                    t.Start("Apply Structural View Template");
                    vs.ViewTemplateId = viewTemplate.Id;
                    vs.Scale = 24;
                    t.Commit();
                }
                catch { }
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
            
            //ANGLE
            XYZ s = origWallLine.GetEndPoint(0);
            XYZ e = origWallLine.GetEndPoint(1);
            XYZ v = e - s;
            int compare = libGeo.Compare(s, e);

            if (e.X > s.X && compare < 1)
            {
                if (e.Y < s.Y)
                {
                    v = s - e;

                    offsetWallLine = origWallLine.CreateOffset(-3, XYZ.BasisZ);
                    // Midpoint of the offset wall line. 
                    offsetWallMid = libGeo.Midpoint(offsetWallLine as Line);
                    // selected section orign point. 
                    sectionOrigin = new XYZ(section.Origin.X, section.Origin.Y, 0);
                    // Move distance between selected and offset. 
                    transaction = offsetWallMid - sectionOrigin;
                }
            }

            if (e.Y < s.Y && compare == 1)
            {
                if (e.X < s.X)
                {
                    v = s - e;

                    offsetWallLine = origWallLine.CreateOffset(-3, XYZ.BasisZ);
                    // Midpoint of the offset wall line. 
                    offsetWallMid = libGeo.Midpoint(offsetWallLine as Line);
                    // selected section orign point. 
                    sectionOrigin = new XYZ(section.Origin.X, section.Origin.Y, 0);
                    // Move distance between selected and offset. 
                    transaction = offsetWallMid - sectionOrigin;
                }
            }

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

        //*****************************viewFamilyType()*****************************
        public ViewFamilyType viewFamilyType(Document doc)
        {
            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault<ViewFamilyType>(x => ViewFamily.Section == x.ViewFamily);

            //ViewFamilyType vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
            //    .Where(z => z.Name == "Detail Views (Beam Top Detail Views)")
            //    .First() as ViewFamilyType;
            return vft;
        }
    }
}
