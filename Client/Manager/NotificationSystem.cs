using System;
using UnityEngine;
using System.Collections.Generic;

public class NotificationSystem
{	
	public static int appearanceTime = 15;

	public List<Notification> Items { get; private set; }

	public NotificationSystem() { Items = new List<Notification>();	}
	
	public void Close(int NID)
	{
		List<Notification> newItems = new List<Notification>();
		
		foreach (Notification notif in Items)
			if (notif.NID != NID)
			{
				newItems.Add(notif);
			}
		
		Items = newItems;
	}
	
}
