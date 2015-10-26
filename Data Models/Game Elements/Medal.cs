using System;

[Serializable]
public class Medal
{	
	public enum Categ { Default, FirstProcessPublished, MarkedProcessDuplicated,
		BestQualityContent, BestVoteConvergence };
	
	public Categ categ { get; private set; }
	public string Name { get; private set; }
	public int ScorePoints { get; private set; }
	
	public Medal()
	{
		categ = Categ.Default;
		Name = "";
	}
	
	public Medal(Categ type)
	{
		categ = type;
		
		switch (categ)
		{
			case Categ.FirstProcessPublished:
				Name = "First process published in the game";
				ScorePoints = 100;
				break;
			case Categ.MarkedProcessDuplicated:
				Name = "Marked a process as a duplicate correctly.";
				ScorePoints = 25;
				break;
			case Categ.BestQualityContent:
				Name = "Created process with best quality vote rate.";
				ScorePoints = 70;
				break;
			case Categ.BestVoteConvergence:
				Name = "Best convergence in votes placed.";
				ScorePoints = 150;
				break;
		}
	}
	
	public static Categ GetCateg(string type)
	{
		Categ ret = Categ.Default;
		
		if (type.Equals("FirstProcessPublished"))
			ret = Categ.FirstProcessPublished;
		else if (type.Equals("MarkedProcessDuplicated"))
			ret = Categ.MarkedProcessDuplicated;
		else if (type.Equals("BestQualityContent"))
			ret = Categ.BestQualityContent;
		else if (type.Equals("BestVoteConvergence"))
			ret = Categ.BestVoteConvergence;
		
		return ret;
	}
}

