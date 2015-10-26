using System;

[Serializable]
public class Achievement
{	
	public enum Categ { Default, ProcessPublished, ProposedVersion,
		VotedProcessPlayers, CorrectedOwnProcess };
	public Categ categ { get; private set; }
	public string Name { get; private set; }
	public int ScorePoints { get; private set; }
	
	public Achievement()
	{
		categ = Categ.Default;
		Name = "";
		ScorePoints = 0;
	}
	
	public Achievement(Categ type)
	{
		categ = type;
		
		switch (categ)
		{
			case Categ.ProcessPublished:
				Name = "First process published";
				ScorePoints = 20;
				break;
			case Categ.ProposedVersion:
				Name = "Proposed modifications to all player's processes.";
				ScorePoints = 20;
				break;
			case Categ.VotedProcessPlayers:
				Name = "Voted processes from all other players.";
				ScorePoints = 20;
				break;
			case Categ.CorrectedOwnProcess:
				Name = "Corrected a process previously created.";
				ScorePoints = 20;
				break;
		}
	}
	
	public static Categ GetCateg(string type)
	{
		Categ ret = Categ.Default;
		
		if (type.Equals("ProcessPublished"))
			ret = Achievement.Categ.ProcessPublished;
		else if (type.Equals("ProposedVersion"))
			ret = Achievement.Categ.ProposedVersion;
		else if (type.Equals("VotedProcessPlayers"))
			ret = Achievement.Categ.VotedProcessPlayers;
		else if (type.Equals("CorrectedOwnProcess"))
			ret = Achievement.Categ.CorrectedOwnProcess;
		
		return ret;
	}
}

