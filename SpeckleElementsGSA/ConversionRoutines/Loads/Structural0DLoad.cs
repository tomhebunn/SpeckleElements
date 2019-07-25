﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleElementsClasses;

namespace SpeckleElementsGSA
{
  [GSAObject("LOAD_NODE.2", new string[] { "NODE.2", "AXIS" }, "loads", true, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSA0DLoad : IGSASpeckleContainer
  {
    public int Axis; // Store this temporarily to generate other loads

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DLoad();

    public void ParseGWACommand(GSAInterfacer GSA, List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      Structural0DLoad obj = new Structural0DLoad();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      int[] targetNodeRefs = GSA.ConvertGSAList(pieces[counter++], GSAEntity.NODE);

      if (nodes != null)
      {
        List<GSANode> targetNodes = nodes
            .Where(n => targetNodeRefs.Contains(n.GSAId)).ToList();

        obj.NodeRefs = targetNodes.Select(n => (string)n.Value.ApplicationId).ToList();
        this.SubGWACommand.AddRange(targetNodes.Select(n => n.GWACommand));

        foreach (GSANode n in targetNodes)
          n.ForceSend = true;
      }

      obj.LoadCaseRef = GSA.GetSID(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      string axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : Convert.ToInt32(axis);

      obj.Loading = new StructuralVectorSix(new double[6]);
      string direction = pieces[counter++].ToLower();
      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = Convert.ToDouble(pieces[counter++]);
          break;
        case "Y":
          obj.Loading.Value[1] = Convert.ToDouble(pieces[counter++]);
          break;
        case "Z":
          obj.Loading.Value[2] = Convert.ToDouble(pieces[counter++]);
          break;
        case "XX":
          obj.Loading.Value[3] = Convert.ToDouble(pieces[counter++]);
          break;
        case "YY":
          obj.Loading.Value[4] = Convert.ToDouble(pieces[counter++]);
          break;
        case "ZZ":
          obj.Loading.Value[5] = Convert.ToDouble(pieces[counter++]);
          break;
        default:
          // TODO: Error case maybe?
          break;
      }

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Structural0DLoad load = this.Value as Structural0DLoad;

      if (load.Loading == null)
        return;

      string keyword = typeof(GSA0DLoad).GetGSAKeyword();

      List<int> nodeRefs = GSA.Indexer.LookupIndices(typeof(GSANode), load.NodeRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      int loadCaseRef = 0;
      try
      {
        loadCaseRef = GSA.Indexer.LookupIndex(typeof(GSALoadCase), load.LoadCaseRef).Value;
      }
      catch { loadCaseRef = GSA.Indexer.ResolveIndex(typeof(GSALoadCase), load.LoadCaseRef); }

      string[] direction = new string[6] { "X", "Y", "Z", "X", "Y", "Z" };

      for (int i = 0; i < load.Loading.Value.Count(); i++)
      {
        List<string> ls = new List<string>();

        if (load.Loading.Value[i] == 0) continue;

        int index = GSA.Indexer.ResolveIndex(typeof(GSA0DLoad));

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        ls.Add(keyword + ":" + GSA.GenerateSID(load));
        ls.Add(load.Name == null || load.Name == "" ? " " : load.Name);
        ls.Add(string.Join(" ", nodeRefs));
        ls.Add(loadCaseRef.ToString());
        ls.Add("GLOBAL"); // Axis
        ls.Add(direction[i]);
        ls.Add(load.Loading.Value[i].ToString());

        GSA.RunGWACommand(string.Join("\t", ls));
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this Structural0DLoad load)
    {
      new GSA0DLoad() { Value = load }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA0DLoad dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA0DLoad)))
        GSASenderObjects[typeof(GSA0DLoad)] = new List<object>();

      List<GSA0DLoad> loads = new List<GSA0DLoad>();

      List<GSANode> nodes = GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSA0DLoad).GetGSAKeyword();
      string[] subKeywords = typeof(GSA0DLoad).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSA0DLoad)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSA0DLoad)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        List<GSA0DLoad> loadSubList = new List<GSA0DLoad>();

        // Placeholder load object to get list of nodes and load values
        // Need to transform to axis so one load definition may be transformed to many
        GSA0DLoad initLoad = new GSA0DLoad() { GWACommand = p };
        initLoad.ParseGWACommand(GSA, nodes);

        // Raise node flag to make sure it gets sent
        foreach (GSANode n in nodes.Where(n => initLoad.Value.NodeRefs.Contains(n.Value.ApplicationId)))
          n.ForceSend = true;

        // Create load for each node applied
        foreach (string nRef in initLoad.Value.NodeRefs)
        {
          GSA0DLoad load = new GSA0DLoad();
          load.GWACommand = initLoad.GWACommand;
          load.SubGWACommand = new List<string>(initLoad.SubGWACommand);
          load.Value.Name = initLoad.Value.Name;
          load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

          // Transform load to defined axis
          GSANode node = nodes.Where(n => (n.Value.ApplicationId == nRef)).First();
          string gwaRecord = null;
          StructuralAxis loadAxis = GSA.Parse0DAxis(initLoad.Axis, out gwaRecord, node.Value.Value.ToArray());
          load.Value.Loading = initLoad.Value.Loading;
          load.Value.Loading.TransformOntoAxis(loadAxis);

          // If the loading already exists, add node ref to list
          GSA0DLoad match = loadSubList.Count() > 0 ? loadSubList.Where(l => (l.Value.Loading.Value as List<double>).SequenceEqual(load.Value.Loading.Value as List<double>)).First() : null;
          if (match != null)
          {
            match.Value.NodeRefs.Add(nRef);
            if (gwaRecord != null)
              match.SubGWACommand.Add(gwaRecord);
          }
          else
          {
            load.Value.NodeRefs = new List<string>() { nRef };
            if (gwaRecord != null)
              load.SubGWACommand.Add(gwaRecord);
            loadSubList.Add(load);
          }
        }

        loads.AddRange(loadSubList);
      }

      GSASenderObjects[typeof(GSA0DLoad)].AddRange(loads);

      if (loads.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
