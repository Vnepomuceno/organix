using UnityEngine;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

public class Server : MonoBehaviour
{	
	public static int SCREEN_WIDTH = 1280;
	public static int SCREEN_HEIGHT = 700;
	
	public enum GameScreen { Home, ViewProcess, ViewComposed, ViewAdHoc,
		ViewVersion, ViewVersionComposed, ViewVersionAdHoc };
	
	public static ServerState State;
	
	public static Process CurrentProcess;
	public static ProcessVersion CurrentVersion;
	public static GameScreen CurrentScreen;
	public static Primitive CurrentPrimitive;
	public static Primitive CurrentSubPrimitive;
	public static Player SelectedPlayer;
	public static bool RequestReset, AssignProcesses, ProcessAssignFinished;
	public static List<string> ProcessNames;
	
	private string XmlMessage;
	private bool XmlReceived;
	public static bool GameOver;
	
	public void Start()
	{
		Network.InitializeServer(32, 8080, false);
		
		State = new ServerState();
		State = State.LoadXml();
		
		CurrentScreen = GameScreen.Home;
		ProcessNames = new List<string>();
		
		XmlMessage = "";
	}
	
	public void OnApplicationQuit()
	{
		State.SaveXml();
		Debug.Log("* Saved server game state in XML.");
		Disconnect();
	}
	
	public static void Disconnect() { Network.Disconnect();	}
	
	public void Update()
	{
		if (RequestReset)
		{
			State.Reset();
			PlayerPrefs.DeleteAll();
			foreach (Player player in State.ActivePlayers)
				player.Reset();
			networkView.RPC("ResetStateAck", RPCMode.Others);
			RequestReset = false;
		}
		
		if (GameOver)
		{
			foreach (Player player in State.ActivePlayers)
			{
				player.ConvergenceRate = State.CalculateConvergenceRate(player.Username);
				State.CalculateFinalPlayerScore(player);
				networkView.RPC("UpdatePlayerBonusMalus", RPCMode.Others, player.Username,
					player.bonusMalus[0], player.bonusMalus[1], player.bonusMalus[2]);
			}
			
			DetermineDuplicationMedals();
			DetermineQualityContentMedal();
			DetermineBestConvergenceRate();
			
			DetermineFinalConsensus();
			
			GameOver = false;
		}
	}
	
	public void DetermineDuplicationMedals()
	{
		foreach (Process process in State.LocalProcesses)
		{
			if (process.markedDuplication)
			{
				Player marker = GetPlayer(process.markAuthor, State.ActivePlayers);
				
				if (marker != null && process.markAuthor.Equals(marker.Username) &&
					process.posDuplicationVotes > process.negDuplicationVotes)
				{
					marker.NewMedal(Medal.Categ.MarkedProcessDuplicated, false, false);
					NewMedal(marker.Username, Medal.Categ.MarkedProcessDuplicated, false);
				}
			}
		}
	}
	
	public void DetermineQualityContentMedal()
	{
		List<Process> winners = new List<Process>();
		winners.Add(State.LocalProcesses[0]);
		
		foreach (Process process in State.LocalProcesses)
		{
			if (process.GetQualityVoteRate() > winners[0].GetQualityVoteRate())
			{
				winners.Clear();
				winners.Add(process);
			}
			else if (process.GetQualityVoteRate() == winners[0].GetQualityVoteRate())
				winners.Add(process);
		}
		
		foreach (Process winner in winners)
		{
			Player player = GetPlayer(winner.Author, State.ActivePlayers);
			
			if (player != null && !player.AlreadyContainsMedal(Medal.Categ.BestQualityContent) &&
				winner.GetQualityVoteRate() != 0)
			{
				player.NewMedal(Medal.Categ.BestQualityContent, false, false);
				NewMedal(winner.Author, Medal.Categ.BestQualityContent, false);
			}
		}
	}
	
	public void DetermineBestConvergenceRate()
	{
		List<Player> winners = new List<Player>();
		winners.Add(State.ActivePlayers[0]);
		
		foreach (Player player in State.ActivePlayers)
		{
			if (player.ConvergenceRate > winners[0].ConvergenceRate)
			{
				winners.Clear();
				winners.Add(player);
			}
			else if (player.ConvergenceRate == winners[0].ConvergenceRate)
				winners.Add(player);
		}
		
		foreach (Player winner in winners)
		{
			if (!winner.AlreadyContainsMedal(Medal.Categ.BestVoteConvergence) &&
				winner.ConvergenceRate != -1)
			{
				winner.NewMedal(Medal.Categ.BestVoteConvergence, false, false);
				NewMedal(winner.Username, Medal.Categ.BestVoteConvergence, false);
			}
		}
	}
	
	public static string GetTimestamp()
	{
		DateTime Now = DateTime.Now;
		return "<b>[" + Now.Hour + ":" + Now.Minute + ":" + Now.Second + "]</b> ";
	}
	
	public string DebugList(List<Process> list)
	{
		string output = "";
		foreach (Process process in list)
		{
			output += output.Equals("") ? "" : ", ";
			if (process is ProcessVersion)
				output += "V" + (process as ProcessVersion).PVID + " (" + process.PID + ")";
			else
				output += "P" + process.PID;
		}

		return output;
	}
	
	public void DetermineFinalConsensus()
	{
		List<Process> processBag = new List<Process>();
		List<Process> orderedProcesses = new List<Process>();
		List<Process> finalConsensus = new List<Process>();

		#region Remotion of processes marked as duplicate by players
		foreach (Process process in State.LocalProcesses)
		{
			if (process.markedDuplication &&
			    process.posDuplicationVotes > process.negDuplicationVotes) continue;

			processBag.Add(process);
			
			foreach (ProcessVersion version in process.Versions)
			{
				if (version.markedDuplication &&
				    version.posDuplicationVotes > version.negDuplicationVotes) continue;
			
				processBag.Add(version);
			}
		}
		#endregion

		#region Order process set by descending order of total process score
		Debug.Log(DebugList(processBag));
		orderedProcesses = processBag.OrderByDescending(proc => proc.GetTotalScore()).ToList();
		Debug.Log(DebugList(orderedProcesses));
		#endregion

		#region Find process(es) with best total score and store it(/them) in the final consensus set
		finalConsensus.Add(orderedProcesses[0]);
		foreach (Process NDProcess in orderedProcesses)
		{
			if (NDProcess.GetTotalScore() == finalConsensus[0].GetTotalScore())
				finalConsensus.Add(NDProcess);
			else if (NDProcess.GetTotalScore() > finalConsensus[0].GetTotalScore())
			{
				finalConsensus.Clear();
				finalConsensus.Add(NDProcess);
			}
		}
		#endregion

		#region Change state of each winning process, assign bonuses to authors and notify players of the final consensus set
		foreach (Process fcProcess in finalConsensus)
		{
			Debug.Log("P" + fcProcess.PID + ((fcProcess is ProcessVersion) ? " V" + (fcProcess as ProcessVersion).PVID : "") +
				": " + fcProcess.Name);
			if (fcProcess is ProcessVersion)
				(fcProcess as ProcessVersion).finalConsensus = true;
			else
				fcProcess.finalConsensus = true;

			Player player = State.GetPlayer(fcProcess.Author, State.ActivePlayers);
			player.Score += (float)(fcProcess.score*0.25);
			
			networkView.RPC("FinalConsensus", RPCMode.Others, fcProcess.PID,
			                (fcProcess is ProcessVersion) ? (fcProcess as ProcessVersion).PVID : -1);
		}
		#endregion
	}
	
	public Player GetPlayer(string username, List<Player> list)
	{
		foreach (Player p in list)
			if (p.Username.Equals(username))
				return p;
		
		return null;
	}
	
	public Player GetPlayer(string player)
	{
		foreach (Player p in State.ActivePlayers)
			if (p.Username.Equals(player))
				return p;
		
		return null;
	}
	
	public Process GetTargetProcess(int PID, int PVID)
	{
		Process process;
		
		if (PVID == -1)
			process = State.GetProcess(PID);
		else
			process = State.GetProcessVersion(PID, PVID);
		
		return process;
	}
	
	public int CalculateProcessScore(int PID, int PVID)
	{
		int score = 0;
		Process process = GetTargetProcess(PID, PVID);

		foreach (Lane lane in process.Pool)
		{
			foreach (Primitive prim in lane.Elements)
			{
				if (prim is ComposedActivity)
				{
					ComposedActivity compAct = prim as ComposedActivity;
					if (compAct.lane.Elements.Count > 2)
						score += 50;
				
					foreach (Primitive subPrimComp in compAct.lane.Elements)
					{
						if (subPrimComp is Activity &&
							(subPrimComp as Activity).Name != "")
							score += 25;
					}
				}
				else if (prim is AdHocActivity)
				{
					AdHocActivity adHocAct = prim as AdHocActivity;
					if (adHocAct.lane.Elements.Count > 0)
						score += 50;
			
					foreach (Primitive subPrimAdHoc in (prim as AdHocActivity).lane.Elements)
					{
						if (subPrimAdHoc is Activity && (subPrimAdHoc as Activity).Name != "")
							score += 25;
					}
				}
				else if (prim is Activity && (prim as Activity).Name != "")
					score += 25;
			}
		}
		
		return score;
	}
	
	public void SendProcess(string player, Process process, bool update)
	{
		int PVID = -1;
		int PID = -1;
		
		if (process is ProcessVersion)
		{
			ProcessVersion version = process as ProcessVersion;
			PVID = version.PVID;
			PID = version.OriginalPID;
			networkView.RPC("NewProcessVersionAck", RPCMode.Others, "Success", version.OriginalPID,
				version.PVID, version.Author, version.markedDuplication, version.markAuthor,
				version.score, version.published, update);
		}
		else
		{
			PID = process.PID;
			networkView.RPC("NewProcessAck", RPCMode.Others, "Success",
				PID, process.Author, process.Name, process.Description,
				process.markedDuplication, process.markAuthor, process.duplicationPID,
				process.duplicationPVID, process.score, process.published, update);
		}
	
		#region Loads LANES
		foreach (Lane lane in process.Pool)
		{
			networkView.RPC("NewLaneAck", RPCMode.Others, "Success",
				player, PID, PVID, lane.PrID, lane.Participant, lane.x, lane.y, update);
			
			#region Loads PRIMITIVES
			foreach (Primitive primitive in lane.Elements)
			{
				#region EVENT
				if (primitive is Event)
				{
					Event ev = primitive as Event;
					
					if (ev.categ.Equals(Event.Categ.Start))
						networkView.RPC("NewEventAck", RPCMode.Others, "Success",
							player, PID, PVID, lane.PrID, ev.PrID, "Start", ev.x, ev.y, update);
					else if (ev.categ.Equals(Event.Categ.End))
						networkView.RPC("NewEventAck", RPCMode.Others, "Success",
							player, PID, PVID, lane.PrID, ev.PrID, "End", ev.x, ev.y, update);
					else if (ev.categ.Equals(Event.Categ.Merge))
						networkView.RPC("NewEventAck", RPCMode.Others, "Success",
							player, PID, PVID, lane.PrID, ev.PrID, "Merge", ev.x, ev.y, update);
				}
				#endregion
				
				#region COMPOSED ACTIVITY
				else if (primitive is ComposedActivity)
				{
					ComposedActivity compActivity = primitive as ComposedActivity;
					networkView.RPC("NewComposedActivityAck", RPCMode.Others, "Success",
						player, PID, PVID, lane.PrID, compActivity.PrID, compActivity.Name,
						compActivity.x, compActivity.y, update);
					
					foreach (Primitive prim in compActivity.lane.Elements)
					{
						if (prim is Activity)
						{
							Activity compSubActivity = prim as Activity;
							networkView.RPC("AddComposedSubActivityAck", RPCMode.Others, "Success",
								player, PID, PVID, lane.PrID, compActivity.PrID, compSubActivity.PrID,
								compSubActivity.Name, compSubActivity.x, compSubActivity.y, update);
						}
						else if (prim is Event)
						{
							Event compSubEvent = prim as Event;
							networkView.RPC("AddEventComposedAck", RPCMode.Others, "Success",
								player, PID, PVID, lane.PrID, compActivity.PrID, compSubEvent.PrID,
								compSubEvent.categ.ToString(), compSubEvent.x, compSubEvent.y, update);
						}
					}
					
					foreach (Flow flowComposed in compActivity.Connections)
					{
						networkView.RPC("AddConnectionComposedAck", RPCMode.Others, "Success",
							player, PID, PVID, lane.PrID, compActivity.PrID, flowComposed.PrID,
							flowComposed.SourceID, flowComposed.TargetID, flowComposed.Condition, update);
					}
				}
				#endregion
				
				#region AD-HOC ACTIVITY 
				else if (primitive is AdHocActivity)
				{
					AdHocActivity adHocActivity = primitive as AdHocActivity;
					networkView.RPC("NewAdHocActivityAck", RPCMode.Others, "Success",
						player, PID, PVID, lane.PrID, adHocActivity.PrID, adHocActivity.Name,
						adHocActivity.x, adHocActivity.y, update);
					
					foreach (Primitive prim in adHocActivity.lane.Elements)
					{
						Activity activity = prim as Activity;
						networkView.RPC("AddAdHocSubActivityAck", RPCMode.Others, "Success",
							player, PID, PVID, lane.PrID, adHocActivity.PrID,
							activity.PrID, activity.Name, activity.x, activity.y, update);
					}
				}
				#endregion
				
				#region WORK ACTIVITY
				else if (primitive is Activity)
				{
					Activity activity = primitive as Activity;
					networkView.RPC("NewActivityAck", RPCMode.Others, "Success",
						player, PID, PVID, lane.PrID, activity.PrID,
						activity.Name, activity.x, activity.y, update);
				}
				#endregion
			}
			#endregion
		}
		#endregion
		
		#region Loads FLOWS
		foreach (Flow edge in process.Connections)
			networkView.RPC("NewConnectionAck", RPCMode.Others, "Success",
				player, PID, PVID, edge.PrID, edge.SourceID, edge.TargetID,
				edge.Condition, edge.categ.ToString(), update);
		#endregion

		#region Loads VOTES
		foreach (Vote qVote in process.QualityVotes)
			if (process is ProcessVersion)
				networkView.RPC("VoteQualityVersionAck", RPCMode.Others, "Success",
					qVote.GetVoter() == null ? "" : qVote.GetVoter(), PID, PVID, qVote.GetVote());
			else
				networkView.RPC("VoteQualityProcessAck", RPCMode.Others, "Success",
				PID, qVote.GetVote(), qVote.GetVoter() == null ? "" : qVote.GetVoter());
		
		foreach (Vote dVote in process.DuplicationVotes)
			networkView.RPC("VoteDuplicationProcessAck", RPCMode.Others, "Success",
				PID, PVID, dVote.GetVote(), dVote.GetVoter() == null ? "" : dVote.GetVoter());
		#endregion
	}
	
	public void DuplicateProcessToVersion(int PID, int PVID, string author)
	{
		Process process = State.GetProcess(PID);
		
		networkView.RPC("UpdateMode", RPCMode.Others, true);
		
		networkView.RPC("NewProcessVersionAck", RPCMode.Others, "Success", PID, PVID, author, false, "", 0, false, true);
		
		foreach (Lane lane in process.Pool)
		{
			NewLane(author, PID, PVID, lane.PrID, lane.Participant);
		
			#region Loads PRIMITIVES
			foreach (Primitive primitive in lane.Elements)
			{
				#region EVENT
				if (primitive is Event)
				{
					Event ev = primitive as Event;
					NewEvent(author, PID, PVID, lane.PrID, ev.PrID, ev.categ.ToString(), ev.x, ev.y);
				}
				#endregion
				
				#region COMPOSED ACTIVITY
				else if (primitive is ComposedActivity)
				{
					ComposedActivity compActivity = primitive as ComposedActivity;
					NewComposedActivity(author, PID, PVID, lane.PrID, compActivity.PrID, compActivity.Name, compActivity.x, compActivity.y);
					
					foreach (Primitive prim in compActivity.lane.Elements)
					{
						if (prim is Activity)
						{
							Activity compSubActivity = prim as Activity;
							AddComposedSubActivity(author, PID, PVID, lane.PrID, compActivity.PrID, compSubActivity.PrID,
								compSubActivity.Name, compSubActivity.x, compSubActivity.y);
						}
						else if (prim is Event)
						{
							Event compSubEvent = prim as Event;
							AddEventComposed(author, PID, PVID, lane.PrID, compActivity.PrID, compSubEvent.PrID, compSubEvent.categ.ToString(),
								compSubEvent.x, compSubEvent.y);
						}
					}
					
					foreach (Flow flowComposed in compActivity.Connections)
						AddConnectionComposed(author, PID, PVID, lane.PrID, compActivity.PrID, flowComposed.PrID, flowComposed.SourceID,
							flowComposed.TargetID, flowComposed.Condition);
				}
				#endregion
				
				#region AD-HOC ACTIVITY 
				else if (primitive is AdHocActivity)
				{
					AdHocActivity adHocActivity = primitive as AdHocActivity;
					NewAdHocActivity(author, PID, PVID, lane.PrID, adHocActivity.PrID, adHocActivity.Name, adHocActivity.x, adHocActivity.y);
					
					foreach (Primitive prim in adHocActivity.lane.Elements)
					{
						Activity activity = prim as Activity;
						AddAdHocSubActivity(author, PID, PVID, lane.PrID, adHocActivity.PrID, activity.PrID, activity.Name);
					}
				}
				#endregion
				
				#region WORK ACTIVITY
				else if (primitive is Activity)
				{
					Activity activity = primitive as Activity;
					NewActivity(author, PID, PVID, lane.PrID, activity.PrID, activity.Name, activity.x, activity.y);
				}
				#endregion
			}
			#endregion
		}
		
		#region Loads FLOWS
		foreach (Flow edge in process.Connections)
			NewConnection(author, PID, PVID, edge.SourceID, edge.TargetID, edge.Condition, edge.categ.ToString());
		#endregion

		networkView.RPC("UpdateMode", RPCMode.Others, false);
	}
	
	public void NewAchievement(string player, Achievement.Categ type, bool updateMode)
	{
		networkView.RPC("NewAchievementAck", RPCMode.Others, "Success", player, type.ToString(), updateMode);
	}
	
	public void NewMedal(string player, Medal.Categ type, bool updateMode)
	{
		networkView.RPC("NewMedalAck", RPCMode.Others, "Success", player, type.ToString(), updateMode);
	}
	
	#region RPC SENDER
	
	#region Language Constructor
	[RPC] public void LoginAck(string status, string username, float gameLength, string processName) {}
	[RPC] public void NewProcessAck(string status, int PID, string author, string name, string description, bool markedDuplicated,string markAuthor, int dupPID, int dupPVID, int score, bool published, bool update) {}
	[RPC] public void NewLaneAck(string status, string player, int PID, int PVID, int PrID, string participant, float x, float y, bool update) {}
	[RPC] public void NewEventAck(string status, string player, int PID, int PVID, int LaneID, int PrID, string category, float x, float y, bool update) {}
	[RPC] public void NewActivityAck(string status, string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y, bool update) {}
	[RPC] public void NewComposedActivityAck(string status, string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y, bool update) {}
	[RPC] public void NewAdHocActivityAck(string status, string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y, bool update) {}
	[RPC] public void AddAdHocSubActivityAck(string status, string player, int PID, int PVID, int LaneID, int ActPrID, int NPrID, string name, float x, float y, bool update) {}
	[RPC] public void AddComposedSubActivityAck(string status, string player, int PID, int PVID, int LaneID, int ActPrID, int NPrID, string name, float x, float y, bool update) {}
	[RPC] public void AddEventComposedAck(string status, string player, int PID, int PVID, int LaneID, int ActPrID, int NPrID, string type, float x, float y, bool update) {}
	[RPC] public void AddConnectionComposedAck(string status, string player, int PID, int PVID, int LaneID, int ActPrID, int FlowID, int sourceID, int targetID, string condition, bool update) {}
	[RPC] public void NewConnectionAck(string status, string player, int PID, int PVID, int PrID, int sourcePrID, int targetPrID, string condition, string type, bool update) {}
	[RPC] public void EditProcessAck(string status, int PID, string name, string description) {}
	[RPC] public void EditLaneAck(string status, int PID, int PVID, int LaneID, string participant) {}
	[RPC] public void EditConnectionAck(string status, int PID, int PVID, int sourcePrID, int targetPrID, string condition) {}
	[RPC] public void EditSubFlowAck(string status, int PID, int PVID, int LaneID, int ActPrID, int FlowID, string condition) {}
	[RPC] public void EditActivityAck(string status, int PID, int PVID, int LaneID, int PrID, string name) {}
	[RPC] public void EditSubActivityAck(string status, int PID, int PVID, int LaneID, int ActPrID, int SPrID, string name) {}
	[RPC] public void RemovePrimitiveAck(string status, int PID, int PVID, int LaneID, int PrID) {}
	[RPC] public void RemoveSubPrimitiveAck(string status, int PID, int PVID, int LaneID, int ActPrID, int SPrID) {}
	[RPC] public void RemoveFlowAck(string status, int PID, int PVID, int FlowID) {}
	[RPC] public void RemoveSubFlowAck(string status, int PID, int PVID, int LaneID, int ActPrID, int FlowID) {}
	[RPC] public void RemoveLaneAck(string status, int PID, int PVID, int LaneID) {}
	[RPC] public void RemoveProcessAck(string status, int PID, int PVID) {}
	[RPC] public void ChangeLaneAck(string status, int PID, int PVID, int PrID, int LaneID) {}
	[RPC] public void RepositionPrimitiveAck(string status, int PID, int PVID, int PrID, float x, float y) {}
	[RPC] public void RepositionSubPrimitiveAck(string status, int PID, int PVID, int ActPrID, int PrID, float x, float y) {}
	[RPC] public void NewProcessVersionAck(string status, int PID, int PVID, string author, bool markedDuplication, string markAuthor, int score, bool published, bool update) {}
	#endregion

	#region Game Mechanics
	[RPC] public void VoteQualityProcessAck(string status, int PID, bool vote, string username) {}
	[RPC] public void VoteQualityVersionAck(string status, string username, int PID, int PVID, bool vote) {}
	[RPC] public void MarkAsDuplicatedAck(string status, int PID, int PVID, int originalPID, int originalPVID, string username) {}
	[RPC] public void VoteDuplicationProcessAck(string status, int PID, int PVID, bool vote, string username) {}
	[RPC] public void PublishProcessAck(string status, string player, int PID, int PVID, int score) {}
	[RPC] public void NewAchievementAck(string status, string player, string type, bool updateMode) {}
	[RPC] public void NewMedalAck(string status, string player, string type, bool updateMode) {}
	[RPC] public void GameOverAck() {}
	#endregion

	#region Data Consistency
	[RPC] public void LoadLocalProcessesAck(string player) {}
	[RPC] public void UpdateMode(bool update) {}
	[RPC] public void ResetStateAck() {}
	[RPC] public void UpdatePlayerBonusMalus(string player, float poisson, float voting, float duplication) {}
	[RPC] public void FinalConsensus(int PID, int PVID) {}
	#endregion

	#endregion
	

	#region RPC RECEIVER
	
	#region Language Constructor
	[RPC]
	public void Login(string username, string password)
	{
		int NumLoggedPlayers = 0;
		
		foreach (Player player in State.ActivePlayers)
			NumLoggedPlayers += player.Online ? 1 : 0;
		
		if (GetPlayer(username, State.RegisteredPlayers.Players) == null)
		{
			networkView.RPC("LoginAck", RPCMode.Others, "Unregistered", username,
				float.Parse(PaintServerPanels.GameLength)*60, PaintServerPanels.ToElicitProcessName);
		}
		else if (!GetPlayer(username, State.RegisteredPlayers.Players).Password.Equals(password))
		{
			networkView.RPC("LoginAck", RPCMode.Others, "WrongPass", username,
				float.Parse(PaintServerPanels.GameLength)*60, PaintServerPanels.ToElicitProcessName);
		}
		else if (GetPlayer(username, State.ActivePlayers) != null && GetPlayer(username, State.ActivePlayers).Online)
		{
			networkView.RPC("LoginAck", RPCMode.Others, "AlreadyLogged", username,
				float.Parse(PaintServerPanels.GameLength)*60, PaintServerPanels.ToElicitProcessName);
		}
		else if (NumLoggedPlayers >= float.Parse(PaintServerPanels.NumberPlayers))
		{
			networkView.RPC("LoginAck", RPCMode.Others, "ExceededNumberPlayers", username,
				float.Parse(PaintServerPanels.GameLength)*60, PaintServerPanels.ToElicitProcessName);
		}
		else
		{
			Player LoggingPlayer = State.ActivePlayers.Find(player => player.Username.Equals(username));
			LoggingPlayer.Online = true;
			networkView.RPC("LoginAck", RPCMode.Others, "Success", username,
				float.Parse(PaintServerPanels.GameLength)*60, PaintServerPanels.ToElicitProcessName);
			Console.Log.Add("Player " + username + " has <b>logged in</b>.");
		}
	}
	
	[RPC]
	public void LogoutAck(string username)
	{
		Player LoggedPlayer = State.ActivePlayers.Find(player => player.Username.Equals(username));
		LoggedPlayer.Online = false;
		Console.Log.Add("Player " + LoggedPlayer.Username + " has <b>logged out</b>.");
	}

	[RPC]
	public void NewProcess(string author, string name, string description)
	{
		Process process = new Process(name, description);
		Player processAuthor = State.ActivePlayers.Find(player => player.Username.Equals(author));
		
		process.Author = author;
		if (processAuthor != null) processAuthor.NumDraftProcesses++;
		
		State.LocalProcesses.Add(process);
		networkView.RPC("NewProcessAck", RPCMode.Others, "Success", process.PID, author, name, description,
			process.markedDuplication, process.markAuthor, process.duplicationPID, process.duplicationPVID,
			process.score, process.published, false);
		Console.Log.Add("<b>Added process</b> " + process.PID + ": " +
			(name.Equals("") ? "Untitled" : name) + ".");
	}
	
	[RPC]
	public void NewLane(string player, int PID, int PVID, string participant)
	{
		try
		{
			Process process = GetTargetProcess(PID, PVID);
			Lane lane = new Lane(participant);
			
			if (process.Pool.Count == 0)
			{
				lane.x = (SCREEN_WIDTH-60)/2;
				lane.y = 180/2;
			}
			else
			{
				lane.x = (SCREEN_WIDTH-60)/2;
				lane.y = 179*process.Pool.Count + 180/2;
			}
			
			process.Pool.Add(lane);
			networkView.RPC("NewLaneAck", RPCMode.Others, "Success", player, PID, PVID, lane.PrID, participant, lane.x, lane.y, false);
			Console.Log.Add("<b>Added lane</b> to process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("NewLaneAck", RPCMode.Others, "Failure", player, PID, PVID, -1, participant, -1, -1, false);
		}
	}
	
	[RPC]
	public void NewLane(string player, int PID, int PVID, int LaneID, string participant)
	{
		try
		{
			Process process = GetTargetProcess(PID, PVID);
			Lane lane = new Lane(LaneID, participant);
			
			if (process.Pool.Count == 0)
			{
				lane.x = (SCREEN_WIDTH-60)/2;
				lane.y = 180/2;
			}
			else
			{
				lane.x = (SCREEN_WIDTH-60)/2;
				lane.y = 179*process.Pool.Count + 180/2;
			}
			
			process.Pool.Add(lane);
			networkView.RPC("NewLaneAck", RPCMode.Others, "Success", player, PID, PVID, lane.PrID, participant, lane.x, lane.y, false);
			Console.Log.Add("<b>Added lane</b> to process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("NewLaneAck", RPCMode.Others, "Failure", player, PID, PVID, -1, participant, -1, -1, false);
		}
	}
	
	[RPC]
	public void EditProcess(int PID, string name, string description)
	{
		try
		{
			Process process = State.GetProcess(PID);
			
			process.Name = name;
			process.Description = description;
			networkView.RPC("EditProcessAck", RPCMode.Others, "Success", PID, name, description);
			Console.Log.Add("<b>Edited process</b> " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("EditProcessAck", RPCMode.Others, "Failure", PID, name, description);
		}
	}
	
	[RPC]
	public void EditLane(int PID, int PVID, int LaneID, string participant)
	{
		try
		{
			Lane lane = State.GetLane(PID, PVID, LaneID);
			lane.Participant = participant;
			
			networkView.RPC("EditLaneAck", RPCMode.Others, "Success", PID, PVID, LaneID, participant);
			Console.Log.Add("<b>Edited lane</b> from process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("EditLaneAck", RPCMode.Others, "Failure", PID, PVID, LaneID, participant);
		}
	}
	
	[RPC]
	public void NewEvent(string player, int PID, int PVID, int LaneID, int PrID, string category, float x, float y)
	{
		try
		{
			Lane lane = State.GetLane(PID, PVID, LaneID);
			Event ev = null;
			if (category.Equals("Start"))
			{
				if (PrID == -1)
					ev = new Event(Event.Categ.Start);
				else
					ev = new Event(PrID, Event.Categ.Start, -1, -1);
				ev.x = 80;
			}
			else if (category.Equals("End"))
			{
				if (PrID == -1)
					ev = new Event(Event.Categ.End);
				else
					ev = new Event(PrID, Event.Categ.End, -1, -1);
				ev.x = SCREEN_WIDTH - 100;
			}
			else if (category.Equals("Merge"))
			{
				if (PrID == -1)
					ev = new Event(Event.Categ.Merge);
				else
					ev = new Event(PrID, Event.Categ.Merge, -1, -1);
				
				if (lane.Elements.Count != 0)
					if (lane.Elements[lane.Elements.Count-1] is Event &&
						(lane.Elements[lane.Elements.Count-1] as Event).categ.Equals(Event.Categ.End))
						ev.x = lane.Elements[lane.Elements.Count-2].x + 150;
					else
						ev.x = lane.Elements[lane.Elements.Count-1].x + 150;
				else
					ev.x = 80;
			}
			
			ev.y = lane.y;
			
			if (x != -1) { ev.x = x; ev.y = y; }
			
			lane.Elements.Add(ev);
			networkView.RPC("NewEventAck", RPCMode.Others, "Success",
				player, PID, PVID, lane.PrID, ev.PrID, category, ev.x, ev.y, false);
			if (ev.categ.Equals(Event.Categ.Start))
				Console.Log.Add("<b>Added start</b> to process " + PID + ".");
			else if (ev.categ.Equals(Event.Categ.End))
				Console.Log.Add("<b>Added end</b> to process " + PID + ".");
			else if (ev.categ.Equals(Event.Categ.Merge))
				Console.Log.Add("<b>Added merge</b> to process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("NewEventAck", RPCMode.Others, "Failure", player, PID, PVID, -1, -1, category, -1, -1, false);
		}
	}
	
	[RPC]
	public void NewActivity(string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y)
	{
		try
		{
			Process process = GetTargetProcess(PID, PVID);
			Activity activity;
			
			if (PrID == -1)
				activity = new Activity(name, "");
			else
				activity = new Activity(PrID, name);
			
			Lane lane = State.GetLane(PID, PVID, LaneID);
			
			if (x == -1)
			{
				if (lane.Elements.Count == 0)
				{
					activity.x = 200;
					activity.y = 180*process.Pool.Count - 90;
				}
				else if (lane.Elements[lane.Elements.Count-1] is Event &&
					!(lane.Elements[lane.Elements.Count-1] as Event).categ.Equals(Event.Categ.Merge))
				{
					activity.x = 200;
					activity.y = lane.Elements[lane.Elements.Count-1].y;
				}
				else
				{
					activity.x = lane.Elements[lane.Elements.Count-1].x + 170;
					activity.y = lane.Elements[lane.Elements.Count-1].y;
				}
			}
			else
			{
				activity.x = x;
				activity.y = y;
			}
			
			lane.Elements.Add(activity);
			networkView.RPC("NewActivityAck", RPCMode.Others, "Success", player, PID, PVID, LaneID,
				activity.PrID, activity.Name, activity.x, activity.y, false);
			Console.Log.Add("<b>Added activity</b> to process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("NewActivityAck", RPCMode.Others, "Failure", player, PID, PVID, LaneID,
				-1, -1, "", -1, -1, false);
		}
	}
	
	[RPC]
	public void NewComposedActivity(string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y)
	{
		try
		{
			Process process = GetTargetProcess(PID, PVID);
			ComposedActivity activity;
			
			if (PrID == -1)
				activity = new ComposedActivity();
			else
				activity = new ComposedActivity(PrID, name);
			
			Lane lane = State.GetLane(PID, PVID, LaneID);
			
			if (x == -1)
			{
				if (lane.Elements.Count == 0)
				{
					activity.x = 200;
					activity.y = 180*process.Pool.Count-90;
				}
				else if (lane.Elements[lane.Elements.Count-1] is Event &&
					!(lane.Elements[lane.Elements.Count-1] as Event).categ.Equals(Event.Categ.Merge))
				{
					activity.x = 200;
					activity.y = lane.Elements[lane.Elements.Count-1].y;
				}
				else
				{
					activity.x = lane.Elements[lane.Elements.Count-1].x + 170;
					activity.y = lane.Elements[lane.Elements.Count-1].y;
				}
			}
			else
			{
				activity.x = x;
				activity.y = y;
			}				
			
			lane.Elements.Add(activity);
	
			networkView.RPC("NewComposedActivityAck", RPCMode.Others, "Success", player, PID, PVID, LaneID,
				activity.PrID, activity.Name, activity.x, activity.y, false);
			Console.Log.Add("<b>Added composed activity</b> to process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("NewComposedActivityAck", RPCMode.Others, "Failure", player, PID, PVID, LaneID, -1, -1, "", (float)-1, (float)-1, false);
		}
	}
	
	[RPC]
	public void NewAdHocActivity(string player, int PID, int PVID, int LaneID, int PrID, string name, float x, float y)
	{
		try
		{
			AdHocActivity activity;
			
			if (PrID == -1)
				activity = new AdHocActivity();
			else
				activity = new AdHocActivity(PrID, name);
			
			Lane lane = State.GetLane(PID, PVID, LaneID);
			
			if (x == -1)
			{
				if (lane.Elements.Count == 0)
				{
					activity.x = 200;
					activity.y = lane.y;
				}
				else if (lane.Elements[lane.Elements.Count-1] is Event &&
					!(lane.Elements[lane.Elements.Count-1] as Event).categ.Equals(Event.Categ.Merge))
				{
					activity.x = 200;
					activity.y = lane.y;
				}
				else
				{
					activity.x = lane.Elements[lane.Elements.Count-1].x + 170;
					activity.y = lane.y;
				}
			}
			else
			{
				activity.x = x;
				activity.y = y;
			}
			
			lane.Elements.Add(activity);
			networkView.RPC("NewAdHocActivityAck", RPCMode.Others, "Success", player, PID, PVID, LaneID,
				activity.PrID, activity.Name, activity.x, activity.y, false);
			Console.Log.Add("<b>Added ad-hoc activity</b> to process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("NewAdHocActivityAck", RPCMode.Others, "Failure", player, PID, PVID, LaneID, -1, -1, "", (float)-1, (float)-1, false);
		}
	}
	
	[RPC]
	public void AddAdHocSubActivity(string player, int PID, int PVID, int LaneID, int ActPrID, int PrID, string name) {
		try
		{
			Activity newActivity;
			Activity activity = State.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			AdHocActivity adHocActivity = activity as AdHocActivity;
			if (PrID == -1)
				newActivity = new Activity(name, "");
			else
				newActivity = new Activity(PrID, name);
			
			if (adHocActivity.lane.Elements.Count == 0)
			{
				newActivity.x = SCREEN_WIDTH/2-235;
				newActivity.y = 80;
			}
			else if (adHocActivity.lane.Elements.Count % 4 == 0)
			{
				newActivity.x = SCREEN_WIDTH/2-235;
				newActivity.y = adHocActivity.lane.Elements[adHocActivity.lane.Elements.Count-1].y + 80;
			}
			else
			{
				newActivity.x = adHocActivity.lane.Elements[adHocActivity.lane.Elements.Count-1].x + 170;
				newActivity.y = adHocActivity.lane.Elements[adHocActivity.lane.Elements.Count-1].y;
			}
			
			adHocActivity.lane.Elements.Add(newActivity);
			networkView.RPC("AddAdHocSubActivityAck", RPCMode.Others, "Success", player, PID, PVID, LaneID,
				ActPrID, newActivity.PrID, newActivity.Name, newActivity.x, newActivity.y, false);
			Console.Log.Add("<b>Added work activity to Ad-Hoc activity</b> " + ActPrID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("AddAdHocSubActivityAck", RPCMode.Others, "Failure", player, PID, PVID, LaneID, ActPrID, -1, -1, "", -1, -1, false);
		}
	}
	
	[RPC]
	public void AddComposedSubActivity(string player, int PID, int PVID, int LaneID, int ActPrID, int PrID, string name, float x, float y)
	{
		try
		{
			Activity newActivity, activity = State.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			ComposedActivity composedActivity = activity as ComposedActivity;
			
			if (PrID == -1) newActivity = new Activity(name, "");
			else newActivity = new Activity(PrID, name);
			
			if (x == -1)
			{
				if (composedActivity.lane.Elements.Count <= 2)
				{
					newActivity.x = 230;
					newActivity.y = 168;
				}
				else
				{
					newActivity.x = composedActivity.lane.Elements[composedActivity.lane.Elements.Count-1].x + 170;
					newActivity.y = composedActivity.lane.Elements[composedActivity.lane.Elements.Count-1].y;
				}
			}
			else
			{
				newActivity.x = x;
				newActivity.y = y;
			}
			
			composedActivity.lane.Elements.Add(newActivity);
			networkView.RPC("AddComposedSubActivityAck", RPCMode.Others, "Success", player, PID, PVID, LaneID, ActPrID,
				newActivity.PrID, name, newActivity.x, newActivity.y, false);
			Console.Log.Add("<b>Added work activity to composed activity</b> " + ActPrID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("AddComposedSubActivityAck", RPCMode.Others, "Failure", player, PID, PVID, LaneID, ActPrID, -1, -1, name, -1, -1, false);
		}
	}
	
	[RPC]
	public void AddEventComposed(string player, int PID, int PVID, int LaneID, int ActPrID, int PrID, string type, float x, float y)
	{
		try
		{
			Activity activity = State.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			ComposedActivity composedActivity = activity as ComposedActivity;
			Event compEvent;
			
			if (type.Equals("Start"))
			{
				if (PrID == -1)
				{
					compEvent = new Event(Event.Categ.Start);
					compEvent.x = 100; compEvent.y = 165;
				}
				else
					compEvent = new Event(PrID, Event.Categ.Start, 100, 165);
			}
			else if (type.Equals("End"))
			{
				if (PrID == -1)
				{
					compEvent = new Event(Event.Categ.End);
					compEvent.x = SCREEN_WIDTH-150; compEvent.y = 165;
				}
				else
					compEvent = new Event(PrID, Event.Categ.Start, SCREEN_WIDTH-150, 165);
			}
			else if (type.Equals("Merge"))
			{
				if (PrID == -1) 
					compEvent = new Event(Event.Categ.Merge);
				else
					compEvent = new Event(PrID, Event.Categ.Merge, -1, -1);
				
				compEvent.x = composedActivity.lane.Elements[composedActivity.lane.Elements.Count-1].x + 100;
				compEvent.y = composedActivity.lane.Elements[composedActivity.lane.Elements.Count-1].y;
			}
			else
				compEvent = new Event();
			
			if (x != -1)
				compEvent.x = x; compEvent.y = y;
			
			composedActivity.lane.Elements.Add(compEvent);
			networkView.RPC("AddEventComposedAck", RPCMode.Others, "Success", player, PID, PVID, LaneID, ActPrID,
				compEvent.PrID, type, compEvent.x, compEvent.y, false);
			Console.Log.Add("<b>Added event to composed activity</b> " + ActPrID + ".");
			
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("AddEventComposedAck", RPCMode.Others, "Failure", player, PID, PVID, LaneID, ActPrID, -1, type, false);
		}
	}
	
	[RPC]
	public void AddConnectionComposed(string player, int PID, int PVID, int LaneID, int ActPrID, int PrID, int sourceID, int targetID, string condition)
	{
		try
		{
			Flow flow;
			Activity activity = State.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			ComposedActivity composedActivity = activity as ComposedActivity;
			
			if (PrID == -1)
				flow = new Flow(Flow.Categ.Sequence, sourceID, targetID, condition);
			else
				flow = new Flow(PrID, Flow.Categ.Sequence, sourceID, targetID, condition);
			
			composedActivity.Connections.Add(flow);
			networkView.RPC("AddConnectionComposedAck", RPCMode.Others, "Success", player, PID, PVID, LaneID, ActPrID,
				flow.PrID, sourceID, targetID, condition, false);
			Console.Log.Add("<b>Added flow</b> between sub primitives " + sourceID + " and " + targetID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("AddConnectionComposedAck", RPCMode.Others, "Failure", player, PID, PVID, LaneID, ActPrID, -1, sourceID, targetID, condition, false);
		}
	} 
	
	[RPC]
	public void NewConnection(string player, int PID, int PVID, int sourcePrID, int targetPrID, string condition, string type)
	{
		try
		{
			Process process = GetTargetProcess(PID, PVID);
			Flow flow;
			
			if (type.Equals("Sequence"))
				flow = new Flow(Flow.Categ.Sequence, sourcePrID, targetPrID, condition);
			else
				flow = new Flow(Flow.Categ.Information, sourcePrID, targetPrID, condition);
			
			process.Connections.Add(flow);
			networkView.RPC("NewConnectionAck", RPCMode.Others, "Success", player, PID, PVID, flow.PrID,
				sourcePrID, targetPrID, condition, type, false);
			if (sourcePrID != targetPrID)
				Console.Log.Add("<b>Added connection</b> between primitive " + sourcePrID + " and " + targetPrID + ".");
			else
				Console.Log.Add("<b>Added loop cycle</b> to primitive " + sourcePrID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("NewConnectionAck", RPCMode.Others, "Failure", player, PID, PVID, -1, sourcePrID, targetPrID, condition, type, false);
		}
	}
	
	[RPC]
	public void EditConnection(int PID, int PVID, int sourcePrID, int targetPrID, string condition)
	{
		try
		{
			Flow flow = State.GetFlow(PID, PVID, sourcePrID, targetPrID);
			
			flow.Condition = condition;
			networkView.RPC("EditConnectionAck", RPCMode.Others, "Success", PID, PVID, sourcePrID, targetPrID, condition);
			Console.Log.Add("<b>Edited connection</b> from process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("EditConnectionAck", RPCMode.Others, "Failure", PID, PVID, sourcePrID, targetPrID, condition);
		}
	}
	
	[RPC]
	public void EditSubFlow(int PID, int PVID, int LaneID, int ActPrID, int FlowID, string condition)
	{
		try
		{
			Flow flow = State.GetSubFlow(PID, PVID, LaneID, ActPrID, FlowID);
			
			flow.Condition = condition;
			networkView.RPC("EditSubFlowAck", RPCMode.Others, "Success", PID, PVID, LaneID, ActPrID, FlowID, condition);
			Console.Log.Add("<b>Edited sub flow</b> " + FlowID + " from composed activity " + ActPrID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("EditSubFlowAck", RPCMode.Others, "Failure", PID, PVID, LaneID, ActPrID, FlowID, condition);
		}
	}
	
	[RPC]
	public void EditActivity(int PID, int PVID, int LaneID, int PrID, string name)
	{
		try
		{
			Activity activity = State.GetActivityWithPrID(PID, PVID, LaneID, PrID);
			
			activity.Name = name;
			networkView.RPC("EditActivityAck", RPCMode.Others, "Success", PID, PVID, LaneID, PrID, name);
			Console.Log.Add("<b>Edited activity with PrimID</b> " + PrID + " from process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("EditActivityAck", RPCMode.Others, "Failure", PID, PVID, LaneID, PrID, "");
		}
	}
	
	[RPC]
	public void EditSubActivity(int PID, int PVID, int LaneID, int ActPrID, int SPrID, string name)
	{
		try
		{
			Primitive primitive = State.GetSubPrimitive(PID, PVID, LaneID, ActPrID, SPrID);
			Activity activity = primitive as Activity;
			
			activity.Name = name;
			networkView.RPC("EditSubActivityAck", RPCMode.Others, "Success", PID, PVID, LaneID, ActPrID, SPrID, name);
			Console.Log.Add("<b>Edited sub activity</b> " + SPrID + " from activity " + ActPrID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("EditSubActivityAck", RPCMode.Others, "Failure", PID, PVID, LaneID, ActPrID, SPrID, "");
		}
	}
	
	[RPC]
	public void RemovePrimitive(int PID, int PVID, int LaneID, int PrID)
	{
		try
		{
			Primitive primitive = State.GetPrimitive(PID, PVID, PrID);
			Lane lane = State.GetLane(PID, PVID, LaneID);
			
			lane.Elements.Remove(primitive);
			State.RemoveFlows(PID, PVID, PrID);
			networkView.RPC("RemovePrimitiveAck", RPCMode.Others, "Success", PID, PVID, LaneID, PrID);
			Console.Log.Add("<b>Removed primitive</b> " + PrID + " from process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("RemovePrimitiveAck", RPCMode.Others, "Failure", PID, PVID, LaneID, PrID);
		}
	}
	
	[RPC]
	public void RemoveSubPrimitive(int PID, int PVID, int LaneID, int ActPrID, int SPrID)
	{
		try
		{
			Activity activity = State.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			Primitive primitive = State.GetSubPrimitive(PID, PVID, LaneID, ActPrID, SPrID);
			
			if (activity is AdHocActivity)
			{
				AdHocActivity adHocActivity = activity as AdHocActivity;
				adHocActivity.lane.Elements.Remove(primitive);
			}
			else if (activity is ComposedActivity)
			{
				ComposedActivity composedActivity = activity as ComposedActivity;
				
				foreach (Flow flow in composedActivity.Connections)
					if (flow.SourceID == SPrID || flow.TargetID == SPrID)
						networkView.RPC("RemoveSubFlowAck", RPCMode.Others, "Success", PID, PVID, LaneID, ActPrID, flow.PrID);
				
				composedActivity.lane.Elements.Remove(primitive);
				State.RemoveSubFlows(PID, PVID, LaneID, ActPrID, SPrID);
			}
			
			networkView.RPC("RemoveSubPrimitiveAck", RPCMode.Others, "Success", PID, PVID, LaneID, ActPrID, SPrID);
			Console.Log.Add("<b>Removed sub primitive</b> " + SPrID + " from activity " + ActPrID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("RemoveSubPrimitiveAck", RPCMode.Others, "Failure", PID, PVID, LaneID, ActPrID, SPrID);
		}
	}
	
	[RPC]
	public void RemoveFlow(int PID, int PVID, int FlowID)
	{
		try
		{
			Process process = GetTargetProcess(PID, PVID);
			Flow flow = State.GetFlow(PID, PVID, FlowID);
			
			process.Connections.Remove(flow);
			networkView.RPC("RemoveFlowAck", RPCMode.Others, "Success", PID, PVID, FlowID);
			Console.Log.Add("<b>Removed flow</b> " + FlowID + " from process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("RemoveFlowAck", RPCMode.Others, "Failure", PID, PVID, FlowID);
		}
	}
	
	[RPC]
	public void RemoveSubFlow(int PID, int PVID, int LaneID, int ActPrID, int FlowID)
	{
		try
		{
			Activity activity = State.GetActivityWithPrID(PID, PVID, LaneID, ActPrID);
			ComposedActivity composedActivity = activity as ComposedActivity;
			Flow flow = State.GetSubFlow(PID, PVID, LaneID, ActPrID, FlowID);
			
			composedActivity.Connections.Remove(flow);
			networkView.RPC("RemoveSubFlowAck", RPCMode.Others, "Success", PID, PVID, LaneID, ActPrID, FlowID);
			Console.Log.Add("<b>Removed sub flow</b> " + FlowID + " from composed activity " + ActPrID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("RemoveSubFlowAck", RPCMode.Others, "Failure", PID, PVID, LaneID, ActPrID, FlowID);
		}
	}
	
	[RPC]
	public void RemoveLane(int PID, int PVID, int LaneID)
	{
		try
		{
			Process process = GetTargetProcess(PID, PVID);
			Lane lane = State.GetLane(PID, PVID, LaneID);
			
			foreach (Primitive prim in lane.Elements)
				State.RemoveFlows(PID, PVID, prim.PrID);
			
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
			
			networkView.RPC("RemoveLaneAck", RPCMode.Others, "Success", PID, PVID, LaneID);
			Console.Log.Add("<b>Removed lane</b> " + LaneID + " from process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("RemoveLaneAck", RPCMode.Others, "Failure", PID, PVID, LaneID);
		}
	}
	
	[RPC]
	public void RemoveProcess(int PID, int PVID)
	{
		try
		{
			Process process = State.GetTargetProcess(PID, PVID);
			Player processAuthor = State.ActivePlayers.Find(player => player.Username.Equals(process.Author));
			
			if (PVID == -1)
			{
				if (process.published)
					processAuthor.NumPubProcesses--;
				else
					processAuthor.NumDraftProcesses--;
					
				State.LocalProcesses.Remove(process);
			}
			else
			{
				Process originalProcess = State.GetProcess(PID);
				if (originalProcess.published)
					processAuthor.NumPubVersions--;
				else
					processAuthor.NumDraftVersions--;
				
				originalProcess.Versions.Remove(process as ProcessVersion);
			}
		
			networkView.RPC("RemoveProcessAck", RPCMode.Others, "Success", PID, PVID);
			Console.Log.Add("<b>Removed process</b> " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("RemoveProcessAck", RPCMode.Others, "Failure", PID, PVID);
		}
	}
	
	[RPC]
	public void ChangeLane(int PID, int PVID, int PrID, int LaneID)
	{
		try
		{
			State.ChangeLane(PID, PVID, PrID, LaneID);
			networkView.RPC("ChangeLaneAck", RPCMode.Others, "Success", PID, PVID, PrID, LaneID);
			Console.Log.Add("<b>Changed lane</b> to " + LaneID + " from process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("ChangeLaneAck", RPCMode.Others, "Failure", PID, PVID, PrID, LaneID);
		}
	}
	
	[RPC]
	public void RepositionPrimitive(int PID, int PVID, int PrID, float x, float y)
	{
		try
		{
			Primitive primitive = State.GetPrimitive(PID, PVID, PrID);
			
			primitive.x = x;
			primitive.y = y;
			
			networkView.RPC("RepositionPrimitiveAck", RPCMode.Others, "Success", PID, PVID, PrID, x, y);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	[RPC]
	public void RepositionSubPrimitive(int PID, int PVID, int PrID, int RPrID, float x, float y)
	{
		try
		{
			Primitive primitive = State.GetPrimitive(PID, PVID, PrID);
			
			if (primitive is AdHocActivity)
			{
				AdHocActivity adHocActivity = primitive as AdHocActivity;
				
				foreach (Primitive prim in adHocActivity.lane.Elements)
					if (prim.PrID == RPrID)
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
					if (prim.PrID == RPrID)
					{
						prim.x = x;
						prim.y = y;
						break;
					}
			}
			
			networkView.RPC("RepositionSubPrimitiveAck", RPCMode.Others, "Success", PID, PVID, PrID, RPrID, x, y);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
		}
	}
	
	/// <summary>
	/// Sends over to the clients the local processes of each player.
	/// ! May be work-intensive !
	/// </summary>
	[RPC]
	public void LoadLocalProcesses(string player)
	{
		Player playerInst = GetPlayer(player);
		
		#region Loads PROCESSES
		foreach (Process process in State.LocalProcesses)
		{
			if (process.published || process.Author.Equals(player))
				SendProcess(player, process, true);
			
			foreach (ProcessVersion version in process.Versions) 
				SendProcess(player, version, true);
		}
		#endregion
		
		foreach (Medal medal in playerInst.Medals)
			NewMedal(player, medal.categ, true);
		
		foreach (Achievement achiev in playerInst.Achievements)
			NewAchievement(player, achiev.categ, true);

		networkView.RPC("LoadLocalProcessesAck", RPCMode.Others, player);
		Console.Log.Add("<b>Loaded processes</b>.");
	}
	
	[RPC]
	public void LoadFromXml(string player, int passNumber, int numberPasses, string XmlFraction)
	{
		if (numberPasses == 1)
		{
			XmlMessage = XmlFraction;
			XmlReceived = true;
		}
		else if (passNumber == numberPasses)
		{
			XmlMessage += XmlFraction;
			XmlReceived = true;
		}
		else
			XmlMessage += XmlFraction;
		
		if (XmlReceived)
		{
			State = State.LoadFromXml(XmlMessage);
			XmlMessage = "";
			XmlReceived = false;
			LoadLocalProcesses(player);
		}
	}
	
	[RPC]
	public void NewProcessVersion(int PID, string author)
	{
		try
		{
			Process process = State.GetProcess(PID);
			ProcessVersion version = new ProcessVersion(process.lastPVID++, process, author);
			Player versionAuthor = State.ActivePlayers.Find(player => player.Username.Equals(author));
			
			process.Versions.Add(version);
			versionAuthor.NumDraftVersions++;

			DuplicateProcessToVersion(PID, version.PVID, author);
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("NewProcessVersionAck", RPCMode.Others, "Failure", PID, -1, author, false, "", -1, false, false);
		}
	}
	#endregion
	
	#region Mechanics
	[RPC]
	public void VoteQualityProcess(int PID, bool vote, string username)
	{
		try
		{
			Process process = State.GetProcess(PID);
			Player player = GetPlayer(username, State.ActivePlayers);
			process.VoteForQuality(vote, username);

			if (!player.AlreadyContainsAchievement(Achievement.Categ.VotedProcessPlayers) &&
				State.PlayerVotedProcessAllPlayers(username))
			{
				player.NewAchievement(Achievement.Categ.VotedProcessPlayers, false, false);
				NewAchievement(username, Achievement.Categ.VotedProcessPlayers, false);
			}
			
			player.NumVotesProcesses++;
			
			networkView.RPC("VoteQualityProcessAck", RPCMode.Others, "Success", PID, vote, username);
			Console.Log.Add("Player " + username + " <b>voted for quality</b> of process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("VoteQualityProcessAck", RPCMode.Others, "Failure", PID, vote, username);
		}
	}
	
	[RPC]
	public void VoteQualityVersion(string username, int PID, int PVID, bool vote)
	{
		try
		{
			Player voter = State.ActivePlayers.Find(player => player.Username.Equals(username));
			ProcessVersion version = State.GetProcessVersion(PID, PVID);
			
			version.VoteForQuality(vote, username);
			if (voter != null)
			{
				if (PVID == -1)
					voter.NumVotesProcesses++;
				else
					voter.NumVotesVersions++;
			}
			
			voter.NumVotesVersions++;
			
			networkView.RPC("VoteQualityVersionAck", RPCMode.Others, "Success", username, PID, PVID, vote);
			Console.Log.Add("Player " + username + " <b>voted for quality</b> of process version " + PVID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("VoteQualityVersionAck", RPCMode.Others, "Failure", username, PID, PVID, vote);
		}
	}
	
	[RPC]
	public void MarkAsDuplicated(int PID, int PVID, int originalPID, int originalPVID, string username)
	{
		try
		{
			Player voter = State.ActivePlayers.Find(player => player.Username.Equals(username));
			Process process = State.GetTargetProcess(PID, PVID);
			
			process.MarkAsDuplication(username, originalPID, originalPVID);
			if (voter != null)
				voter.NumDupMarks++;
			
			VoteDuplicationProcess(PID, PVID, true, username);
			if (PVID == -1)
				voter.NumVotesProcesses++;
			else
				voter.NumVotesVersions++;

			networkView.RPC("MarkAsDuplicatedAck", RPCMode.Others, "Success", PID, PVID, originalPID, originalPVID, username);
			Console.Log.Add("Player " + username + " <b>marked as duplicated</b> process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("MarkAsDuplicatedAck", RPCMode.Others, "Failure", PID, PVID, originalPID, originalPVID, username);
		}
	}
	
	[RPC]
	public void VoteDuplicationProcess(int PID, int PVID, bool vote, string username)
	{
		try
		{
			Player voter = State.ActivePlayers.Find(player => player.Username.Equals(username));
			Process process = State.GetTargetProcess(PID, PVID);
			
			process.VoteForDuplication(vote, username);
			if (voter != null)
			{
				if (PVID == -1) voter.NumVotesProcesses++;
				else voter.NumVotesVersions++;
			}
			
			if (PVID == -1) voter.NumVotesProcesses++;
			else voter.NumVotesVersions++;
			
			networkView.RPC("VoteDuplicationProcessAck", RPCMode.Others, "Success", PID, PVID, vote, username);
			Console.Log.Add("Player " + username + " <b>voted for duplication</b> process " + PID + ".");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("VoteDuplicationProcessAck", RPCMode.Others, "Failure", PID, PVID, vote, username);
		}
	}
	
	[RPC]
	public void PublishProcess(string username, int PID, int PVID)
	{
		try
		{
			Process process = GetTargetProcess(PID, PVID);
			int score = CalculateProcessScore(PID, PVID);
			Player player = GetPlayer(username, State.ActivePlayers);
			
			#region  First Process Published ACHIEVEMENT
			if (player != null && player.NumPubProcesses == 0 &&
				!player.AlreadyContainsAchievement(Achievement.Categ.ProcessPublished))
			{
				NewAchievement(username, Achievement.Categ.ProcessPublished, false);
				player.NewAchievement(Achievement.Categ.ProcessPublished, false, false);
			}
			#endregion
			#region Corrected Own Process ACHIEVEMENT
			if (player != null && State.PlayerCorrectedOwnProcess(username) &&
				!player.AlreadyContainsAchievement(Achievement.Categ.CorrectedOwnProcess))
			{
				NewAchievement(username, Achievement.Categ.CorrectedOwnProcess, false);
				player.NewAchievement(Achievement.Categ.CorrectedOwnProcess, false, false);
			}
			#endregion
			#region Corrected Processes All Players ACHIEVEMENT
			if (PVID != -1 && player != null && !player.AlreadyContainsAchievement(Achievement.Categ.ProposedVersion) &&
				State.PlayerModifProcessAllPlayers(username))
			{
				player.NewAchievement(Achievement.Categ.ProposedVersion, false, false);
				NewAchievement(username, Achievement.Categ.ProposedVersion, false);
			}
			#endregion
			#region First Process Published MEDAL
			if (State.GetNumPublishedProcesses() == 0)
			{
				NewMedal(username, Medal.Categ.FirstProcessPublished, false);
				player.NewMedal(Medal.Categ.FirstProcessPublished, false, false);
			}
			#endregion

			float poissonFactor = (float)(process.score *
				State.CalculateModPoissonValue(process.CalculateNumberActivities(),
					int.Parse(PaintServerPanels.ExpectedNumActivities)));
			
			process.published = true;
			process.score += score;
			process.bonusMalus[0] = poissonFactor;
			player.Score += score;
			
			if (PVID == -1)
			{
				networkView.RPC("UpdateMode", RPCMode.Others, true);
				SendProcess(username, process, false);
				networkView.RPC("UpdateMode", RPCMode.Others, false);
				player.NumPubProcesses++;
				player.NumDraftProcesses--;
			}
			else
			{
				player.NumPubVersions++;
				player.NumDraftVersions--;
			}
			
			networkView.RPC("PublishProcessAck", RPCMode.Others, "Success", username, PID, PVID, score);
			Console.Log.Add("Player " + username + " published process " + PID + ", scoring " + score + " points.");
		}
		catch (InvalidOperationException ioe)
		{
			Debug.Log(ioe.StackTrace);
			networkView.RPC("PublishProcessAck", RPCMode.Others, "Failure", username, PID, PVID, -1);
		}
	}
	
	[RPC]
	public void SignalGameTimeout(string username)
	{
		Player player = GetPlayer(username);
		player.GameOver = true;
	}
	#endregion

	#endregion
}
