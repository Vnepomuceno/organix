using System;
using UnityEngine;
using System.Collections.Generic;

public class Client : MonoBehaviour
{	
	public bool LoggedIn;
	public string IPaddress = "127.0.0.1";
	public int Port = 25000;
	
	public string Username, Password, LoginStatus;
	public string AttemptedLogIn = "";
	
	private string[] IOExceptions =
	{
		/*  0 */ "Cannot create new process.",
		/*  1 */ "Cannot add lane. Process not found.",
		/*  2 */ "Cannot edit lane. Proocess not found.",
		/*  3 */ "Cannot add event. Process not found.",
		/*  4 */ "Cannot add activity. Process not found.",
		/*  5 */ "Cannot add composed activity. Process not found.",
		/*  6 */ "Cannot add ad-hoc activity. Process not found.",
		/*  7 */ "Cannot add work activity to ad-hoc activity. Ad-hoc activity not found.",
		/*  8 */ "Cannot add work activity to composed activity. Composed activity not found.",
		/*  9 */ "Cannot add event to composed activity. Composed activity not found.",
		/* 10 */ "Cannot add flow between subprimitives. Subprimitives not found.",
		/* 11 */ "Cannot add flow between primitives. Primitives not found.",
		/* 12 */ "Cannot edit process. Process not found.",
		/* 13 */ "Cannot edit flow between primitives. Primitives not found.",
		/* 14 */ "Cannot edit sub flow from composed activity.",
		/* 15 */ "Cannot edit activity. Activity not found.",
		/* 16 */ "Cannot edit sub activity. Sub activity not found.",
		/* 17 */ "Cannot remove primitive. Primitive not found.",
		/* 18 */ "Cannot remove sub primitive. Sub primitive not found.",
		/* 19 */ "Cannot remove flow. Flow not found.",
		/* 20 */ "Cannot remove sub flow. Sub flow not found.",
		/* 21 */ "Cannot remove lane. Lane not found",
		/* 22 */ "Cannot remove process. Process not found",
		/* 23 */ "Cannot change lane of primitive.",
		/* 24 */ "Cannot vote for process.",
		/* 25 */ "Cannot mark process as duplicated.",
		/* 26 */ "Cannot vote for process regarding duplication.",
		/* 27 */ "Cannot publish process.",
		/* 28 */ "Action could not be performed."
	};
	
	public void Start()
	{
		Username = Password = LoginStatus = "";
		Network.Connect(IPaddress, Port);
	}
	
	public void OnApplicationQuit()
	{
		networkView.RPC("LogoutAck", RPCMode.Server, Painter.Manager.CurrentPlayer == null ? "" : Painter.Manager.CurrentPlayer.Username);
	}
	
	private void ValidateAcknowledge(string status, string ioe)
	{
		if (status.Equals("Success"))
			return;
		else if (status.Equals("Failure"))
			throw new InvalidOperationException(ioe);
		else
			throw new Exception("Unknown RPC acknowledge.");
	}
	
	#pragma warning disable
	public bool ProcessExists(int PID, int PVID)
	{
		try
		{
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			return true;
		}
		catch (InvalidOperationException ioe)
		{
			return false;
		}
	}
	#pragma warning restore
	
	public bool DirectedToPlayer(string sender, int PID, int PVID, bool update, bool published)
	{
		if (update && !Painter.UpdateMode)
			return false;
		if (published && !Painter.Manager.CurrentPlayer.Username.Equals(sender))
			return true;
		else if (Painter.Manager.CurrentPlayer.Username.Equals(sender))
			return true;
		else
			return false;
	}
	
	#region RPC SENDER
	
	#region Language Constructor
	[RPC] public void Login(string username, string password) {}
	[RPC] public void LogoutAck(string username) {}
	[RPC] public void NewProcess(string author, string name, string description) {}
	[RPC] public void NewProcessVersion(int PID, string author) {}
	[RPC] public void NewLane(string player, int PID, int PVID, string participant) {}
	[RPC] public void EditLane(int PID, int PVID, int LaneID, string participant) {}
	[RPC] public void NewEvent(string player, int PID, int PVID, int LaneID, int PrID, string category, float x, float y) {}
	[RPC] public void NewActivity(string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y) {}
	[RPC] public void NewComposedActivity(string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y) {}
	[RPC] public void NewAdHocActivity(string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y) {}
	[RPC] public void AddAdHocSubActivity(string player, int PID, int PVID, int LaneID, int ActPrID, int PrID, string name) {}
	[RPC] public void AddComposedSubActivity(string player, int PID, int PVID, int LaneID, int ActPrID, int PrID, string name, float x, float y) {}
	[RPC] public void AddEventComposed(string player, int PID, int PVID, int LaneID, int ActPrID, int PrID, string type, float x, float y) {}
	[RPC] public void AddConnectionComposed(string player, int PID, int PVID, int LaneID,int ActPrID, int PrID, int sourceID, int targetID, string condition) {}
	[RPC] public void NewConnection(string player, int PID, int PVID, int sourcePrID, int targetPrID, string condition, string type) {}
	[RPC] public void EditProcess(int PID, string name, string description) {}
	[RPC] public void EditConnection(int PID, int PVID, int sourcePrID, int targetPrID, string condition) {}
	[RPC] public void EditSubFlow(int PID, int PVID, int LaneID, int ActPrID, int FlowID, string condition) {}
	[RPC] public void EditActivity(int PID, int PVID, int LaneID, int PrID, string name) {}
	[RPC] public void EditSubActivity(int PID, int PVID, int LaneID, int ActPrID, int SPrID, string name) {}
	[RPC] public void RemovePrimitive(int PID, int PVID, int LaneID, int PrID) {}
	[RPC] public void RemoveSubPrimitive(int PID, int PVID, int LaneID, int ActPrID, int SPrID) {}
	[RPC] public void RemoveFlow(int PID, int PVID, int FlowID) {}
	[RPC] public void RemoveSubFlow(int PID, int PVID, int LaneID, int ActPrID, int FlowID) {}
	[RPC] public void RemoveLane(int PID, int PVID, int LaneID) {}
	[RPC] public void RemoveProcess(int PID, int PVID) {}
	[RPC] public void ChangeLane(int PID, int PVID, int PrID, int LaneID) {}
	[RPC] public void RepositionPrimitive(int PID, int PVID, int PrID, float x, float y) {}
	[RPC] public void RepositionSubPrimitive(int PID, int PVID, int ActPrID, int PrID, float x, float y) {}
	#endregion

	#region Game Mechanics
	[RPC] public void VoteQualityProcess(int PID, bool vote, string username) {}
	[RPC] public void VoteQualityVersion(string username, int PID, int PVID, bool vote) {}
	[RPC] public void MarkAsDuplicated(int PID, int PVID, int originalPID, int originalPVID, string username) {}
	[RPC] public void VoteDuplicationProcess(int PID, int PVID, bool vote, string username) {}
	[RPC] public void PublishProcess(string username, int PID, int PVID) {}
	[RPC] public void SignalGameTimeout(string username) {}
	#endregion

	#region Data Consistency
	[RPC] public void LoadLocalProcesses(string player) {}
	[RPC] public void LoadFromXml(string player, int passNumber, int numberPasses, string XmlFraction) {}
	#endregion

	#endregion


	#region RPC RECEIVER
	
	#region Language Constructor
	[RPC]
	public void LoginAck(string status, string username, float gameLength, string processName)
	{
		if (username.Equals(AttemptedLogIn))
		{
			if (status.Equals("Success"))
			{
				LoggedIn = true;
				Painter.Manager.GameLength = gameLength;
				Painter.Manager.ToElicitProcessName = processName;
				Painter.Manager.CurrentPlayer = new Player(Username, Password);
				Painter.Manager.CurrentPlayer.Username = Username;
				Painter.Manager.CurrentPlayer.Password = Password;
				Painter.Manager.CurrentScreen = GameManager.GameScreen.Home;
				Painter.Manager.GameOn = true;
				
				Painter.UpdateMode = true;
				Painter.Manager.GameState.Reset();
				networkView.RPC("LoadLocalProcesses", RPCMode.Server, Painter.Manager.CurrentPlayer.Username);
			}
			else if (status.Equals("WrongPass"))
				LoginStatus = "Wrong password.";
			else if (status.Equals("Unregistered"))
				LoginStatus = "User not registered.";
			else if (status.Equals("AlreadyLogged"))
				LoginStatus = "User already logged in.";
			else if (status.Equals("ExceededNumberPlayers"))
				LoginStatus = "Number of logged players exceeded.";
			else
				throw new Exception("Unknown login acknowledge.");
		}
	}
	
	[RPC]
	public void NewProcessAck(string status, int PID, string author, string name, string description,
		bool markedDuplication, string markAuthor, int dupPID, int dupPVID, int score,
		bool published, bool update)
	{
		ValidateAcknowledge(status, IOExceptions[0]);
		if (update && !Painter.UpdateMode)
			return;
		
		if (Painter.Manager.CurrentPlayer.Username.Equals(author) || published)
		{
			Process p = new Process(PID, name, description);
			p.Author = author;
			p.markedDuplication = markedDuplication;
			p.markAuthor = markAuthor;
			p.duplicationPID = dupPID;
			p.duplicationPVID = dupPVID;
			p.score = score;
			p.published = published;
			Painter.Manager.GameState.LocalProcesses.Add(p);
			Painter.Manager.CurrentProcess = p;
			if (Painter.Manager.CurrentPlayer.Username.Equals(author))
			{
				Painter.Manager.CurrentPlayer.Score += p.score;
				if (published)
					Painter.Manager.CurrentPlayer.NumPubProcesses++;
				else
					Painter.Manager.CurrentPlayer.NumDraftProcesses++;
			}
			
			if (!update)
				Painter.Manager.CurrentScreen = GameManager.GameScreen.ProcessCreation;
			
			if (!Painter.UpdateMode)
			{
				Painter.ProcessName = "";
				PaintPanels.processNameWindow = true;
			}
			
			string participant = "Participant";
			if (!Painter.UpdateMode && p.Pool.Count == 0)
				LanguageConstructor.AddLane(PID, -1, participant);
		}
	}
	
	[RPC]
	public void NewProcessVersionAck(string status, int PID, int PVID, string author,
		bool markedDuplication, string markAuthor, int score, bool published, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[0]);
			if (!ProcessExists(PID, -1) || (update && !Painter.UpdateMode))
				return;
			
			Process process = Painter.Manager.GameState.GetProcess(PID);
			ProcessVersion version = new ProcessVersion(PVID, process, author);
			
			version.score = score;
			version.published = published;
			version.markedDuplication = markedDuplication;
			version.markAuthor = markAuthor;
			process.Versions.Add(version);
			Painter.Manager.CurrentVersion = version;
			if (Painter.Manager.CurrentPlayer.Username.Equals(author) && published)
				Painter.Manager.CurrentPlayer.NumPubVersions++;
			else if (Painter.Manager.CurrentPlayer.Username.Equals(author))
				Painter.Manager.CurrentPlayer.NumDraftVersions++;
			
			if (!update)
				Painter.Manager.CurrentScreen = GameManager.GameScreen.ProcessVersionCreate;
			
			if (!Painter.UpdateMode && process.Pool.Count == 0)
				LanguageConstructor.AddLane(PID, -1, "Participant");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void NewLaneAck(string status, string player, int PID, int PVID, int PrID,
		string participant, float x, float y, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[1]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			Lane lane;
							
			if (participant.Equals(""))
				lane = new Lane(PrID);
			else
				lane = new Lane(PrID, participant);
			lane.x = x;
			lane.y = y;
			
			process.Pool.Add(lane);
			
			if (!Painter.UpdateMode && process.Pool.Count == 1)
			{
				LanguageConstructor.AddStartEvent(PID, PVID, PrID);
				LanguageConstructor.AddEndEvent(PID, PVID, PrID);
			}
			
			Painter.Manager.CurrentLane = lane;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void EditLaneAck(string status, int PID, int PVID, int LaneID, string participant)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[2]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Lane lane = Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
			lane.Participant = participant;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void NewEventAck(string status, string player, int PID, int PVID, int LaneID, int PrID,
		string category, float x, float y, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[3]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Lane lane = Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
			Event ev = null;
			
			if (category.Equals("Start"))
				ev = new Event(PrID, Event.Categ.Start, x, y);
			else if (category.Equals("End"))
				ev = new Event(PrID, Event.Categ.End, x, y);
			else if (category.Equals("Merge"))
				ev = new Event(PrID, Event.Categ.Merge, x, y);
			
			lane.Elements.Add(ev);
			Painter.Manager.InspectorPrimitive = ev;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void NewActivityAck(string status, string player, int PID, int PVID, int LaneID,
		int PrID, string name, float x, float y, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[4]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Lane lane = Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
			Activity activity = new Activity(PrID, name, x, y);
			
			lane.Elements.Add(activity);
			Painter.activityName = activity.Name;
			Painter.Manager.InspectorPrimitive = activity;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void NewComposedActivityAck(string status, string player, int PID, int PVID, int LaneID, int PrID, string name,
		float x, float y, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[5]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Lane lane = Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
			ComposedActivity activity = new ComposedActivity(PrID, name, x, y);
			
			lane.Elements.Add(activity);
			Painter.Manager.InspectorPrimitive = activity;
			
			if (!Painter.UpdateMode && activity.lane.Elements.Count < 2)
			{
				LanguageConstructor.AddEventComposed(PID, PVID, LaneID, PrID, "Start");
				LanguageConstructor.AddEventComposed(PID, PVID, LaneID, PrID, "End");
			}
		}
		catch (InvalidOperationException ioe) 
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void NewAdHocActivityAck(string status, string player, int PID, int PVID, int LaneID, int PrID, string name,
		float x, float y, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[6]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Lane lane = Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
			AdHocActivity activity = new AdHocActivity(PrID, name, x, y);

			lane.Elements.Add(activity);
			Painter.Manager.InspectorPrimitive = activity;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void AddAdHocSubActivityAck(string status, string player, int PID, int PVID, int LaneID,
		int ActPrID, int NPrID, string name, float x, float y, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[7]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Activity activity = Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			AdHocActivity adHocActivity = activity as AdHocActivity;
			Activity newActivity = new Activity(NPrID, name, x, y);
			
			adHocActivity.lane.Elements.Add(newActivity);
			Painter.Manager.InspectorSubPrimitive = activity;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void AddComposedSubActivityAck(string status, string player, int PID, int PVID, int LaneID,
		int ActPrID, int NPrID, string name, float x, float y, bool update)
	{	
		try
		{
			ValidateAcknowledge(status, IOExceptions[8]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Activity activity = Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			ComposedActivity composedActivity = activity as ComposedActivity;
			Activity newActivity = new Activity(NPrID, name, x, y);
			
			composedActivity.lane.Elements.Add(newActivity);
			Painter.Manager.InspectorSubPrimitive = newActivity;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void AddEventComposedAck(string status, string player, int PID, int PVID, int LaneID, int ActPrID, int NPrID, string type,
		float x, float y, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[9]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Activity activity = Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			ComposedActivity composedActivity = activity as ComposedActivity;
			Event compEvent;
			
			if (type.Equals("Start"))
				compEvent = new Event(NPrID, Event.Categ.Start, x, y);
			else if (type.Equals("End"))
				compEvent = new Event(NPrID, Event.Categ.End, x, y);
			else if (type.Equals("Merge"))
				compEvent = new Event(NPrID, Event.Categ.Merge, x, y);
			else
				compEvent = new Event();
			
			compEvent.x = x; compEvent.y = y;
			composedActivity.lane.Elements.Add(compEvent);
			Painter.Manager.InspectorSubPrimitive = compEvent;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void AddConnectionComposedAck(string status, string player, int PID, int PVID, int LaneID, int ActPrID, int PrID,
		int sourceID, int targetID, string condition, bool update)
	{		
		try
		{
			ValidateAcknowledge(status, IOExceptions[10]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Activity activity = Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			ComposedActivity composedActivity = activity as ComposedActivity;
			Flow flow = new Flow(PrID, Flow.Categ.Sequence, sourceID, targetID, condition);
			
			composedActivity.Connections.Add(flow);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void NewConnectionAck(string status, string player, int PID, int PVID, int PrID,
		int SourcePrimID, int TargetPrimID, string condition, string type, bool update)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[11]);
			if (!ProcessExists(PID, PVID) || (update && !Painter.UpdateMode))
				return;
			
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			Flow flow;
			
			if (type.Equals("Sequence"))
				flow = new Flow(PrID, Flow.Categ.Sequence, SourcePrimID, TargetPrimID, condition);
			else
				flow = new Flow(PrID, Flow.Categ.Information, SourcePrimID, TargetPrimID, condition);
			process.Connections.Add(flow);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void EditProcessAck(string status, int PID, string name, string description)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[12]);
			if (!ProcessExists(PID, -1))
				return;
			
			Process process = Painter.Manager.GameState.GetProcess(PID);
			
			process.Name = name;
			process.Description = description;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void EditConnectionAck(string status, int PID, int PVID, int SourcePrID, int TargetPrID, string condition)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[13]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Flow flow = Painter.Manager.GameState.GetFlow(PID, PVID, SourcePrID, TargetPrID);
			
			flow.Condition = condition;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void EditSubFlowAck(string status, int PID, int PVID, int LaneID, int ActPrID, int FlowID, string condition)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[14]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Flow flow = Painter.Manager.GameState.GetSubFlow(PID, PVID, LaneID, ActPrID, FlowID);
			
			flow.Condition = condition;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void EditActivityAck(string status, int PID, int PVID, int LaneID, int PrID, string name)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[15]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Activity activity = Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, PrID);
			activity.Name = name;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void EditSubActivityAck(string status, int PID, int PVID, int LaneID, int ActPrID, int SPrID, string name)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[16]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Primitive primitive = Painter.Manager.GameState.GetSubPrimitive(PID, PVID, LaneID, ActPrID, SPrID);
			Activity activity = primitive as Activity;
			
			activity.Name = name;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RemovePrimitiveAck(string status, int PID, int PVID, int LaneID, int PrID)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[17]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Primitive primitive = Painter.Manager.GameState.GetPrimitive(PID, PVID, PrID);
			Lane lane = Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
			
			lane.Elements.Remove(primitive);
			Painter.Manager.GameState.RemoveFlows(PID, PVID, PrID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RemoveSubPrimitiveAck(string status, int PID, int PVID, int LaneID, int ActPrID, int SPrID)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[18]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Activity activity = Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			Primitive primitive = Painter.Manager.GameState.GetSubPrimitive(PID, PVID, LaneID, ActPrID, SPrID);
			
			if (activity is AdHocActivity)
			{
				AdHocActivity adHocActivity = activity as AdHocActivity;	
				adHocActivity.lane.Elements.Remove(primitive);
			}
			else if (activity is ComposedActivity)
			{
				ComposedActivity composedActivity = activity as ComposedActivity;	
				composedActivity.lane.Elements.Remove(primitive);
				Painter.Manager.GameState.RemoveSubFlows(PID, PVID, LaneID, ActPrID, SPrID);
			}
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RemoveFlowAck(string status, int PID, int PVID, int FlowID)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[19]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			Flow flow = Painter.Manager.GameState.GetFlow(PID, PVID, FlowID);
			
			process.Connections.Remove(flow);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RemoveSubFlowAck(string status, int PID, int PVID, int LaneID, int ActPrID, int FlowID)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[20]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Activity activity = Painter.Manager.GameState.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			ComposedActivity composedActivity = activity as ComposedActivity;
			Flow flow = Painter.Manager.GameState.GetSubFlow(PID, PVID, LaneID, ActPrID, FlowID);
			
			composedActivity.Connections.Remove(flow);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RemoveLaneAck(string status, int PID, int PVID, int LaneID)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[21]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			Lane lane = Painter.Manager.GameState.GetLane(PID, PVID, LaneID);
			
			foreach (Primitive prim in lane.Elements)
				Painter.Manager.GameState.RemoveFlows(PID, PVID, prim.PrID);
			
			bool toReposition = false;
			foreach (Lane l in process.Pool)
			{
				if (l.PrID == LaneID) toReposition = true;
				
				if (toReposition)
					foreach (Primitive p in l.Elements)
						p.y -= 180;
			}
			
			process.Pool.Remove(lane);
			
			for (int i = 0; i < process.Pool.Count; i++)
			{
				if (i == 0)
				{
					process.Pool[0].x = (Painter.SCREEN_WIDTH-60)/2;
					process.Pool[0].y = 180/2;
				}
				else
				{
					process.Pool[i].x = (Painter.SCREEN_WIDTH-60)/2;
					process.Pool[i].y = 179*i + 180/2;
				}
			}
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RemoveProcessAck(string status, int PID, int PVID)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[22]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			
			if (process is ProcessVersion)
			{
				if ((process as ProcessVersion).PVID == Painter.Manager.CurrentVersion.PVID)
				{
					Painter.Manager.CurrentVersion = null;
					Painter.Manager.CurrentScreen = GameManager.GameScreen.Home;
				}
			}
			else
			{
				if (process.PID == Painter.Manager.CurrentProcess.PID)
				{
					Painter.Manager.CurrentProcess = null;
					Painter.Manager.CurrentScreen = GameManager.GameScreen.Home;
				}
			}
			
			if (PVID == -1) 
				Painter.Manager.GameState.LocalProcesses.Remove(process);
			else
			{
				Process originalProcess = Painter.Manager.GameState.GetProcess(PID);
				originalProcess.Versions.Remove(process as ProcessVersion);
			}
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void ChangeLaneAck(string status, int PID, int PVID, int PrID, int LaneID)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[23]);
			if (!ProcessExists(PID, PVID))
				return;
			
			Painter.Manager.GameState.ChangeLane(PID, PVID, PrID, LaneID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RepositionPrimitiveAck(string status, int PID, int PVID, int PrID, float x, float y)
	{
		try
		{
			if (!ProcessExists(PID, PVID))
					return;
			
			Primitive primitive = Painter.Manager.GameState.GetPrimitive(PID, PVID, PrID);
			
			primitive.x = x;
			primitive.y = y;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RepositionSubPrimitiveAck(string status, int PID, int PVID, int ActPrID, int PrID, float x, float y)
	{
		try
		{
			Primitive primitive = Painter.Manager.GameState.GetPrimitive(PID, PVID, ActPrID);
			
			if (primitive is AdHocActivity)
			{
				AdHocActivity adHocActivity = primitive as AdHocActivity;
				
				foreach (Primitive prim in adHocActivity.lane.Elements)
					if (prim.PrID == PrID)
					{
						prim.x = x;
						prim.y = y;
						break;
					}
			}
			else if (primitive is ComposedActivity)
			{
				ComposedActivity composedActivity = primitive as ComposedActivity;
				
				foreach (Primitive prim in composedActivity.lane.Elements)
					if (prim.PrID == PrID)
					{
						prim.x = x;
						prim.y = y;
						break;
					}
			}
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	#endregion

	#region Data Consistency
	[RPC]
	public void LoadLocalProcessesAck(string player)
	{
		if (Painter.Manager.CurrentPlayer != null &&
			Painter.Manager.CurrentPlayer.Username.Equals(player))
			Painter.UpdateMode = false;
	}
	#endregion

	#region Mechanics
	[RPC]
	public void VoteQualityProcessAck(string status, int PID, bool vote, string username)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[24]);
			Process process = Painter.Manager.GameState.GetProcess(PID);
			process.VoteForQuality(vote, username);
			Painter.Manager.CurrentPlayer.NumVotesProcesses++;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void VoteQualityVersionAck(string status, string username, int PID, int PVID, bool vote)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[24]);
			ProcessVersion version = Painter.Manager.GameState.GetProcessVersion(PID, PVID);
			version.VoteForQuality(vote, username);
			Painter.Manager.CurrentPlayer.NumVotesVersions++;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void MarkAsDuplicatedAck(string status, int PID, int PVID, int originalPID, int originalPVID, string username)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[25]);
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			process.MarkAsDuplication(username, originalPID, originalPVID);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void VoteDuplicationProcessAck(string status, int PID, int PVID, bool vote, string username)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[26]);
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			process.VoteForDuplication(vote, username);
			if (PVID == -1) Painter.Manager.CurrentPlayer.NumVotesProcesses++;
			else Painter.Manager.CurrentPlayer.NumVotesVersions++;
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void PublishProcessAck(string status, string player, int PID, int PVID, int score)
	{
		try
		{
			ValidateAcknowledge(status, IOExceptions[27]);
			
			Process process = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
			process.score = score;
			process.published = true;
			process.Author = player;
			Painter.Manager.CurrentScreen = GameManager.GameScreen.Home;
			
			if (Painter.Manager.CurrentPlayer.Username.Equals(player))
			{
				Painter.Manager.Notifications.Items.Add(
					new Notification(Notification.Type.Default,
						(PVID == -1 ? "Process" : "Version") + " published! You scored " + score + " points."));
				Painter.Manager.CurrentPlayer.Score += score;
				if (PVID == -1)
				{
					Painter.Manager.CurrentPlayer.NumDraftProcesses--;
					Painter.Manager.CurrentPlayer.NumPubProcesses++;
				}
				else
				{
					Painter.Manager.CurrentPlayer.NumDraftVersions--;
					Painter.Manager.CurrentPlayer.NumPubVersions++;
				}
			}
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void NewAchievementAck(string status, string player, string type, bool updateMode)
	{
		try
		{
			if (!Painter.Manager.CurrentPlayer.Username.Equals(player)) return;
			
			Achievement.Categ categ = Achievement.GetCateg(type);
			ValidateAcknowledge(status, IOExceptions[28]);
			
			Painter.Manager.CurrentPlayer.NewAchievement(categ, true, updateMode);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void NewMedalAck(string status, string player, string type, bool updateMode)
	{
		try
		{
			if (!Painter.Manager.CurrentPlayer.Username.Equals(player)) return;
			
			Medal.Categ categ = Medal.GetCateg(type);
			ValidateAcknowledge(status, IOExceptions[28]);

			Painter.Manager.CurrentPlayer.NewMedal(categ, true, updateMode);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void GameOverAck()
	{
		Painter.Manager.LoadPlayerScore(Painter.Manager.CurrentPlayer.Username);
		PaintPanels.HidePanels();
		Painter.Manager.GameOn = false;
		Painter.EnableProcessEdition = false;
		PaintPanels.ShowGameOver = true;
	}
	
	[RPC]
	public void UpdatePlayerBonusMalus(string player, float poisson, float voting, float duplication)
	{
		if (!Painter.Manager.CurrentPlayer.Username.Equals(player)) return;
		
		Painter.Manager.CurrentPlayer.bonusMalus[0] = poisson;
		Painter.Manager.CurrentPlayer.bonusMalus[1] = voting;
		Painter.Manager.CurrentPlayer.bonusMalus[2] = duplication;
	}
	
	[RPC]
	public void FinalConsensus(int PID, int PVID)
	{
		Process fcProcess = Painter.Manager.GameState.GetTargetProcess(PID, PVID);
		fcProcess.finalConsensus = true;
	}
	#endregion

	[RPC]
	public void UpdateMode(bool update) { Painter.UpdateMode = update; }

	[RPC]
	public void ResetStateAck()
	{
		Painter.Manager.GameState.Reset();
		Painter.Manager.CurrentPlayer.Reset();
		Painter.Manager.CurrentScreen = GameManager.GameScreen.Home;
	}

	#endregion
}
