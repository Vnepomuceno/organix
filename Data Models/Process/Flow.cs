using System;
using System.Xml.Serialization;

[System.Serializable]
public class Flow : Primitive
{	
	public enum Categ { Sequence, Information };
	[XmlAttribute("Category")]
	public Categ categ;
	[XmlAttribute("SourceID")]
	public int SourceID;
	[XmlAttribute("TargetID")]
	public int TargetID;
	[XmlAttribute("Condition")]
	public string Condition;

	#region SERVER
	public Flow() {}
	public Flow(Categ c, int sourceID, int targetID, string condition)
	{
		categ = c;
		SourceID = sourceID;
		TargetID = targetID;
		Condition = condition;
	}
	#endregion

	#region CLIENT
	public Flow(int PrID, Categ c, int sourceID, int targetID, string condition) : base(PrID, 0, 0)
	{
		categ = c;
		SourceID = sourceID;
		TargetID = targetID;
		Condition = condition;
	}
	#endregion
}