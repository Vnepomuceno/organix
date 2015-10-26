using System;
using UnityEngine;
using System.Collections.Generic;

public class Notification
{	
	public enum Type { Default, Achievement, Medal, Exception };
	public static int IDCounter = 0;
	
	public int NID { get; private set; }
	public DateTime time;
	public Type type;
	public string content;
	
	public Notification()
	{
		NID = System.Threading.Interlocked.Increment(ref IDCounter);
		time = System.DateTime.Now;
		type = Type.Default;
		content = "";
	}
	
	public Notification(Type notificationType, string text)
	{
		NID = System.Threading.Interlocked.Increment(ref IDCounter);
		time = System.DateTime.Now;
		type = notificationType;
		content = text;
	}
	
}

