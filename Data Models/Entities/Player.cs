using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

[Serializable]
public class Player
{	
	private static int IDCounter = 0;
	[XmlAttribute("ID")]
	public int PlayerID;
	public string Username;
	public string Password;
	public float Score;
	public int NumDraftProcesses, NumDraftVersions;
	public int NumPubProcesses, NumPubVersions;
	public int AvgProcessScore, AvgVersionScore;
	public int AvgProcessActiv, AvgVersionActiv;
	public int NumDupProcesses, NumDupVersions;
	public int NumDupMarks;
	public int NumVotesProcesses, NumVotesVersions;
	public float ConvRateProcesses, ConvRateVersions;
	public float ConvergenceRate;
	[XmlIgnore]
	public bool Online;
	
	public List<Achievement> Achievements { get; private set; }
	public List<Medal> Medals { get; private set; }
	
	/// <summary>
	/// Bonus-Malus system:
	/// [0] - Poisson Expected Value
	/// [1] - Voting
	/// [2] - Duplication Detection
	/// </summary>
	public float[] bonusMalus;
	public bool GameOver;
	
	public Player()
	{
		PlayerID = System.Threading.Interlocked.Increment(ref IDCounter);
		Username = "";
		Password = "";
		Achievements = new List<Achievement>();
		Medals = new List<Medal>();
		bonusMalus = new float[3] { 0, 0, 0 };
	}
	
	public Player(string user, string pass)
	{
		PlayerID = System.Threading.Interlocked.Increment(ref IDCounter);
		Username = user;
		Password = pass;
		Achievements = new List<Achievement>();
		Medals = new List<Medal>();
		bonusMalus = new float[3] { 0, 0, 0 };
	}
	
	public void Reset()
	{
		Score = NumDraftProcesses = NumPubProcesses = NumPubVersions = NumDraftVersions = AvgProcessScore = AvgVersionScore =
			AvgProcessActiv = AvgVersionActiv = NumDupProcesses = NumDupVersions = NumDupMarks = NumVotesProcesses =
			NumVotesVersions = 0;
		ConvRateProcesses = ConvRateVersions = 0f;
		
		Medals.Clear();
		Achievements.Clear();
		bonusMalus = new float[3] { 0, 0, 0 };
	}
	
	public void NewAchievement(Achievement.Categ type, bool client, bool updateMode)
	{
		Achievement achiev = new Achievement(type);
		string notifText = "";
		
		Achievements.Add(achiev);
		switch (type)
		{
			case Achievement.Categ.ProcessPublished:
				notifText = "First process you published.";
				break;
			case Achievement.Categ.ProposedVersion:
				notifText = "Proposed modifications to all player's processes.";
				break;
			case Achievement.Categ.VotedProcessPlayers:
				notifText = "Voted processes from all other players.";
				break;
			case Achievement.Categ.CorrectedOwnProcess:
				notifText = "Corrected a process previously created.";
				break;
		}
		
		Score += achiev.ScorePoints;
		
		if (client && !updateMode)
			Painter.Manager.Notifications.Items.Add(new Notification(Notification.Type.Achievement, notifText));
	}
	
	public bool AlreadyContainsMedal(Medal.Categ type)
	{
		foreach (Medal medal in Medals)
			if (medal.categ.Equals(type))
				return true;
		
		return false;
	}
	
	public bool AlreadyContainsAchievement(Achievement.Categ type)
	{
		foreach (Achievement achiev in Achievements)
			if (achiev.categ.Equals(type))
				return true;
		
		return false;
	}
	
	public void NewMedal(Medal.Categ type, bool client, bool updateMode)
	{
		Medal medal = new Medal(type);
		string notifText = "";
		
		Medals.Add(medal);
		switch (type)
		{
			case Medal.Categ.FirstProcessPublished:
				notifText = "First process published in the game.";
				break;
			case Medal.Categ.MarkedProcessDuplicated:
				notifText = "Marked a process as a duplicate correctly.";
				break;
			case Medal.Categ.BestQualityContent:
				notifText = "Created process with best quality vote rate.";
				break;
			case Medal.Categ.BestVoteConvergence:
				notifText = "Best convergence in votes placed.";
				break;
		}
		
		Score += medal.ScorePoints;
		
		if (client && !updateMode)
			Painter.Manager.Notifications.Items.Add(new Notification(Notification.Type.Medal, notifText));
	}
	
	public float GetConvergenceVoteRate()
	{
		return (ConvRateProcesses + ConvRateVersions) / ((ConvRateProcesses == 0 || ConvRateVersions == 0) ? 1 : 2);
	}
	
	public int GetAchievementsScore()
	{
		int score = 0;
		
		foreach (Achievement ach in Achievements)
			score += ach.ScorePoints;
		
		return score;
	}
	
	public int GetMedalsScore()
	{
		int score = 0;
		
		foreach (Medal med in Medals)
			score += med.ScorePoints;
		
		return score;
	}
	
}