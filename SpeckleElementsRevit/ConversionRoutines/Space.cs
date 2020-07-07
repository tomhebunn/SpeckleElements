using Autodesk.Revit.DB;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleElementsClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO: need to check if space is placed in the model? Does it matter if it isn't??

namespace SpeckleElementsRevit
{
    public static partial class Conversions {
        // following the routine set up to look at room objects
        public static Space ToSpeckle(this Autodesk.Revit.DB.Mechanical.Space mySpace) {
            
            var speckleSpace = new Space();

            // get name and reference / number for space
            speckleSpace.spaceName = mySpace.get_Parameter(Autodesk.Revit.DB.SpatialElement.Name).AsString();
            speckleSpace.spaceNumber = mySpace.Number;

            //get space location 
            var locPt = ((Autodesk.Revit.DB.LocationPoint) mySpace.Location).Point;
            speckleSpace.spaceLocation = new SpecklePoint( locPt.X / Scale, locPt.Y / Scale, locPt.Z / Scale);

            (speckleSpace.Faces, speckleSpace.Vertices) = GetFaceVertexArrayFromElement( mySpace );

            var seg = mySpace.GetBoundarySegments( new Autodesk.Revit.DB.SpatialElementBoundaryOptions() );
            var myPolyCurve = new SpecklePolycurve() { Segments = new List<SpeckleCore.SpeckleObject>() };

            foreach(BoundarySegment segment in seg[0]) {

                var crv = segment.GetCurve();
                var converted = SpeckleCore.Converter.Serialise( crv );
                myPolyCurve.Segments.Add( converted as SpeckleObject );
            }

            speckleSpace.baseCurve = myPolyCurve;
            speckleSpace.parameters = GetElementParams( mySpace );
            // speckleSpace.typeParameters = GetElementTypeParams( mySpace );

            speckleSpace.ApplicationId = mySpace.UniqueId;
            speckleSpace.elementId = mySpace.Id.ToString();
            speckleSpace.GenerateHash();

            return speckleSpace;
        }
    }
}