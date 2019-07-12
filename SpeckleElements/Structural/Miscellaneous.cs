﻿extern alias SpeckleNewtonsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SNJ = SpeckleNewtonsoft.Newtonsoft.Json;

namespace SpeckleElements
{
  [Serializable]
  public partial class StructuralAssembly : SpeckleLine, IStructural
  {
    public override string Type { get => base.Type + "/StructuralAssembly"; }

    [SNJ.JsonIgnore]
    private Dictionary<string, object> StructuralProperties
    {
      get
      {
        if (base.Properties == null)
          base.Properties = new Dictionary<string, object>();

        if (!base.Properties.ContainsKey("structural"))
          base.Properties["structural"] = new Dictionary<string, object>();

        return base.Properties["structural"] as Dictionary<string, object>;

      }
      set
      {
        if (base.Properties == null)
          base.Properties = new Dictionary<string, object>();

        base.Properties["structural"] = value;
      }
    }

    [SNJ.JsonIgnore]
    public int NumPoints
    {
      get
      {
        if (StructuralProperties.ContainsKey("numPoints") && int.TryParse(StructuralProperties["numPoints"].ToString(), out int numPoints))
        {
          return numPoints;
        }
        return 0;
      }
      set => StructuralProperties["numPoints"] = value;
    }

    /// <summary>Base SpeckleLine.</summary>
    [SNJ.JsonIgnore]
    public SpeckleLine BaseLine
    {
      get => this as SpeckleLine;
      set
      {
        this.Value = value.Value;
        this.Domain = value.Domain;
      }
    }

    [SNJ.JsonIgnore]
    public SpecklePoint OrientationPoint
    {
      get => StructuralProperties.ContainsKey("orientationPoint") ? (StructuralProperties["orientationPoint"] as SpecklePoint) : null;
      set => StructuralProperties["orientationPoint"] = value;
    }

    [SNJ.JsonIgnore]
    public double Width
    {
      get => (StructuralProperties.ContainsKey("width") && double.TryParse(StructuralProperties["width"].ToString(), out double width)) ? width : 0;
      set => StructuralProperties["width"] = value;
    }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [SNJ.JsonIgnore]
    public List<string> ElementRefs
    {
      get
      {
        if (StructuralProperties.ContainsKey("elementRefs"))
        {
          try
          {
            try
            {
              return (List<string>)StructuralProperties["elementRefs"];
            }
            catch
            {
              this.ElementRefs = ((List<object>)StructuralProperties["elementRefs"]).Select(x => Convert.ToString(x)).ToList();
              return this.ElementRefs;
            }
          }
          catch
          { return null; }
        }
        else
          return null;
      }
      set => StructuralProperties["elementRefs"] = value;
    }
  }

  [Serializable]
  public partial class StructuralConstructionStage : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralConstructionStage"; }

    /// <summary>Application ID of members to include in the stage of the construction sequence.</summary>
    [SNJ.JsonProperty("elementRefs", Required = SNJ.Required.Default, NullValueHandling = SNJ.NullValueHandling.Ignore)]
    public List<string> ElementRefs { get; set; }

    /// <summary>Number of days in the stage</summary>
    [SNJ.JsonProperty("stageDays", Required = SNJ.Required.Default, NullValueHandling = SNJ.NullValueHandling.Ignore)]
    public int StageDays { get; set; }
  }

  [Serializable]
  public partial class StructuralStagedNodalRestraints : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralStagedNodalRestraints"; }

    /// <summary>A list of the X, Y, Z, Rx, Ry, and Rz restraints.</summary>
    [SNJ.JsonProperty("restraint", Required = SNJ.Required.Default, NullValueHandling = SNJ.NullValueHandling.Ignore)]
    public StructuralVectorBoolSix Restraint { get; set; }

    /// <summary>Application IDs of StructuralNodes to apply restrain.</summary>
    [SNJ.JsonProperty("nodeRefs", Required = SNJ.Required.Default, NullValueHandling = SNJ.NullValueHandling.Ignore)]
    public List<string> NodeRefs { get; set; }

    /// <summary>Application IDs of StructuralConstructionStages to apply restraints on</summary>
    [SNJ.JsonProperty("constructionStageRefs", Required = SNJ.Required.Default, NullValueHandling = SNJ.NullValueHandling.Ignore)]
    public List<string> ConstructionStageRefs { get; set; }
  }
}