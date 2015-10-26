using System;
using UnityEngine;

public class PaintServerPanels : MonoBehaviour
{	
	private GUISkin MySkin;
	private int topLayerNumber = 1;
	public static bool PlayerEscaped;
	public static bool ShowVersionsPanel, ShowOptionsMenu, ShowLoadMenu, ShowExportMenu,
		ShowConfiguration, ShowDuplicationWindow;
	private string XmlExport, XmlLoad;
	public static string NumberPlayers = "";
	public static string GameLength = "60";
	public static string ToElicitProcessName = "";
	public static string ExpectedNumActivities = "10";
		
	public void Start()
	{
		MySkin = (GUISkin)Resources.Load("OrganixGUI");
		XmlExport = XmlLoad = "";
	}
	
	public void Update()
	{
		if (NumberPlayers.Equals(""))
			NumberPlayers = "" + Server.State.RegisteredPlayers.Players.Count;
		
		if (PlayerEscaped)
		{
			if (ShowOptionsMenu || ShowExportMenu || ShowLoadMenu || ShowConfiguration)
			{
				ShowOptionsMenu = ShowExportMenu = ShowLoadMenu = ShowConfiguration = false;
				XmlExport = "";
			}
			else
				ShowOptionsMenu = true;
			PlayerEscaped = false;
		}
	}
	
	public void OnGUI()
	{
		GUI.depth = topLayerNumber;
		GUI.skin = MySkin;
		
		if (ShowLoadMenu) DrawLoadMenu();
		if (ShowExportMenu) DrawExportMenu();
		if (ShowConfiguration) DrawConfigurationWindow();
		if (ShowDuplicationWindow) DrawDuplicationWindow();
	}
	
	private void DrawConfigurationWindow()
	{
		PainterServer.DrawOverlayDarker();
		
		GUI.BeginGroup(new Rect(Server.SCREEN_WIDTH/2-200-15, Server.SCREEN_HEIGHT/2-225-20, 400, 450), "", "box");
		GUI.Label(new Rect(2, -15, 400, 100), "<size=40>SET GAME CONFIGURATION</size>", "GameTitle");
		
		GUI.Label(new Rect(63, 95, 200, 30), "<size=20>Game Length:</size>", "NormalText");
		GameLength = GUI.TextField(new Rect(200, 92, 50, 40), GameLength, "TextFieldWindow");
		GUI.Label(new Rect(260, 95, 200, 30), "<size=20>minutes</size>", "NormalText");
		
		GUI.Label(new Rect(63, 150, 200, 30), "<size=20>Number of Players:</size>", "NormalText");
		NumberPlayers = GUI.TextField(new Rect(240, 147, 50, 40), NumberPlayers, "TextFieldWindow");
		
		GUI.Box(new Rect(53, 210, 290, 2), "", "Separator");
		
		GUI.Label(new Rect(69, 225, 300, 30), "<size=20>Name of process to be elicited:</size>", "NormalText");
		ToElicitProcessName = GUI.TextField(new Rect(53, 260, 290, 30), ToElicitProcessName, "TextFieldWindow");
		
		GUI.Label(new Rect(53, 315, 300, 30), "<size=20>Expected</size>", "NormalText");
		GUI.Label(new Rect(190, 315, 300, 30), "<size=20>activities per process</size>", "NormalText");
		ExpectedNumActivities = GUI.TextField(new Rect(137, 312, 45, 40), ExpectedNumActivities, "TextFieldWindow");
		
		if (GUI.Button(new Rect(200-65, 450-75, 132, 50), "Save", "GreenButton"))
		{
			ShowConfiguration = false;
		}
		GUI.EndGroup();
	}
	
	private void DrawExportMenu()
	{
		XmlExport = PlayerPrefs.GetString("XmlServerState");
		
		PainterServer.DrawOverlay();
		
		GUILayout.BeginArea(new Rect(Server.SCREEN_WIDTH/2-500, Server.SCREEN_HEIGHT/2-300, 1000, 600), "", "box");
		GUI.Label(new Rect(5, -20, 120, 100), "<size=40>EXPORT</size>", "GameTitle");
		XmlExport = GUI.TextArea(new Rect(20, 70, 960, 505), XmlExport, "XmlPanel");
		GUILayout.EndArea();
	}
	
	private void DrawLoadMenu()
	{
		PainterServer.DrawOverlay();
	
		GUILayout.BeginArea(new Rect(Server.SCREEN_WIDTH/2-500, Server.SCREEN_HEIGHT/2-325, 1000, 650), "", "box");
		GUI.Label(new Rect(20, -20, 120, 100), "<size=40>LOAD XML</size>", "GameTitle");
		XmlLoad = GUI.TextArea(new Rect(20, 70, 960, 505), XmlLoad, "XmlPanel");
		
		if (GUI.Button(new Rect(365, 585, 132, 50), "Load", "GreenButton"))
		{
			Server.State = Server.State.LoadFromXml(XmlLoad);
			Server.State.SaveXml();
			ShowLoadMenu = false;
		}
		GUILayout.EndArea();
	}
	
	private void DrawDuplicationWindow()
	{
		Process process;
		if (Server.CurrentScreen == Server.GameScreen.ViewProcess ||
			Server.CurrentScreen == Server.GameScreen.ViewComposed ||
			Server.CurrentScreen == Server.GameScreen.ViewAdHoc)
			process = Server.State.GetProcess(Server.CurrentProcess.PID);
		else
			process = Server.State.GetTargetProcess(Server.CurrentProcess.PID, Server.CurrentVersion.PVID);
		int width = 400, height = 270;
		
		PainterServer.DrawOverlay();
		GUI.BeginGroup(new Rect(Server.SCREEN_WIDTH/2-width/2, Server.SCREEN_HEIGHT/2-height/2-20, width, height), "", "DuplicationWindow");
		if (GUI.Button(new Rect(360, 12, 30, 30), "", "CloseButtonWindow"))
		{
			ShowDuplicationWindow = false;
		}
		
		if (process.markedDuplication)
		{
			GUI.Label(new Rect(0, 50, 404, 60),
				"<size=25><b>" + process.markAuthor + " marked this process\n as a duplication of " +
				((process.duplicationPVID == -1) ?
					("process " + process.duplicationPID) :
					("version " + process.duplicationPVID + " (P" + process.duplicationPID + ")")) + ".</b></size>", "Center");
			GUI.Label(new Rect(0, 120, 404, 30),
				"<size=18>Yes: " + process.posDuplicationVotes + " , " +
				"No: " + process.negDuplicationVotes + "</size>", "Center");
			if (GUI.Button(new Rect(135, 180, 132, 50), "Okay", "GreenButton"))
				ShowDuplicationWindow = false;
		}
		GUI.EndGroup();
	}
	
}