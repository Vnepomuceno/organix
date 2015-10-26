using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

[XmlRoot("GameState")]
[Serializable]
public class ClientState
{	
	[XmlArray("Processes"), XmlArrayItem("Process")]
	public List<Process> LocalProcesses;
	public Event ev;
	public Activity ac;
	public ComposedActivity cAc;
	public AdHocActivity ahAc;
	
	public ClientState()
	{
		LocalProcesses = new List<Process>();
	}
	
	public int GetNumPublishedProcesses()
	{
		int n = 0;
		
		foreach (Process process in LocalProcesses)
			if (process.published)
				n++;
		
		return n;
	}

	public Process GetProcess(int PID)
	{
		Process process = null;
		
		foreach (Process p in LocalProcesses)
			if (p.PID == PID)
			{
				process = p;
				break;
			}
		
		if (process == null)
			throw new InvalidOperationException("Process " + PID + " could not be found.");
		else
			return process;
	}
	
	public ProcessVersion GetProcessVersion(int PID, int PVID)
	{
		Process process = GetProcess(PID);
		ProcessVersion version = null;
		
		foreach (ProcessVersion pv in process.Versions)
			if (pv.PVID == PVID)
			{
				version = pv;
				break;
			}

			return version;
	}
	
	public Lane GetLane(int PID, int PVID, int PrID)
	{
		Lane lane = null;
		Process process = GetTargetProcess(PID, PVID);
		
		foreach (Lane l in process.Pool)
			if (l.PrID == PrID)
			{
				lane = l;
				break;
			}
		
		if (lane == null)
			throw new InvalidOperationException("Lane " + PrID + " could not be found.");
		else
			return lane;
	}
	
	public int GetLaneID(int PID, int PVID, int PrID)
	{
		Process process = GetTargetProcess(PID, PVID);
		int LaneID = -1;
		
		foreach (Lane lane in process.Pool)
			foreach (Primitive prim in lane.Elements)
				if (prim.PrID == PrID)
					LaneID = lane.PrID;
		
		return LaneID;
	}
	
	public Activity GetActivityWithPrID(int PID, int PVID, int LaneID, int PrID)
	{
		Lane lane = GetLane(PID, PVID, LaneID);
		Activity activity = null;
		
		foreach (Primitive prim in lane.Elements)
			if ((prim is Activity) && ((Activity)prim).PrID == PrID)
			{
				activity = prim as Activity;
				break;
			}
		
		if (activity == null)
			throw new InvalidOperationException("Activity with PrID " + PrID + " could not be found.");
		else
			return activity;
	}
	
	public Primitive GetSubPrimitive(int PID, int PVID, int LaneID, int ActPrID, int SPrID)
	{
		Activity activity = GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
		Primitive subPrimitive = null;
		
		if (activity is AdHocActivity)
		{
			AdHocActivity adHocActivity = activity as AdHocActivity;
			
			foreach (Primitive adHocPrim in adHocActivity.lane.Elements)
			{
				if (adHocPrim.PrID == SPrID)
				{
					subPrimitive = adHocPrim;
					break;
				}
			}
		}
		else if (activity is ComposedActivity)
		{
			ComposedActivity composedActivity = activity as ComposedActivity;
			
			foreach (Primitive compPrim in composedActivity.lane.Elements)
			{
				if (compPrim.PrID == SPrID)
				{
					subPrimitive = compPrim;
					break;
				}
			}
		}
		
		if (subPrimitive == null)
			throw new InvalidOperationException("Sub activity " + SPrID + " could not be found.");
		else
			return subPrimitive;
	}
	
	public Flow GetFlow(int PID, int PVID, int FlowID)
	{
		Flow flow = null;
		Process process = GetTargetProcess(PID, PVID);
		
		foreach (Flow f in process.Connections)
			if (f.PrID == FlowID)
			{
				flow = f;
				break;
			}
		
		if (flow == null)
			throw new InvalidOperationException("Flow " + FlowID + " could not be found.");
		else
			return flow;
	}
	
	public Flow GetSubFlow(int PID, int PVID, int LaneID, int ActPrID, int FlowID)
	{
		Activity activity = GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
		ComposedActivity composedActivity = activity as ComposedActivity;
		Flow subFlow = null;
		
		foreach (Flow flow in composedActivity.Connections)
			if (flow.PrID == FlowID)
			{
				subFlow = flow;
				break;
			}
		
		if (subFlow == null)
			throw new InvalidOperationException("Sub flow " + FlowID + " could not be found.");
		else
			return subFlow;
	}
	
	public Flow GetFlow(int PID, int PVID, int sourcePrID, int targetPrID)
	{
		Flow flow = null;
		Process process = GetTargetProcess(PID, PVID);
		
		foreach (Flow f in process.Connections)
			if (f.SourceID == sourcePrID && f.TargetID == targetPrID)
			{
				flow = f;
				break;
			}
		
		if (flow == null)
			throw new InvalidOperationException("Flow connecting primitives " + sourcePrID + " and " + targetPrID + " could not be found.");
		else
			return flow;
	}
	
	public Primitive GetPrimitive(int PID, int PVID, int PrID)
	{
		Primitive primitive = null;
		Process process = GetTargetProcess(PID, PVID);
		
		foreach (Lane lane in process.Pool)
			foreach (Primitive prim in lane.Elements)
				if (prim.PrID == PrID)
				{
					primitive = prim;
					break;
				}
		
		if (primitive == null)
			throw new InvalidOperationException("Primitive " + PrID + " could not be found.");
		else
			return primitive;
	}
		
	public void ChangeLane(int PID, int PVID, int PrID, int LaneID)
	{
		Process process = GetTargetProcess(PID, PVID);
		Lane newLane = GetLane(PID, PVID, LaneID);
		Primitive primitive = GetPrimitive(PID, PVID, PrID);
		
		foreach (Lane lane in process.Pool)
			foreach (Primitive prim in lane.Elements)
				if (prim.PrID == PrID)
				{
					lane.Elements.Remove(prim);
					break;
				}
		
		newLane.Elements.Add(primitive);
	}
	
	public void RemoveFlows(int PID, int PVID, int PrID)
	{
		List<Flow> newConnectionsList = new List<Flow>();
		Process process = GetTargetProcess(PID, PVID);
		
		foreach (Flow flow in process.Connections)
			if (flow.TargetID != PrID && flow.SourceID != PrID)
				newConnectionsList.Add(flow);
		
		process.Connections = newConnectionsList;
	}
	
	public void RemoveSubFlows(int PID, int PVID, int LaneID, int ActPrID, int SPrID)
	{
		List<Flow> newFlowList = new List<Flow>();
		ComposedActivity activity = GetActivityWithPrID(PID, PVID, LaneID, ActPrID) as ComposedActivity;
		
		foreach (Flow flow in activity.Connections) 
			if (flow.TargetID != SPrID && flow.SourceID != SPrID)
				newFlowList.Add(flow);
		
		activity.Connections = newFlowList;
	}
	
	public void Reset()
	{
		LocalProcesses.Clear();
		Process.IDCounter = 0;
		ProcessVersion.IDCounter = 0;
		Activity.IDCounter = 0;
	}
	
	
	public Process GetTargetProcess(int PID, int PVID)
	{
		Process process;
		
		if (PVID == -1) process = GetProcess(PID);
		else process = GetProcessVersion(PID, PVID);
		
		return process;
	}
	
}
