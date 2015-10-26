using System;

[Serializable]
public class Vote
{
	public bool Type;
	public string User = "";
	
	public Vote() {}
	
	public Vote(bool type, string username)
	{
		Type = type;
		User = username;
	}
	
	public bool GetVote() { return Type; }
	public string GetVoter() { return User;	}
}