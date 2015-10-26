using System;
using UnityEngine;

public static class Mechanics
{
	public static void RepositionPrimitive(int PID, int PVID, int PrID, float x, float y)
	{
		Painter.ClientManager.networkView.RPC("RepositionPrimitive", RPCMode.Server, PID, PVID, PrID, x, y);
	}
	
	public static void RepositionSubPrimitive(int PID, int PVID, int PrID, int RPrID, float x, float y)
	{
		Painter.ClientManager.networkView.RPC("RepositionSubPrimitive", RPCMode.Server, PID, PVID, PrID, RPrID, x, y);
	}
	
	public static void VoteQualityProcess(int PID, bool type, string username)
	{
		Painter.ClientManager.networkView.RPC("VoteQualityProcess", RPCMode.Server, PID, type, username);
	}
	
	public static void VoteQualityVersion(string username, int PID, int PVID, bool type)
	{
		Painter.ClientManager.networkView.RPC("VoteQualityVersion", RPCMode.Server, username, PID, PVID, type);
	}
	
	public static void MarkAsDuplicated(int PID, int PVID, int originalPID, int originalPVID, string username)
	{
		Process duplicate = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
		Process original = Painter.Manager.GameState.GetTargetProcess(originalPID, originalPVID);
		
		if (duplicate.timeStamp < original.timeStamp)
			PaintPanels.markDupMessage = "<size=14><b>WARNING</b>: This process cannot be marked as a duplicate \n because it was created first than the target process.</size>";
		else
		{
			PaintPanels.markDupMessage = "";
			Painter.ClientManager.networkView.RPC("MarkAsDuplicated", RPCMode.Server, PID, PVID, originalPID, originalPVID, username);
		}
	}
	
	public static void VoteDuplicationProcess(int PID, int PVID, bool type, string username)
	{
		Painter.ClientManager.networkView.RPC("VoteDuplicationProcess", RPCMode.Server, PID, PVID, type, username);
	}
	
	public static void PublishProcess(string username, int PID, int PVID)
	{
		Process process;
		process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
		
		if (process.Validate())
		{
			Painter.Manager.GameState.LocalProcesses.Remove(process);
			Painter.Manager.CurrentScreen = GameManager.GameScreen.Home;
			Painter.ClientManager.networkView.RPC("PublishProcess", RPCMode.Server, username, PID, PVID);
		}
	}
	
	public static void NewProcessVersion(int PID, string author)
	{
		Painter.ClientManager.networkView.RPC("NewProcessVersion", RPCMode.Server, PID, author);
	}
	
	public static void SignalGameTimeout(string username)
	{
		Painter.ClientManager.networkView.RPC("SignalGameTimeout", RPCMode.Server, username);
	}
	
}

