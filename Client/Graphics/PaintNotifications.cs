using System;
using UnityEngine;

public class PaintNotifications : MonoBehaviour
{
	private GUISkin MySkin;
	private int topLayerNumber = 1;
		
	public void Start() { MySkin = (GUISkin)Resources.Load("OrganixGUI"); }
	
	public void OnGUI()
	{
		GUI.depth = topLayerNumber;
		GUI.skin = MySkin;	
		DrawNotifications();
	}
	
	private void DrawNotifications()
	{
		Rect nextNotifRect = new Rect(15, Painter.SCREEN_HEIGHT - 100, 244, 80);
		DateTime now = System.DateTime.Now;
		
		foreach (Notification notif in Painter.Manager.Notifications.Items)
		{
			if ((now-notif.time).Seconds > NotificationSystem.appearanceTime)
				Painter.Manager.Notifications.Close(notif.NID);
			if (nextNotifRect.y < 300)
			{
				nextNotifRect.x += 260;
				nextNotifRect.y = Painter.SCREEN_HEIGHT-100;
			}
			
			switch (notif.type)
			{
				case Notification.Type.Default:
					if (GUI.Button(nextNotifRect, notif.content, "Notification"))
						Painter.Manager.Notifications.Close(notif.NID);
					break;
					
				case Notification.Type.Achievement:
					if (GUI.Button(nextNotifRect, notif.content, "NotificationAchiev"))
						Painter.Manager.Notifications.Close(notif.NID);
					break;
					
				case Notification.Type.Medal:
					if (GUI.Button(nextNotifRect, notif.content, "NotificationMedal"))
						Painter.Manager.Notifications.Close(notif.NID);
					break;
					
				case Notification.Type.Exception:
					if (GUI.Button(nextNotifRect, notif.content, "NotificationException"))
						Painter.Manager.Notifications.Close(notif.NID);
					break;
			}
			nextNotifRect.y -= 90;
		}
	}
	
}