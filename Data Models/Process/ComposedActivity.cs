using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class ComposedActivity : Activity
{	
	public Lane lane;
	public List<Flow> Connections;
	[XmlIgnore]
	public bool cached;
	
	// SERVER
	
	public ComposedActivity()
	{
		lane = new Lane();
		Connections = new List<Flow>();
	}
	
	public ComposedActivity(int PrID, string name) : base(PrID, name)
	{
		lane = new Lane();
		Connections = new List<Flow>();
	}
	
	public ComposedActivity(int PrID, string name, float x, float y) : base(PrID, name, x, y)
	{
		lane = new Lane();
		Connections = new List<Flow>();
	}

}

