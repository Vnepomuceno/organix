using UnityEngine;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;

[XmlRoot("ServerData")]
public class RegPlayersContainer
{	
	[XmlArray("RegisteredPlayers"), XmlArrayItem("Player")]
	public List<Player> Players;
	[XmlIgnore]
	public string xml = "<?xml version=\"1.0\" encoding=\"us-ascii\"?>" +
		"<ServerData xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
		"<RegisteredPlayers>" +
			"<Player ID=\"1\"><Username>New York</Username><Password></Password></Player>" +
			"<Player ID=\"2\"><Username>California</Username><Password></Password></Player>" +
			"<Player ID=\"3\"><Username>Ohio</Username><Password></Password></Player>" +
			"<Player ID=\"4\"><Username>Florida</Username><Password></Password></Player>" +
			"<Player ID=\"5\"><Username>Pennsylvania</Username><Password></Password></Player>" +
			"<Player ID=\"6\"><Username>Washington</Username><Password></Password></Player>" +
		"</RegisteredPlayers></ServerData>";
	
	public RegPlayersContainer() { Players = new List<Player>(); }
	
	public RegPlayersContainer LoadXML()
	{
		XmlSerializer serializer = new XmlSerializer(typeof(RegPlayersContainer));
		RegPlayersContainer loadedData;
		
		using (TextReader reader = new StringReader(xml))
			loadedData = (RegPlayersContainer)serializer.Deserialize(reader);
		
		return loadedData;
	}
	
}

