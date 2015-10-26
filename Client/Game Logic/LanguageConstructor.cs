using System;
using UnityEngine;
using System.Collections.Generic;

public static class LanguageConstructor
{
	public static void PlayerInTheGame()
	{
		if (!Painter.Manager.GameOn)
		{
			Painter.Manager.Notifications.Items.Add(new Notification(Notification.Type.Exception,
				"Your time to continue playing is up."));
			return;
		}
	}
	
	public static void AddProcess(string author, string name, string description)
	{
		PlayerInTheGame();
		Painter.ClientManager.networkView.RPC("NewProcess", RPCMode.Server, author, name, description);
	}
	
	public static void AddLane(int PID, int PVID, string participant)
	{
		Painter.ClientManager.networkView.RPC("NewLane", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, participant);
	}
	
	public static void EditLane(int PID, int PVID, int LaneID, string participant)
	{
		Painter.ClientManager.networkView.RPC("EditLane", RPCMode.Server, PID, PVID, LaneID, participant);
	}
	
	public static void AddStartEvent(int PID, int PVID, int LaneID)
	{
		Painter.ClientManager.networkView.RPC("NewEvent", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, -1, "Start", (float)-1, (float)-1);
	}
	
	public static void AddEndEvent(int PID, int PVID, int LaneID)
	{
		Painter.ClientManager.networkView.RPC("NewEvent", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, -1, "End", (float)-1, (float)-1);
	}
	
	public static void AddMerge(int PID, int PVID, int LaneID)
	{
		Painter.ClientManager.networkView.RPC("NewEvent", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, -1, "Merge", (float)-1, (float)-1);
	}
	
	public static void AddActivity(int PID, int PVID, int LaneID, string name)
	{
		Painter.ClientManager.networkView.RPC("NewActivity", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, -1, name, (float)-1, (float)-1);
	}
	
	public static void AddComposedActivity(int PID, int PVID, int LaneID, string name)
	{
		Painter.ClientManager.networkView.RPC("NewComposedActivity", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, -1, name, (float)-1, (float)-1);
	}
	
	public static void AddAdHocActivity(int PID, int PVID, int LaneID)
	{
		Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
		
		Painter.ClientManager.networkView.RPC("NewAdHocActivity", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, -1, "", (float)-1, (float)-1);
	}
	
	public static void AddAdHocSubActivity(int PID, int PVID, int LaneID, int ActPrID)
	{
		Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
		
		Painter.ClientManager.networkView.RPC("AddAdHocSubActivity", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, ActPrID, -1, "");
	}
	
	public static void AddEventComposed(int PID, int PVID, int LaneID, int ActPrID, string type)
	{
		Painter.ClientManager.networkView.RPC("AddEventComposed", RPCMode.Others,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, ActPrID, -1, type, (float)-1, (float)-1);
	}
	
	public static void AddComposedSubActivity(int PID, int PVID, int LaneID, int ActPrID, string name)
	{
		Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
		
		Painter.ClientManager.networkView.RPC("AddComposedSubActivity", RPCMode.Server,
			Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, ActPrID, -1, name, (float)-1, (float)-1);
	}
	
	public static void AddConnectionComposed(int PID, int PVID, int LaneID, int ActPrID, int sourceID, int targetID, string condition)
	{
		Primitive source = Painter.Manager.GameState.GetSubPrimitive(PID, PVID, LaneID, ActPrID, sourceID);
		
		if (sourceID == targetID && source is Event)
			Painter.Manager.Notifications.Items.Add(
				new Notification(Notification.Type.Exception, "You cannot create a loop cycle in an event."));
		else
			Painter.ClientManager.networkView.RPC("AddConnectionComposed", RPCMode.Others,
				Painter.Manager.CurrentPlayer.Username, PID, PVID, LaneID, ActPrID, -1, sourceID, targetID, condition);
	}
	
	public static void AddConnection(int PID, int PVID, int sourcePrID, int targetPrID, string condition, string type)
	{
		try
		{
			Process process;
			if (PVID == -1)
				process = Painter.Manager.GameState.GetProcess(PID);
			else
				process = Painter.Manager.GameState.GetProcessVersion(PID, PVID);
			
			Primitive source = Painter.Manager.GameState.GetPrimitive(PID, PVID, sourcePrID);
			
			foreach (Flow flow in process.Connections) {
				if (flow.SourceID == sourcePrID && flow.TargetID == targetPrID)
				{
					Painter.Manager.Notifications.Items.Add(
						new Notification(Notification.Type.Exception, "The flow you are trying to create already exists."));
					return;
				}
				else if (flow.SourceID == targetPrID && flow.TargetID == sourcePrID)
				{
					Painter.Manager.Notifications.Items.Add(
						new Notification(Notification.Type.Exception, "A reverse flow between the two primitives already exists."));
					return;
				}
			}
			
			if (sourcePrID == targetPrID && source is Event)
				Painter.Manager.Notifications.Items.Add(
					new Notification(Notification.Type.Exception, "You cannot create a loop cycle in an event."));
			else if (sourcePrID == targetPrID && type.Equals("Information"))
				Painter.Manager.Notifications.Items.Add(
					new Notification(Notification.Type.Exception, "You cannot create a loop cycle with information flows."));
			else
				Painter.ClientManager.networkView.RPC("NewConnection", RPCMode.Server,
					Painter.Manager.CurrentPlayer.Username, PID, PVID, sourcePrID, targetPrID, condition, type);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void EditProcess(int PID, string name, string description)
	{
		try
		{
			Painter.Manager.GameState.GetProcess(PID);
			Painter.ClientManager.networkView.RPC("EditProcess", RPCMode.Server, PID, name, description);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void EditConnection(int PID, int PVID, int sourcePrID, int targetPrID, string condition)
	{
		try
		{
			Painter.Manager.GameState.GetPrimitive(PID, PVID, sourcePrID);
			Painter.Manager.GameState.GetPrimitive(PID, PVID, targetPrID);
			Painter.ClientManager.networkView.RPC("EditConnection", RPCMode.Server, PID, PVID, sourcePrID, targetPrID, condition);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void EditSubFlow(int PID, int PVID, int LaneID, int ActPrID, int FlowID, string condition)
	{
		try
		{
			Painter.ClientManager.networkView.RPC("EditSubFlow", RPCMode.Server, PID, PVID, LaneID, ActPrID, FlowID, condition);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void EditActivity(int PID, int PVID, int LaneID, int PrID, string name)
	{
		Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, PrID);
		Painter.ClientManager.networkView.RPC("EditActivity", RPCMode.Server, PID, PVID, LaneID, PrID, name);
	}
	
	public static void EditSubPrimitive(int PID, int PVID, int LaneID, int ActPrID, int SPrID, string name)
	{
		Painter.Manager.GameState.GetSubPrimitive(PID, PVID, LaneID, ActPrID, SPrID);
		Painter.ClientManager.networkView.RPC("EditSubActivity", RPCMode.Server, PID, PVID, LaneID, ActPrID, SPrID, name);
	}
		
	public static void RemovePrimitive(int PID, int PVID, int LaneID, int PrID)
	{
		try
		{
			Painter.Manager.GameState.GetPrimitive(PID, PVID, PrID);
			Painter.ClientManager.networkView.RPC("RemovePrimitive", RPCMode.Server, PID, PVID, LaneID, PrID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void RemoveSubPrimitive(int PID, int PVID, int LaneID, int ActPrID, int SPrID)
	{
		try
		{
			Painter.Manager.GameState.GetSubPrimitive(PID, PVID, LaneID, ActPrID, SPrID);
			Painter.ClientManager.networkView.RPC("RemoveSubPrimitive", RPCMode.Server, PID, PVID, LaneID, ActPrID, SPrID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void RemoveFlow(int PID, int PVID, int FlowID)
	{
		try
		{
			Painter.Manager.GameState.GetFlow(PID, PVID, FlowID);
			Painter.ClientManager.networkView.RPC("RemoveFlow", RPCMode.Server, PID, PVID, FlowID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void RemoveSubFlow(int PID, int PVID, int LaneID, int ActPrID, int FlowID)
	{
		try
		{
			Painter.Manager.GameState.GetSubFlow(PID, PVID, LaneID, ActPrID, FlowID);
			Painter.ClientManager.networkView.RPC("RemoveSubFlow", RPCMode.Server, PID, PVID, LaneID, ActPrID, FlowID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void RemoveLane(int PID, int PVID, int LaneID)
	{
		try
		{
			Lane lane = Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
			
			foreach (Primitive prim in lane.Elements)
				if (prim is Event
					&& (((Event)prim).categ.Equals(Event.Categ.Start) ||
						((Event)prim).categ.Equals(Event.Categ.End)))
				{
					Painter.Manager.Notifications.Items.Add(
						new Notification(Notification.Type.Exception, "You cannot remove lanes with start or end events."));
					return;
				}
			
			if (Painter.Manager.CurrentLane.PrID == LaneID)
				Painter.Manager.CurrentLane = Painter.Manager.CurrentProcess.Pool[0];
			
			Painter.ClientManager.networkView.RPC("RemoveLane", RPCMode.Server, PID, PVID, LaneID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void RemoveProcess(int PID, int PVID)
	{
		try
		{
			Painter.ClientManager.networkView.RPC("RemoveProcess", RPCMode.Server, PID, PVID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	public static void ChangeLane(int PID, int PVID, int PrID, int LaneID)
	{
		try
		{
			Painter.ClientManager.networkView.RPC("ChangeLane", RPCMode.Server, PID, PVID, PrID, LaneID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
}
