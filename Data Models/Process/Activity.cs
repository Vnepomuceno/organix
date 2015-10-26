using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class Activity : Primitive
{	
	[XmlAttribute("Name")]
	public string Name;
	[XmlAttribute("Description")]
	public string Description;
	
	#region SERVER
	public Activity() {	Name = Description = ""; }
	public Activity(int PrID, string name) : base(PrID, -1, -1) { Name = name; }

	public Activity(string name, string description)
	{
		Name = name;
		Description = description;
	}
	#endregion

	#region CLIENT
	public Activity(int PrID, string name, float x, float y) : base(PrID, x, y) { Name = name; }
	public override string ToString() {	return "Activity " + PrID; }
	#endregion
	
}

