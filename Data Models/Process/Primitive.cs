using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Xml.Serialization;

[Serializable]
public class Primitive
{	
	public static int IDCounter = 0;
	
	[XmlAttribute("PrID")]
	public int PrID;
	[XmlAttribute("X")]
	public float x;
	[XmlAttribute("Y")]
	public float y;
	[XmlIgnore]
	public bool dragging;	
	[XmlIgnore]
	public List<Primitive> Targets, Sources;
	
	public Primitive()
	{
		PrID = System.Threading.Interlocked.Increment(ref IDCounter);
		Targets = new List<Primitive>();
		Sources = new List<Primitive>();
	}
	
	public Primitive(float left, float top)
	{
		PrID = System.Threading.Interlocked.Increment(ref IDCounter);
		x = left;
		y = top;
		Targets = new List<Primitive>();
		Sources = new List<Primitive>();
	}
	
	public Primitive(int PrimitiveID, float left, float top)
	{
		PrID = PrimitiveID;
		x = left;
		y = top;
		Targets = new List<Primitive>();
		Sources = new List<Primitive>();	
	}
	
	public void UpdateCoordinates(Rect rect)
	{
		x = rect.x + rect.width/2;
		y = rect.y + rect.height/2;
	}
	
	public void UpdCoord(Rect rect)
	{
		x = rect.x;
		y = rect.y;
	} 
	
	public bool Equals(Primitive primitive)
	{
		if (this.GetType().Equals(primitive.GetType()))
		{
			if (this is Activity)
			{
				Activity a = this as Activity;
				Activity target = primitive as Activity;
				if (a.PrID == target.PrID)
					return true;
			}
			else if (this is Event)
			{
				Event ev = this as Event;
				Event target = primitive as Event;
				if (ev.categ.Equals(target.categ))
					return true;
			}
			else if (this is Flow)
			{
				Flow f = this as Flow;
				Flow target = primitive as Flow;
				if (f.categ.Equals(target.categ) &&
					f.Condition.Equals(target.Condition) &&
					f.SourceID == target.SourceID &&
					f.TargetID  == target.TargetID)
					return true;
			}
		}
		return false;	
	}
	
}