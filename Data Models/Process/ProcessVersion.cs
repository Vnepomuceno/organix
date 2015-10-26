using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class ProcessVersion : Process
{	
	public int PVID { get; private set;}
	public int OriginalPID;
	
	public ProcessVersion() {}
	
	public ProcessVersion(int VersionID, Process process, string author) : base(process.PID)
	{
		OriginalPID = process.PID;
		PVID = VersionID;
		Author = author;
		base.Name = process.Name;
		base.Pool = new List<Lane>();
		base.Connections = new List<Flow>();
	}
	
}