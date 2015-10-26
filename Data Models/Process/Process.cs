using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class Process
{	
	public static int IDCounter = 0;
	[XmlAttribute("PID")]
	public int PID { get; private set; }
	public int lastPVID = 1;
	
	[XmlArray("Pool"), XmlArrayItem("Lane")]
	public List<Lane> Pool;
	[XmlArray("Flows"), XmlArrayItem("Flow")]
	public List<Flow> Connections;
	[XmlArray("QualityVotes"), XmlArrayItem("QualVote")]
	public List<Vote> QualityVotes;
	public float ppv = 0;
	[XmlArray("DuplicationVotes"), XmlArrayItem("DupVote")]
	public List<Vote> DuplicationVotes;
	[XmlArray("ProcessVersions"), XmlArrayItem("ProcessVersion")]
	public List<ProcessVersion> Versions;
	
	public int posVotes, negVotes;
	public int posDuplicationVotes, negDuplicationVotes;
	[XmlAttribute("Name")]
	public string Name;
	[XmlAttribute("Description")]
	public string Description;
	[XmlAttribute("Author")]
	public string Author;
	[XmlAttribute("Published")]
	public bool published;
	public bool markedDuplication;
	public string markAuthor;
	[XmlAttribute("OriginalPID")]
	public int duplicationPID;
	[XmlAttribute("OriginalPVID")]
	public int duplicationPVID;
	public int score;
	[XmlAttribute("FinalConsensus")]
	public bool finalConsensus;
	
	/// <summary>
	/// Bonus-Malus system:
	/// [0] - Poisson Expected Value
	/// [1] - Positive VR
	/// [2] - Marked Duplicate
	/// [3] - Best VR in Process Tree 
	/// </summary>
	public float[] bonusMalus;
	
	public long timeStamp;
	
	public Process()
	{
		InitializeProcess();
		PID = System.Threading.Interlocked.Increment(ref IDCounter);
	}
	
	public Process(string name, string descrip)
	{
		InitializeProcess();
		PID = System.Threading.Interlocked.Increment(ref IDCounter);
		Name = name;
		Description = descrip;
	}
	
	public Process(int pid, string name, string descrip)
	{
		InitializeProcess();
		PID = pid;
		Name = name;
		Description = descrip;
	}
	
	public Process(int originalPID)
	{
		InitializeProcess();
		PID = originalPID;
	}
	
	public void InitializeProcess()
	{
		Pool = new List<Lane>();
		Connections = new List<Flow>();
		QualityVotes = new List<Vote>();
		DuplicationVotes = new List<Vote>();
		Versions = new List<ProcessVersion>();
		Name = Description = "";
		posVotes = negVotes = posDuplicationVotes = negDuplicationVotes = 0;
		markedDuplication = false;
		Author = markAuthor = "";
		timeStamp = DateTime.UtcNow.Ticks;
		bonusMalus = new float[4] { 0, 0, 0, 0 };
	}
	
	public int GetNextPVID() 
	{
		int LastPVID = Versions.Count == 0 ? 1 : (Versions[Versions.Count-1]).PVID;	
		return LastPVID;
	}
	
	public void VoteForQuality(bool type, string username)
	{
		foreach (Vote qv in QualityVotes)
			if (qv.GetVoter().Equals(username))
				throw new InvalidOperationException("Username already voted for the process.");

		Vote vote = new Vote(type, username);
		QualityVotes.Add(vote);
		
		if (type)
			posVotes++;
		else
			negVotes++;
	}
	
	public void MarkAsDuplication(string username, int originalPID, int originalPVID)
	{
		markedDuplication = true;
		markAuthor = username;
		duplicationPID = originalPID;
		duplicationPVID = originalPVID;
	}
	
	public void VoteForDuplication(bool type, string username)
	{
		foreach (Vote dv in DuplicationVotes)
			if (dv.GetVoter().Equals(username))
				throw new InvalidOperationException("Username already voted for the process duplication.");
		
		Vote vote = new Vote(type, username);
		DuplicationVotes.Add(vote);
		
		if (type)
			posDuplicationVotes++;
		else
			negDuplicationVotes++;
	}
	
	public bool PlayerAlreadyVoted(string username, string type)
	{
		if (type.Equals("Quality"))
		{
			foreach (Vote qv in QualityVotes)
				if (qv.GetVoter().Equals(username))
					return true;
		}
		else if (type.Equals("Duplication"))
		{
			foreach (Vote dv in DuplicationVotes)
				if (dv.GetVoter().Equals(username))
					return true;
		}
		else
			throw new InvalidOperationException("Unknown vote type.");
		
		return false;
	}
	
	public bool GetPlayerVote(string username, string type)
	{
		if (type.Equals("Quality"))
		{
			foreach (Vote qv in QualityVotes)
				if (qv.GetVote())
					return true;
		}
		else if (type.Equals("Duplication"))
		{
			foreach (Vote dv in DuplicationVotes)
				if (dv.GetVote())
					return true;
		}
		else
			throw new InvalidOperationException("Unknown vote type.");
		
		return false;
	}
	
	public bool Validate()
	{
		bool invalid = false;
		List<Primitive> AllPrimitives = new List<Primitive>();
		
		foreach (Lane lane in Pool)
			AllPrimitives.AddRange(lane.Elements);
		
		#region Erase previous calculated cache
		foreach (Primitive prim in AllPrimitives)
		{
			prim.Sources.Clear();
			prim.Targets.Clear();
			
			if (prim is ComposedActivity) {
				foreach (Primitive subPrim in (prim as ComposedActivity).lane.Elements) {
					subPrim.Sources.Clear();
					subPrim.Targets.Clear();
				}
			}
		}
		#endregion
		
		FillFlowCache();
		
		foreach (Primitive prim in AllPrimitives)
		{
			if (!ValidateFlows(prim))
				invalid = true;
		}
		
		if (Pool.Count == 1 && Pool[0].Elements.Count == 2)
		{
			Painter.Manager.Notifications.Items.Add(new Notification(Notification.Type.Exception,
				"The process cannot be empty."));
			invalid = true;
		}
		if (Connections.Count == 0)
		{
			Painter.Manager.Notifications.Items.Add(new Notification(Notification.Type.Exception,
				"The process must contain sequence flows."));
			invalid = true;
		}
		if (Name.Equals(""))
		{
			Painter.Manager.Notifications.Items.Add( new Notification(Notification.Type.Exception,
				"The process must be named."));
			invalid = true;
		}
		
		if (invalid) return false;
		else return true;
	}
	
	private void FillFlowCache()
	{
		foreach (Flow flow in Connections)
		{
			if (flow.SourceID == flow.TargetID)
				continue;
			Primitive source, target;
			if (Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
				Painter.Manager.CurrentScreen == GameManager.GameScreen.AdHocCreation || 
				Painter.Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation)
			{
				source = Painter.Manager.GameState.GetPrimitive(PID, -1, flow.SourceID);
				target = Painter.Manager.GameState.GetPrimitive(PID, -1, flow.TargetID);
			}
			else
			{
				ProcessVersion version = Painter.Manager.CurrentVersion;
				source = Painter.Manager.GameState.GetPrimitive(version.OriginalPID, version.PVID, flow.SourceID);
				target = Painter.Manager.GameState.GetPrimitive(version.OriginalPID, version.PVID, flow.TargetID);
			}
			
			if (source is ComposedActivity)
				FillFlowCache(source.PrID);
			if (target is ComposedActivity)
				FillFlowCache(target.PrID);
			
			source.Targets.Add(target);
			target.Sources.Add(source);
		}
	}
	
	private void FillFlowCache(int PrID)
	{
		int PVID = (Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
				Painter.Manager.CurrentScreen == GameManager.GameScreen.AdHocCreation || 
				Painter.Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation) ? -1 : Painter.Manager.CurrentVersion.PVID;
		int laneID = Painter.Manager.GameState.GetLaneID(PID, PVID, PrID);
		ComposedActivity activity = Painter.Manager.GameState.GetActivityWithPrID(Painter.Manager.CurrentProcess.PID, PVID, laneID, PrID) as ComposedActivity;
		
		foreach (Flow subFlow in activity.Connections)
		{
			if (subFlow.SourceID == subFlow.TargetID)
				continue;
			
			int LaneID = Painter.Manager.GameState.GetLaneID(PID, PVID, activity.PrID);
			Primitive subFlowSource = Painter.Manager.GameState.GetSubPrimitive(PID, PVID, LaneID, activity.PrID, subFlow.SourceID);
			Primitive subFlowTarget = Painter.Manager.GameState.GetSubPrimitive(PID, PVID, LaneID, activity.PrID, subFlow.TargetID);
			
			subFlowSource.Targets.Add(subFlowTarget);
			subFlowTarget.Sources.Add(subFlowSource);
		}
	}
	
	private bool ValidateFlows(Primitive prim)
	{
		if (prim is Event && (prim as Event).categ.Equals(Event.Categ.Start))
		{
			Event start = prim as Event;
			if (start.Sources.Count != 0)
			{
				Painter.Manager.Notifications.Items.Add(
					new Notification(Notification.Type.Exception,
					"Start events must have no source primitives."));
				return false;
			}
		}
		else if (prim is Event && (prim as Event).categ.Equals(Event.Categ.End))
		{
			Event end = prim as Event;
			if (end.Targets.Count != 0)
			{
				Painter.Manager.Notifications.Items.Add(
					new Notification(Notification.Type.Exception,
					"End events must have no target primitives."));
				return false;
			}
		}
		else if (prim is Event && (prim as Event).categ.Equals(Event.Categ.Merge))
		{
			Event merge = prim as Event;
			if (merge.Sources.Count == 0 || merge.Targets.Count == 0)
			{
				Painter.Manager.Notifications.Items.Add(
					new Notification(Notification.Type.Exception,
					"Merge events must be connected by sequence flows."));
				return false;
			}
		}
		else if (prim is Activity)
		{
			Activity act = prim as Activity;
			
			if (act.Targets.Count == 0 || act.Sources.Count == 0)
			{
				Painter.Manager.Notifications.Items.Add(
					new Notification(Notification.Type.Exception,
					"Activity <i>\"" + act.Name + "\"</i> must be connected by sequence flows."));
				return false;
			}
			
			if (prim is ComposedActivity)
			{
				ComposedActivity compAct = prim as ComposedActivity;
				foreach (Primitive subComp in compAct.lane.Elements)
					if (!ValidateFlows(subComp)) return false;
			}
		}
		
		return true;
	}
	
	public int CalculateNumberActivities()
	{
		int numActiv = 0;
		
		foreach (Lane lane in Pool) {
			foreach (Primitive prim in lane.Elements)
			{
				if (prim is AdHocActivity)
				{
					foreach(Primitive subPrim in (prim as AdHocActivity).lane.Elements)
						numActiv++;
				}
				else if (prim is ComposedActivity)
					foreach (Primitive subPrim in (prim as ComposedActivity).lane.Elements)
					{
						if (subPrim is Activity)
							numActiv++;
					}
				else if (prim is Activity)
					numActiv++;
			}
		}
		
		return numActiv;
	}
	
	public float CalculateConvergenceRate(string username)
	{
		float ConvRateQuality = CalculateConvRateQuality(username);
		float ConvRateDuplication = CalculateConvRateDuplication(username);
		
		if (ConvRateQuality == -1 && ConvRateDuplication == -1)
			return -1;
		
		else if (ConvRateQuality == -1)
			return ConvRateDuplication;
		
		else if (ConvRateDuplication == -1)
			return ConvRateQuality;
		
		else
			return (ConvRateQuality + ConvRateDuplication)/2;
	}
	
	public float CalculateConvRateQuality(string username)
	{
		foreach (Vote vote in QualityVotes)
			if (vote.GetVoter().Equals(username))
			{
				if (vote.GetVote())
					return posVotes / QualityVotes.Count;
				else
					return negVotes / QualityVotes.Count;
			}
		
		return -1;
	}
	
	public float CalculateConvRateDuplication(string username)
	{
		foreach (Vote vote in DuplicationVotes)
			if (vote.GetVoter().Equals(username))
			{
				if (vote.GetVote())
					return posDuplicationVotes / DuplicationVotes.Count;
				else
					return negDuplicationVotes / DuplicationVotes.Count;
			}
		
		return -1;
	}
	
	public float GetQualityVoteRate()
	{
		return (posVotes == 0) ? 0 :
			(posVotes/(posVotes+negVotes));
	}
	
	public bool IsDuplication()
	{
		return (markedDuplication && posDuplicationVotes > negDuplicationVotes);
	}

	public float GetTotalScore()
	{
		return score + bonusMalus[0] + bonusMalus[1] + bonusMalus[2] + bonusMalus[3];
	}
	
	/// <summary>
	/// Returns a <see cref="System.String"/> that represents the current <see cref="Process"/>.
	/// </summary>
	/// <returns>
	/// A <see cref="System.String"/> that represents the current <see cref="Process"/>.
	/// </returns>
	public override string ToString() { return "Process " + PID; }
}

