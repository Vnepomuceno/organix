using System;
using UnityEngine;

public class PaintEditName : MonoBehaviour
{
	
	private GUISkin MySkin;
	private int topLayerNumber = 9;
	public static bool changeName;
	public static bool changeSubName;
	public static bool changeCondition;
	public static Rect primitiveRect;
	
 	public void Start()
	{
		MySkin = (GUISkin)Resources.Load("OrganixGUI");
	}
	
	public void OnGUI()
	{
		GUI.depth = topLayerNumber;
		GUI.skin = MySkin;
		
		// Change activity name
		if (changeName)
		{
			Activity activity = Painter.Manager.InspectorPrimitive as Activity;
			if (UnityEngine.Event.current.Equals(UnityEngine.Event.KeyboardEvent("return")))
			{
				LanguageConstructor.EditActivity(Painter.Manager.CurrentProcess.PID,
					(Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation) ? -1 : Painter.Manager.CurrentVersion.PVID,
					Painter.Manager.CurrentLane.PrID,
					activity.PrID, Painter.activityName);
				changeName = false;
			}
			Painter.activityName = GUI.TextField(new Rect(primitiveRect.x+15+15, primitiveRect.y+80+11-Painter.scrollPos.y, 120, 25), Painter.activityName);
		}
		
		// Change sub activity name
		else if (changeSubName)
		{
			//Activity subActivity = Painter.Manager.InspectorSubPrimitive as Activity;
			if (UnityEngine.Event.current.Equals(UnityEngine.Event.KeyboardEvent("return")))
			{
				LanguageConstructor.EditSubPrimitive(Painter.Manager.CurrentProcess.PID,
					(Painter.Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
					Painter.Manager.CurrentScreen == GameManager.GameScreen.AdHocCreation) ? -1 : Painter.Manager.CurrentVersion.PVID,
					Painter.Manager.CurrentLane.PrID, Painter.Manager.InspectorPrimitive.PrID,
					Painter.Manager.InspectorSubPrimitive.PrID, Painter.subPrimitiveName);
				changeSubName = false;
			}
			Painter.subPrimitiveName = GUI.TextField(new Rect(primitiveRect.x+15+15, primitiveRect.y+80+11, 120, 25), Painter.subPrimitiveName);
		}
		
		// Change flow condition
		else if (changeCondition)
		{
			if (Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
				Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
			{
				Flow flow = Painter.Manager.InspectorPrimitive as Flow;
				if (UnityEngine.Event.current.Equals(UnityEngine.Event.KeyboardEvent("return")))
				{
					LanguageConstructor.EditConnection(Painter.Manager.CurrentProcess.PID,
						(Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
					Painter.Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation) ? -1 : Painter.Manager.CurrentVersion.PVID,
						flow.SourceID, flow.TargetID,
						Painter.condition);
					changeCondition = false;
				}
			}
			else if (Painter.Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
				Painter.Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
			{
				Flow flow = Painter.Manager.InspectorSubPrimitive as Flow;
				if (UnityEngine.Event.current.Equals(UnityEngine.Event.KeyboardEvent("return")))
				{
					LanguageConstructor.EditSubFlow(Painter.Manager.CurrentProcess.PID,
						(Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
						Painter.Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation) ? -1 : Painter.Manager.CurrentVersion.PVID,
						Painter.Manager.CurrentLane.PrID,
						Painter.Manager.InspectorPrimitive.PrID, flow.PrID, Painter.condition);
					changeCondition = false;
				}
			}
		}
	}
}