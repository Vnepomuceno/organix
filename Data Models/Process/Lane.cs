using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class Lane : Primitive
{	
	public enum Categ { Default, Parameterized };
	[XmlAttribute("Category")]
	public Categ categ;
	[XmlAttribute("Participant")]
	public string Participant;
	[XmlArray("LaneElements"), XmlArrayItem("Primitive")]
	public List<Primitive> Elements;

	#region SERVER
	public Lane()
	{
		categ = Categ.Default;
		Elements = new List<Primitive>();
	}
	
	public Lane(string participant)
	{
		categ = Categ.Parameterized;
		Participant = participant;
		Elements = new List<Primitive>();
	}
	#endregion

	#region CLIENT
	public Lane(int PrID) : base(PrID, 0, 0)
	{
		categ = Categ.Default;
		Elements = new List<Primitive>();
	}
	
	public Lane(int PrID, string participant) : base(PrID, 0, 0)
	{
		categ = Categ.Parameterized;
		Participant = participant;
		Elements = new List<Primitive>();
	}
	#endregion

}