﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleElements;

namespace SpeckleElementsRevit
{
  public static partial class Conversions
  {
    //TODO
    public static Element ToNative(this Structural2DLoadPanel myLoad)
    {
      return null;
    }

    public static List<SpeckleObject> ToSpeckle(this Autodesk.Revit.DB.Structure.AreaLoad myAreaLoad)
    {
      List<XYZ> points = new List<XYZ>();
      
      List<double[]> polylines = new List<double[]>();
      
      var loops = myAreaLoad.GetLoops();
      foreach (CurveLoop loop in loops)
      {
        List<double> coor = new List<double>();
        foreach (Curve curve in loop)
        {
          var point = (SpeckleCoreGeometryClasses.SpecklePoint)SpeckleCore.Converter.Serialise(curve.GetEndPoint(0));
          coor.AddRange(point.Value);
        }

        polylines.Add(coor.ToArray());
      }

      var forces = new StructuralVectorThree(new double[3]);
      
      forces.Value[0] = myAreaLoad.ForceVector1.X;
      forces.Value[1] = myAreaLoad.ForceVector1.Y;
      forces.Value[2] = myAreaLoad.ForceVector1.Z;

      if (myAreaLoad.OrientTo == LoadOrientTo.HostLocalCoordinateSystem)
      {
        var hostTransform = myAreaLoad.HostElement.GetLocalCoordinateSystem();

        XYZ b0 = hostTransform.get_Basis(0);
        XYZ b1 = hostTransform.get_Basis(1);
        XYZ b2 = hostTransform.get_Basis(2);

        double fx = forces.Value[0] * b0.X + forces.Value[1] * b1.X + forces.Value[2] * b2.X;
        double fy = forces.Value[0] * b0.Y + forces.Value[1] * b1.Y + forces.Value[2] * b2.Y;
        double fz = forces.Value[0] * b0.Z + forces.Value[1] * b1.Z + forces.Value[2] * b2.Z;

        forces = new StructuralVectorThree(new double[] { fx, fy, fz });
      }
      else if (myAreaLoad.OrientTo == LoadOrientTo.WorkPlane)
      {
        var workPlane = ((Autodesk.Revit.DB.SketchPlane)Doc.GetElement(myAreaLoad.WorkPlaneId)).GetPlane();
        
        XYZ b0 = workPlane.XVec;
        XYZ b1 = workPlane.YVec;
        XYZ b2 = workPlane.Normal;

        double fx = forces.Value[0] * b0.X + forces.Value[1] * b1.X + forces.Value[2] * b2.X;
        double fy = forces.Value[0] * b0.Y + forces.Value[1] * b1.Y + forces.Value[2] * b2.Y;
        double fz = forces.Value[0] * b0.Z + forces.Value[1] * b1.Z + forces.Value[2] * b2.Z;

        forces = new StructuralVectorThree(new double[] { fx, fy, fz });
      }
      
      var myLoadCase = new StructuralLoadCase();
      myLoadCase.Name = myAreaLoad.LoadCaseName;
      myLoadCase.StructuralId = myLoadCase.Name;
      switch (myAreaLoad.LoadCategoryName)
      {
        case "Dead Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Dead;
          break;
        case "Live Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Live;
          break;
        case "Seismic Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Earthquake;
          break;
        case "Snow Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Snow;
          break;
        case "Wind Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Wind;
          break;
        default:
          myLoadCase.CaseType = StructuralLoadCaseType.Generic;
          break;
      }

      List<SpeckleObject> myLoads = new List<SpeckleObject>();

      foreach (double[] vals in polylines)
      {
        var myLoad = new Structural2DLoadPanel();
        myLoad.Name = myAreaLoad.Name;
        myLoad.Value = vals.ToList();
        myLoad.Loading = forces;
        myLoad.LoadCaseRef = myLoadCase.StructuralId;

        myLoads.Add(myLoad);
      }
      
      return myLoads.Concat(new List<SpeckleObject>() { myLoadCase }).ToList();
    }
  }
}