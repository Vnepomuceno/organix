using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("GameState")]
public class ServerState
{	
	[XmlArray("Processes"), XmlArrayItem("Process")]
	public List<Process> LocalProcesses;
	[XmlArray("ActivePlayers"), XmlArrayItem("Player")]
	public List<Player> ActivePlayers;
	[XmlIgnore]
	public RegPlayersContainer RegisteredPlayers;
	public bool GameOver = false;

	public ServerState() {
		LocalProcesses = new List<Process>();
		ActivePlayers = new List<Player>();
		RegisteredPlayers = new RegPlayersContainer();
		RegisteredPlayers = RegisteredPlayers.LoadXML();
	}

	public Event ev;
	public Activity ac;
	public ComposedActivity cAc;
	public AdHocActivity ahAc;
	
	public int GetNumPublishedProcesses()
	{
		int n = 0;
		
		foreach (Process process in LocalProcesses)
			if (process.published)
				n++;
		
		return n;
	}
	
	/// <summary>
	/// Gets the process.
	/// </summary>
	/// <returns>
	/// The process.
	/// </returns>
	/// <param name='PID'>
	/// PI.
	/// </param>
	/// <exception cref='InvalidOperationException'>
	/// Is thrown when an operation cannot be performed.
	/// </exception>
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
		
		if (version == null)
			throw new InvalidOperationException("Process Version " + PVID + " could not be found.");
		else
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
	
	public Activity GetActivity(int PID, int PVID, int PrID)
	{
		Activity activity = null;
		Process process = GetTargetProcess(PID, PVID);
		
		foreach(Lane lane in process.Pool)
		{
			foreach (Primitive prim in lane.Elements)
				if (prim is Activity && prim.PrID == PrID)
				{
					activity = prim as Activity;
					break;
				}
		}
		
		if (activity == null)
			throw new InvalidOperationException("Activity with PrID " + PrID + " could not be found.");
		else
			return activity;
	}
	
	
	public Activity GetActivityWithPrID(int PID, int PVID, int LaneID, int PrID)
	{
		Lane lane = GetLane(PID, PVID, LaneID);
		Activity activity = null;
		
		foreach (Primitive prim in lane.Elements)
			if ((prim is Activity) &&
			((Activity)prim).PrID == PrID)
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
	
	public Flow GetFlow(int PID, int PVID, int SourcePrID, int TargetPrID)
	{
		Flow flow = null;
		Process process = GetTargetProcess(PID, PVID);
		
		foreach (Flow f in process.Connections)
			if (f.SourceID == SourcePrID && f.TargetID == TargetPrID)
			{
				flow = f;
				break;
			}
		
		if (flow == null)
			throw new InvalidOperationException("Flow connecting primitives " + SourcePrID + " and " + TargetPrID + " could not be found.");
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
		foreach (Player player in ActivePlayers)
			player.Reset();
		ActivePlayers.Clear();
		Process.IDCounter = 0;
		ProcessVersion.IDCounter = 0;
		Activity.IDCounter = 0;
		GameOver = false;
	}
	
	/// <summary>
	/// Saves the xml.
	/// </summary>
	/// <param name='path'>
	/// Path.
	/// </param>
	public void SaveXml()
	{
		StringWriter writer = new StringWriter();
		XmlSerializer serializer = new XmlSerializer(typeof(ServerState));
		serializer.Serialize(writer, this);
		PlayerPrefs.SetString("XmlServerState", writer.ToString());
	}
	
	/// <summary>
	/// Loads the xml.
	/// </summary>
	/// <returns>
	/// The xml.
	/// </returns>
	/// <param name='path'>
	/// Path.
	/// </param>
	public ServerState LoadXml()
	{
		ServerState state;
		XmlSerializer serializer = new XmlSerializer(typeof(ServerState));
		
		state = serializer.Deserialize(new StringReader(PlayerPrefs.GetString("XmlServerState"))) as ServerState;

		if (state.ActivePlayers.Count == 0)
			foreach (Player player in state.RegisteredPlayers.Players)
				state.ActivePlayers.Add(player);
		
		foreach (Player player in state.ActivePlayers)
			player.Online = false;
		
		return state;
	}
	
	public ServerState LoadFromXml(string xmlDocument)
	{
		string xml = xmlDocument;
		XmlSerializer serializer = new XmlSerializer(typeof(ServerState));
		return serializer.Deserialize(new StringReader(xml)) as ServerState;
		
	}
	
	public float CalculateConvergenceRate(string username)
	{
		float ConvergenceRate = 0;
		int NumProcesses = 0;
		Player player = GetPlayer(username, Server.State.ActivePlayers);
		if (player == null) return -1;
		
		foreach (Process process in LocalProcesses)
		{
			float ConvRate = process.CalculateConvergenceRate(username);
			if (ConvRate != -1)
			{
				ConvergenceRate += ConvRate;
				NumProcesses++;
			}
		}
		
		return ConvergenceRate == 0 ? 0 : (float)ConvergenceRate / NumProcesses;
	}
	
	public Process GetTargetProcess(int PID, int PVID)
	{
		Process process;
		
		if (PVID == -1) process = GetProcess(PID);
		else process = GetProcessVersion(PID, PVID);
		
		return process;
	}
	
	public Player GetPlayer(string username, List<Player> list)
	{
		foreach (Player p in list)
			if (p.Username.Equals(username))
				return p;
		
		return null;
	}
	
	public bool PlayerModifProcessAllPlayers(string username)
	{
		List<Player> players = GetActivePlayers();
		
		if (players.Count == 1) return false;
		
		foreach (Process process in LocalProcesses)
			if (!process.Author.Equals(username))
				foreach (ProcessVersion version in process.Versions)
					if (version.Author.Equals(username))
					{
						Player p = GetPlayer(process.Author, Server.State.ActivePlayers);
						players.Remove(p);
					}
		
		return (players.Count == 1);
	}
	
	public bool PlayerVotedProcessAllPlayers(string username)
	{
		List<Player> players = GetActivePlayers();
		
		if (players.Count == 1) return false;
		
		foreach (Process process in LocalProcesses)
			if (!process.Author.Equals(username))
				foreach (Vote vote in process.QualityVotes)
					if (vote.GetVoter().Equals(username))
					{
						Player p = GetPlayer(process.Author, Server.State.ActivePlayers);
						players.Remove(p);
					}
		
		return (players.Count == 1);
	}
	
	public bool PlayerCorrectedOwnProcess(string username)
	{
		foreach (Process process in LocalProcesses)
			if (process.Author.Equals(username))
				foreach (ProcessVersion version in process.Versions)
					if (version.Author.Equals(username))
						return true;
		
		return false;
	}
	
	public List<Player> GetActivePlayers()
	{
		List<Player> ret = new List<Player>();
		
		foreach (Player p in Server.State.ActivePlayers)
			ret.Add(p);
		
		return ret;
	}
	
	
	/***********************************/
	/*         SCORING SYSTEM          */
	/***********************************/
	
	public void CalculateFinalPlayerScore(Player player)
	{
		foreach (Process process in LocalProcesses)
		{
			CalculateProcessAuthorshipScore(process, player);
			
			foreach (ProcessVersion version in process.Versions)
				CalculateVersionAuthorshipScore(version, player);
			
			CalculateProcessVotesScore(process, player);
			CalculateMarkAsDuplicateScore(process, player);
			CalculateVotingAbstention(process, player);
		}
	}
	
	private void CalculateProcessAuthorshipScore(Process process, Player author)
	{
		if (process.Author.Equals(author.Username))
		{
			// Expected number of activities (Poisson Distribution)	
			float poissonFactor = (float)(process.score *
				CalculateModPoissonValue(process.CalculateNumberActivities(),
					int.Parse(PaintServerPanels.ExpectedNumActivities)));
			
			author.bonusMalus[0] += poissonFactor;
			process.bonusMalus[0] = poissonFactor;
			
			// Quality voting
			float votingFactor = 0;
			
			if (process.QualityVotes.Count == 0)
				votingFactor = 0;
			else if (process.posVotes > process.negVotes)
				votingFactor = process.score*(((float)process.posVotes/process.QualityVotes.Count)/4);
			else
				votingFactor = -process.score*(((float)process.negVotes/process.QualityVotes.Count)/4);
			
			author.bonusMalus[0] += votingFactor;
			process.bonusMalus[1] = votingFactor;
			
			// Process marked as a duplicate
			float duplicationFactor = 0;
			
			if (process.markedDuplication && (process.posDuplicationVotes > process.negDuplicationVotes))
				duplicationFactor = - process.score*0.25f;
			else if (process.markedDuplication && (process.posDuplicationVotes < process.negDuplicationVotes))
				duplicationFactor = process.score*0.1f;
			
			author.bonusMalus[0] += duplicationFactor;
			process.bonusMalus[2] = duplicationFactor;
			
			// Best Percentage of Positive Votes (PPV) in process tree
			int PVID = 0;
			float bestPPV = 0f;
			float bestVoteRateFactor = 0;
			foreach (ProcessVersion version in process.Versions)
				if (version.QualityVotes.Count != 0 &&
				    (version.posVotes/version.QualityVotes.Count) > bestPPV)
				{
					bestPPV = version.posVotes/version.QualityVotes.Count;
					PVID = version.PVID;
				}
			
			if (PVID == 0)
				bestVoteRateFactor = process.score*0.1f;
			else
				bestVoteRateFactor = - process.score*0.05f;
			
			author.bonusMalus[0] += bestVoteRateFactor;
			process.bonusMalus[3] = bestVoteRateFactor;
		}
	}
	
	private void CalculateVersionAuthorshipScore(ProcessVersion version, Player author)
	{
		if (version.Author.Equals(author.Username))
		{
		// Expected number of activities (Poisson Distribution)
			float poissonFactor = (float)(version.score *
				CalculateModPoissonValue(version.CalculateNumberActivities(),
					int.Parse(PaintServerPanels.ExpectedNumActivities)));
			
			author.bonusMalus[0] += poissonFactor;
			version.bonusMalus[0] = poissonFactor;
			
			// Quality voting
			float votingFactor = 0;
			
			if (version.QualityVotes.Count == 0)
				votingFactor = 0;
			else if (version.posVotes > version.negVotes)
				votingFactor = version.score*(((float)version.posVotes/version.QualityVotes.Count)/2);
			else
				votingFactor = -version.score*(((float)version.negVotes/version.QualityVotes.Count)/2);
			
			author.bonusMalus[0] += votingFactor;
			version.bonusMalus[1] = votingFactor;
			
			// Process marked as a duplicate
			float duplicationFactor = 0;
			if (version.markedDuplication && (version.posDuplicationVotes > version.negDuplicationVotes))
				duplicationFactor = - version.score*0.5f;
			else if (version.markedDuplication && (version.posDuplicationVotes <= version.negDuplicationVotes))
				duplicationFactor = version.score*0.1f;
			
			author.bonusMalus[0] += duplicationFactor;
			version.bonusMalus[2] = duplicationFactor;
		}
	}
	
	private void CalculateProcessVotesScore(Process process, Player voter)
	{
		CalculateVoteScoreList(process, voter, "Quality");
		CalculateVoteScoreList(process, voter, "Duplication");
		
		foreach (ProcessVersion version in process.Versions)
		{
			CalculateVoteScoreList(version, voter, "Quality");
			CalculateVoteScoreList(version, voter, "Duplication");
		}
	}
	
	private void CalculateVoteScoreList(Process process, Player voter, string votingType)
	{
		if (votingType.Equals("Quality"))
		{
			foreach (Vote qualVote in process.QualityVotes)
			{
				if (qualVote.GetVoter().Equals(voter.Username))
				{
					voter.bonusMalus[1] += 10;
					
					if (IsConvergentVote(qualVote, process, "Quality"))
						voter.bonusMalus[1] += 10;
					else
						voter.bonusMalus[1] -= 5;
					
					continue;
				}
			}
		}
		else
		{
			foreach (Vote dupVote in process.DuplicationVotes)
			{
				if (dupVote.GetVoter().Equals(voter.Username))
				{
					voter.bonusMalus[1] += 10;
					
					if (IsConvergentVote(dupVote, process, "Duplication"))
						voter.bonusMalus[1] += 10;
					else
						voter.bonusMalus[1] -= 5;
					
					continue;
				}
			}
		}
	}
	
	private void CalculateVotingAbstention(Process process, Player player)
	{
		if (process.Author.Equals(player.Username)) return;
		
		if (!PlayerVoted(player, process.QualityVotes))
			player.bonusMalus[1] -= 10;
		if (!PlayerVoted(player, process.DuplicationVotes))
			player.bonusMalus[1] -= 25;
		
		foreach (ProcessVersion version in process.Versions)
		{
			if (!PlayerVoted(player, version.QualityVotes))
				player.bonusMalus[1] -= 10;
			if (!PlayerVoted(player, version.DuplicationVotes))
				player.bonusMalus[1] -= 25;
		}
	}
	
	private bool PlayerVoted(Player player, List<Vote> voteList)
	{
		foreach (Vote vote in voteList)
			if (vote.GetVoter().Equals(player.Username))
				return true;
		
		return false;
	}
	
	private bool IsConvergentVote(Vote vote, Process process, string votingType)
	{
		if (votingType.Equals("Quality"))
		{
			if ((vote.GetVote() && (process.posVotes > process.negVotes)) ||
				(!vote.GetVote() && (process.posVotes < process.negVotes)))
				return true;
		}
		else
		{
			if ((vote.GetVote() && (process.posDuplicationVotes > process.negDuplicationVotes)) ||
				(!vote.GetVote() && (process.posDuplicationVotes < process.negDuplicationVotes)))
				return true;
		}
		
		return false;
	}
	
	private void CalculateMarkAsDuplicateScore(Process process, Player voter)
	{
		if (process.markedDuplication && process.markAuthor.Equals(voter.Username))
		{
			if (process.posDuplicationVotes > process.negDuplicationVotes)
				voter.bonusMalus[2] += process.score*0.2f;
			else if (process.posDuplicationVotes < process.negDuplicationVotes)
				voter.bonusMalus[2] -= process.score*0.1f;
			else
				voter.bonusMalus[2] += process.score*0.05f;
		}

		foreach (ProcessVersion version in process.Versions)
		{
			if (version.markedDuplication && version.markAuthor.Equals(voter.Username))
			{
				if (version.posDuplicationVotes > version.negDuplicationVotes)
					voter.bonusMalus[2] += version.score*0.2f;
				else if (version.posDuplicationVotes < version.negDuplicationVotes)
					voter.bonusMalus[2] -= version.score*0.1f;
				else
					voter.bonusMalus[2] += version.score*0.05f;
			}
		}
	}

	public double CalculatePoissonValue(int numberActivities, int expectedValue)
	{
		double f = 1;
		double r;
		
		r = Math.Pow(expectedValue, numberActivities)*Math.Exp(-expectedValue);
		
		while (numberActivities > 0)
		{
			f = f*numberActivities;
			numberActivities--;
		}
			
		return (r/f);
	}

	public double CalculateModPoissonValue(int numberActivities, int expectedValue)
	{
		return ((2 * CalculatePoissonValue(numberActivities, expectedValue)) - CalculatePoissonValue(expectedValue, expectedValue));
	}
	
}
