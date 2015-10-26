using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class Event : Primitive
{	
	public enum Categ { Start, End, Merge };
	[XmlAttribute("Category")]
	public Categ categ;

	#region SERVER
	public Event() {}
	public Event(Categ c) { categ = c; }
	#endregion

	#region CLIENT
	public Event(int PrID, float x, float y) : base(PrID, x, y) { }
	public Event(int PrID, Categ c, float x, float y) : base(PrID, x, y) { categ = c; }
	#endregion
	
}