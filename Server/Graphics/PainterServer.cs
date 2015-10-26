using System;
using System.Collections.Generic;
using UnityEngine;

public class PainterServer : MonoBehaviour
{	
	public static int SCREEN_WIDTH = 1280;
	public static int SCREEN_HEIGHT = 700;
	
	private GUISkin MyStyle;
    public static Texture2D lineTex, lineInfoTex;
	private static Texture Overlay, OverlayDarker, ArrowHead, ArrowInfoHead, LoopArrow, toolbarBackground;
	private static bool NumDraftProcessesChosen, ProcessesDefined;
	private static bool removeWindow;
	private static int NumDraftProcesses;
	private static bool ShowDisconnected, ShowMenu;
	private Vector2 scrollPos = Vector2.zero, ScrollVersions = Vector2.zero,
		ScrollContent = Vector2.zero;
	int bottomLayerNumber = 10;
	private int PosVersionsPanel = Server.SCREEN_HEIGHT-109-60;
	
	public void Start()
	{
		MyStyle = (GUISkin)Resources.Load("OrganixGUI");
		lineTex = (Texture2D)Resources.Load("Images/LineTex");
        lineInfoTex = (Texture2D)Resources.Load("Images/LineInfoTex");
		Overlay = (Texture)Resources.Load("Images/Overlay");
		OverlayDarker = (Texture)Resources.Load("Images/OverlayDarker");
		ArrowHead = (Texture)Resources.Load("Images/Meta-Language/ArrowHead");
		ArrowInfoHead = (Texture)Resources.Load("Images/Meta-Language/ArrowHeadTex");
		LoopArrow = (Texture)Resources.Load("Images/Meta-Language/LoopArrow");
		toolbarBackground = (Texture)Resources.Load("Images/Toolbar/toolbarBackground");
	}
	
	public void OnGUI()
	{
		GUI.depth = bottomLayerNumber;
		GUI.skin = MyStyle;
		
		DrawBackground();
		
		switch (Server.CurrentScreen)
		{
			case Server.GameScreen.Home:
				DrawSidebar(Server.State.ActivePlayers, Server.State.RegisteredPlayers.Players, Server.State.LocalProcesses);
				DrawTopContentFrame();
				Console.DrawConsole();
				break;
				
			case Server.GameScreen.ViewProcess:
				DrawBoard(Server.CurrentProcess);
				DrawToolBar();
				DrawVersionsPanel();
				break;
				
			case Server.GameScreen.ViewAdHoc:
				DrawAdHocSubActivities(Server.CurrentPrimitive as AdHocActivity);
				DrawToolBar();
				break;
				
			case Server.GameScreen.ViewComposed:
				DrawComposedSubActivities(Server.CurrentPrimitive as ComposedActivity);
				DrawToolBar();
				break;
				
			case Server.GameScreen.ViewVersion:
				DrawBoard(Server.CurrentVersion);
				DrawToolBar();
				DrawVersionsPanel();
				break;
		}
		
		if (ShowDisconnected)
		{
			DrawOverlayDarker();
			GUI.BeginGroup(new Rect(SCREEN_WIDTH/2-200, SCREEN_HEIGHT/2-130, 400, 160), "", "box");
			GUI.Label(new Rect(97, 30, 200, 20), "<size=47>SERVER DISCONNECTED</size>", "GameTitle");
			GUI.Label(new Rect(0, 95, 400, 20), "<size=25>Reload page to restart it...</size>", "Center");
			GUI.EndGroup();
		}
		
		if (UnityEngine.Event.current.type == EventType.KeyUp &&
			UnityEngine.Event.current.keyCode == KeyCode.Escape)
			PaintServerPanels.PlayerEscaped = true;
	}
	
	public void DrawBackground()
	{
		Texture2D background = (Texture2D)Resources.Load("Images/background");
		int x, y;
		int dim = 200;
		for (x = 0; x < Screen.width; x += dim)
			for (y = 0; y < Screen.height; y += dim)
				GUI.DrawTexture(new Rect(x, y, dim, dim), background);
	}
	
	private void DrawVersionsPanel()
	{
		int versionWidth = 159;
		int versionHeight = 109;
		Rect versionRect = new Rect(0, 0, versionWidth, versionHeight);

		if (Server.CurrentProcess.published)
		{
			Process targetScoreProc;
			if (Server.CurrentScreen == Server.GameScreen.ViewVersion ||
			    Server.CurrentScreen == Server.GameScreen.ViewVersionAdHoc ||
			    Server.CurrentScreen == Server.GameScreen.ViewVersionComposed)
				targetScoreProc = Server.CurrentVersion;
			else
				targetScoreProc = Server.CurrentProcess;

			GUI.BeginGroup(new Rect(SCREEN_WIDTH-160, PosVersionsPanel - 145, 150, 135));
			GUI.DrawTexture(new Rect(0, 0, 150, 135), OverlayDarker);
			GUI.Label(new Rect(15, 10, 150, 20), "<size=12>Activ. Score: +" + targetScoreProc.score + " pts</size>", "LeftWhiteText");
			GUI.Label(new Rect(15, 30, 150, 20), "<size=12>Poisson: " + ((targetScoreProc.bonusMalus[0] < 0) ? "" : "+") + (int)targetScoreProc.bonusMalus[0] + " pts</size>", "LeftWhiteText");
			GUI.Label(new Rect(15, 45, 150, 20), "<size=12>Vote Rate: " + ((targetScoreProc.bonusMalus[1] < 0) ? "" : "+") + targetScoreProc.bonusMalus[1] + " pts</size>", "LeftWhiteText");
			GUI.Label(new Rect(15, 60, 150, 20), "<size=12>Duplication: " + ((targetScoreProc.bonusMalus[2] < 0) ? "" : "+") + targetScoreProc.bonusMalus[2] + " pts</size>", "LeftWhiteText");
			GUI.Label(new Rect(15, 75, 150, 20), "<size=12>Best VR: " + ((targetScoreProc.bonusMalus[3] < 0) ? "" : "+") + targetScoreProc.bonusMalus[3] + " pts</size>", "LeftWhiteText");
			GUI.Label(new Rect(15, 100, 150, 20), "<size=15>Total: +" + (int)targetScoreProc.GetTotalScore() + " pts</size>", "LeftBoldWhiteText");
			GUI.EndGroup();

			GUI.BeginGroup(new Rect(-4, PosVersionsPanel, Server.SCREEN_WIDTH+8, versionHeight+65), "", "box");
			if (GUI.Button(new Rect(Server.SCREEN_WIDTH - 30, 8, 28, 28), "", "ViewButton"))
			{
				if (PosVersionsPanel == Server.SCREEN_HEIGHT-versionHeight-60)
					PosVersionsPanel = Server.SCREEN_HEIGHT-versionHeight-60 + 133;
				else
					PosVersionsPanel = Server.SCREEN_HEIGHT-versionHeight-60;
			}
			
			GUI.Label(new Rect(15, 7, 100, 20), "<size=19>Process Versions</size>", "SectionTitle");
			ScrollVersions = GUI.BeginScrollView(new Rect(15, 35, Server.SCREEN_WIDTH-20, versionHeight+20),
							ScrollVersions,
							new Rect(0, 0, (Server.CurrentProcess.Versions.Count+2)*(versionWidth+20), versionHeight-10),
							new GUIStyle(GUI.skin.horizontalScrollbar), GUIStyle.none);
			
			#region Original Process
			string processStyle = (Server.CurrentScreen == Server.GameScreen.ViewProcess) ? "VersionItemSel" : "VersionItem";
			if (GUI.Button(versionRect, "", processStyle))
			{
				Server.CurrentScreen = Server.GameScreen.ViewProcess;
			}
			GUI.Label(new Rect(versionRect.x+15, versionRect.y-30, 100, 100), "Process " + Server.CurrentProcess.PID, "VersionPanelItemText");
			if (Server.CurrentProcess.finalConsensus)
				GUI.Label(new Rect(versionRect.x+15, versionRect.y+35, 100, 100), "<size=20>Final Consensus</size>", "VersionPanelItemText");
			
			versionRect.x += versionWidth+20;
			#endregion

			#region Process Versions
			foreach (ProcessVersion version in Server.CurrentProcess.Versions)
			{
				string versionStyle = (Server.CurrentVersion != null && Server.CurrentVersion.PVID == version.PVID
					&& Server.CurrentScreen == Server.GameScreen.ViewVersion) ? "VersionItemSel" : "VersionItem";
				if (GUI.Button(versionRect, "", versionStyle))
				{
					Server.CurrentVersion = version;
					Server.CurrentScreen = Server.GameScreen.ViewVersion;
				}
				GUI.Label(new Rect(versionRect.x+15, versionRect.y-10, 100, 100), "Version " +
					version.PVID + "\n\n" + (version.published ? "<size=15>Published</size>" : "<size=15>Unpublished</size>"), "VersionPanelItemText");
				if (Server.CurrentProcess.finalConsensus)
					GUI.Label(new Rect(versionRect.x+15, versionRect.y+35, 100, 100), "<size=20>Final Consensus</size>", "VersionPanelItemText");
				versionRect.x += versionWidth+20;
			}
			#endregion
			
			GUI.EndScrollView();
			GUI.EndGroup();
		}
	}
	
	public int GetNumberActivePlayers(List<Player> activeList)
	{
		int n = 0;
		
		foreach (Player player in activeList)
			if (player.Online)
				n++;
		
		return n;
	}
	
	public void DrawSidebar(List<Player> ActivePlayers, List<Player> RegisteredPlayers, List<Process> LocalProcesses)
	{
		Rect nextItem = new Rect(15, 10, 135, 20);
		
		GUI.BeginGroup(new Rect(-4, -4, 150, SCREEN_HEIGHT+9), "", "box");	
		
		GUI.Label(nextItem, "<size=14><b>Online Players</b> (" + GetNumberActivePlayers(ActivePlayers) + ")</size>");
		nextItem.y += 30;
		
		foreach (Player p in ActivePlayers)
		{
			if (p.Online)
			{
				if (GUI.Button(new Rect(4, nextItem.y, 142, 30), p.Username, "PlayerButton"))
					Server.SelectedPlayer = p;
				nextItem.y += 26;
			}
		}
		nextItem.y += 10;
		
		GUI.Box(new Rect(0, nextItem.y, 147, 2), "", "Separator");
		nextItem.y += 13;
		
		GUI.Label(nextItem, "<size=14><b>Registered Players</b> (" + RegisteredPlayers.Count + ")</size>", "ConsoleText");
		nextItem.y += 25;
		foreach (Player pReg in RegisteredPlayers)
		{
			if (GUI.Button(new Rect(4, nextItem.y, 142, 30), pReg.Username, "PlayerButton"))
				Server.SelectedPlayer = Server.State.GetPlayer(pReg.Username, Server.State.ActivePlayers);
			nextItem.y += 26;
		}
		nextItem.y += 20;
		
		if (Server.SelectedPlayer != null)
		{
			Player player = Server.SelectedPlayer;
			GUI.Box(new Rect(0, nextItem.y, 147, 2), "", "Separator"); nextItem.y += 13;
			
			GUI.Label(nextItem, "<size=17><b>" + player.Username + "</b></size>", "ConsoleText");
			nextItem.y += 23;
			GUI.Label(nextItem, "<size=15>" + player.Score + " score points</size>", "ConsoleText");
			nextItem.y += 23;
			GUI.Label(nextItem, "<size=12>" + player.NumDraftProcesses +
				((player.NumDraftProcesses == 1) ? " draft process</size>" : " draft processes</size>"), "ConsoleText");
			nextItem.y += 16;
			GUI.Label(nextItem, "<size=12>" + player.NumDraftVersions +
				((player.NumDraftVersions == 1) ? " draft version</size>" : " draft versions</size>"), "ConsoleText");
			nextItem.y += 22;
			GUI.Label(nextItem, "<size=12>" + player.NumPubProcesses +
				((player.NumPubProcesses == 1) ? " published process</size>" : " published processes</size>"), "ConsoleText");
			nextItem.y += 16;
			GUI.Label(nextItem, "<size=12>" + player.NumPubVersions +
				((player.NumPubVersions == 1) ? " published version</size>" : " published versions</size>"), "ConsoleText");
			nextItem.y += 22;
			GUI.Label(nextItem, "<size=12>" + (player.NumVotesProcesses + player.NumVotesVersions) +
				(((player.NumVotesProcesses + player.NumVotesVersions) == 1) ? " vote placed</size>" : " votes placed</size>"),
				"ConsoleText");
			nextItem.y += 16;
			GUI.Label(nextItem, "<size=12>" + player.NumDupMarks +
				((player.NumDupMarks == 1) ? " duplication mark</size>" : " duplication marks</size>"),
				"ConsoleText");
			nextItem.y += 22;
			GUI.Label(nextItem, "<size=12>" + player.Achievements.Count + "/4 achievements</size>", "ConsoleText");
			nextItem.y += 16;
			GUI.Label(nextItem, "<size=12>" + player.Medals.Count + "/4 medals</size>", "ConsoleText");
		}

		GUI.Label(new Rect(15, SCREEN_HEIGHT-120, 150, 20), "<size=18><b>Endpoint</b></size>", "ConsoleText");
		GUI.Label(new Rect(15, SCREEN_HEIGHT-95, 150, 20), "<size=15>IP: <b>" + Network.player.ipAddress + "</b></size>", "ConsoleText");
		GUI.Label(new Rect(15, SCREEN_HEIGHT-75, 150, 20), "<size=15>Port: <b>" + Network.player.port + "</b></size>", "ConsoleText");

		if (GUI.Button(new Rect(4, SCREEN_HEIGHT-48, 72, 52), "", "DisconnectButton"))
		{
			Server.Disconnect();
			ShowDisconnected = true;
		}
		
		if (GUI.Button(new Rect(76, SCREEN_HEIGHT-48, 70, 52), "", "OptionsServerButton"))
			ShowMenu = !ShowMenu;
		
		GUI.EndGroup();

		if (ShowMenu)
		{
			if (GUI.Button(new Rect(70, Server.SCREEN_HEIGHT-47-36*5+5, 125, 36), "Set Configuration", "MenuBulletServer"))
			{
				PaintServerPanels.ShowConfiguration = true;
				ShowMenu = false;
			}
			if (GUI.Button(new Rect(70, Server.SCREEN_HEIGHT-47-36*4+4, 125, 36), "End Game", "MenuBulletServer"))
			{
				Server.GameOver = true;
				Server.State.GameOver = true;
				networkView.RPC("GameOverAck", RPCMode.Others);
				ShowMenu = false;
			}
			if (GUI.Button(new Rect(70, Server.SCREEN_HEIGHT-47-36*3+3, 125, 36), "Load XML", "MenuBulletServer"))
			{
				PaintServerPanels.ShowLoadMenu = true;
				ShowMenu = false;
			}
			if (GUI.Button(new Rect(70, Server.SCREEN_HEIGHT-47-36*2+2, 125, 36), "Export XML", "MenuBulletServer"))
			{
				PaintServerPanels.ShowExportMenu = true;
				Server.State.SaveXml();
				ShowMenu = false;
			}
			if (GUI.Button(new Rect(70, Server.SCREEN_HEIGHT-47-36, 125, 36), "Reset State", "MenuBulletServer"))
			{
				Server.RequestReset = true;
				ShowMenu = false;
			}
		}
	}
	
	public void DrawBoard(Process process)
	{
		if (process != null)
		{
			if (process.markedDuplication)
					GUI.Label(new Rect(150, 45, 200, 20), "Marked as duplicate of P" +
							process.duplicationPID + ((process.duplicationPVID != -1) ? " V" + process.duplicationPVID : ""),
							"SectionTitle");
			
			scrollPos = GUI.BeginScrollView(new Rect(15, 80, Server.SCREEN_WIDTH-30, Server.SCREEN_HEIGHT-100),
							scrollPos,
							new Rect(0, 0, Server.SCREEN_WIDTH-50, Server.CurrentProcess.Pool.Count*180+30));
			
			DrawProcess(process);
			
			GUI.EndScrollView();
			
			if (process.Pool.Count > 3)
			{
				Matrix4x4 backupMatrix = GUI.matrix;
				GUIUtility.RotateAroundPivot(270f, new Vector2(15, Server.SCREEN_HEIGHT));
				Rect poolLabelRect = new Rect(15, Server.SCREEN_HEIGHT, Server.SCREEN_HEIGHT, 30);
				GUI.Label(poolLabelRect, "Process " + process.PID + ": " + process.Name, "PoolLabelText");
				GUI.matrix = backupMatrix;
			}
			
			GUI.Label(new Rect(15, 45, 200, 20), "Author: " + process.Author, "SectionTitle");
		}
	}
	
	public void DrawProcess(Process process)
	{
		int laneHeight = 180;
		Rect nextPoolRect = new Rect(0, 0, Server.SCREEN_WIDTH-60, laneHeight);
		
		#region DRAWS LANES
		Matrix4x4 backupMatrix = GUI.matrix;
		if (process != null)
		{
			foreach (Lane lane in process.Pool)
			{
				GUI.Box(new Rect(0, lane.y-90, Server.SCREEN_WIDTH-60, laneHeight), "", "Lane");
				
				if (process.Pool.Count != 1)
				{
					Rect laneButtonRect = new Rect(29, lane.y-laneHeight/2, 25, laneHeight);
					string laneLabelStyle;
					
					if (Server.CurrentPrimitive != null && lane.PrID == Server.CurrentPrimitive.PrID)
						laneLabelStyle = "LaneLabelSel";
					else
						laneLabelStyle = "LaneLabel";
					
					if (GUI.Button(laneButtonRect, "", laneLabelStyle))
						Server.CurrentPrimitive = lane;
					
					GUIUtility.RotateAroundPivot(270f, new Vector2(29, lane.y+laneHeight/2));
					Rect laneRect = new Rect(29, lane.y+90, laneHeight, 25);
					GUI.Label(laneRect, lane.Participant, "Center");
					GUI.matrix = backupMatrix;
				}
				
				nextPoolRect.y += laneHeight-1;
			}
			
			Rect poolLabelButtonRect = new Rect(0, 0, 30, laneHeight*process.Pool.Count-process.Pool.Count+1);
			GUI.Button(poolLabelButtonRect, "", "PoolLabel");
			
			if (process.Pool.Count <= 3)
			{
				GUIUtility.RotateAroundPivot(270f, new Vector2(0, laneHeight*process.Pool.Count));
				Rect poolLabelRect = new Rect(laneHeight*process.Pool.Count > Server.SCREEN_HEIGHT-100 ? laneHeight*process.Pool.Count/4 : 0,
					laneHeight*process.Pool.Count,
					laneHeight*process.Pool.Count > Server.SCREEN_HEIGHT-100 ? laneHeight*process.Pool.Count/2 : laneHeight*process.Pool.Count ,
					30);
				GUI.Label(poolLabelRect, "Process " + process.PID + ": " + process.Name, "PoolLabelText");
				GUI.matrix = backupMatrix;
			}

			GUI.matrix = backupMatrix;
			
			#region DRAWS EDGE FLOWS
			foreach(Flow flow in process.Connections)
			{
				Primitive source = Server.State.GetPrimitive(process.PID, -1, flow.SourceID);
				Primitive target = Server.State.GetPrimitive(process.PID, -1, flow.TargetID);
				
				if (source is Event && ((Event)source).categ.Equals(Event.Categ.Start))
					DrawLine(flow, Color.black, 3, true, false, -1, -1);
				else if (target is Event && ((Event)target).categ.Equals(Event.Categ.End))
					DrawLine(flow, Color.black, 3, false, true, -1, -1);
				else if (source is Event && ((Event)source).categ.Equals(Event.Categ.Merge))
					DrawLine(flow, Color.black, 3, true, false, -1, -1);
				else if (target is Event && ((Event)target).categ.Equals(Event.Categ.Merge))
					DrawLine(flow, Color.black, 3, false, true, -1, -1);
				else
					DrawLine(flow, Color.black, 3, false, false, -1, -1);
			}
			#endregion
			
			#region DRAWS LANGUAGE PRIMITIVES
			foreach (Lane lane in process.Pool)
			{
				foreach (Primitive prim in lane.Elements)
				{
					Rect primitiveRect;
					if (prim is Event)
						primitiveRect = new Rect(prim.x-20, prim.y-20, 40, 40);
					else if (prim is Activity)
						primitiveRect = new Rect(prim.x-75, prim.y-25, 150, 50);
					else
						primitiveRect = new Rect(prim.x-20, prim.x-20, 40, 40);
					
					#region EVENT
					if (prim is Event)
					{
						#region Start Event
						if (((Event)prim).categ.Equals(Event.Categ.Start))
						{
							string startStyle;
							if (Server.CurrentPrimitive != null && prim.PrID == Server.CurrentPrimitive.PrID)
								startStyle = "StartSel";
							else
								startStyle = "StartEvent";
							
							if (GUI.Button(primitiveRect, "", startStyle))
								Server.CurrentPrimitive = prim;
						}
						#endregion
						#region End Event
						else if (((Event)prim).categ.Equals(Event.Categ.End))
						{
							string endStyle;
							if (Server.CurrentPrimitive != null && prim.PrID == Server.CurrentPrimitive.PrID)
								endStyle = "EndSel";
							else
								endStyle = "EndEvent";
							
							if (GUI.Button(primitiveRect, "", endStyle))
								Server.CurrentPrimitive = prim;
						}
						#endregion
						#region Merge Event
						else if (((Event)prim).categ.Equals(Event.Categ.Merge))
						{
							string mergeStyle;
							if (Server.CurrentPrimitive != null && prim.PrID == Server.CurrentPrimitive.PrID)
								mergeStyle = "MergeSel";
							else
								mergeStyle = "Merge";
							
							if (GUI.Button(primitiveRect, "", mergeStyle))
								Server.CurrentPrimitive = prim;
						}
						#endregion
					}
					#endregion
					#region ACTIVITY
					#region Composed Activity
					else if (prim is ComposedActivity)
					{
						ComposedActivity a = prim as ComposedActivity;
						string compActStyle;
						if (Server.CurrentPrimitive != null && prim.PrID == Server.CurrentPrimitive.PrID)
							compActStyle = "ComposedActivitySel";
						else
							compActStyle = "ComposedActivity";
						
						#region Open composed activity
						if (Input.GetKeyUp(KeyCode.Space) &&
							Server.CurrentPrimitive != null &&
							prim.PrID == Server.CurrentPrimitive.PrID)
						{
							Server.CurrentPrimitive = prim;
							Server.CurrentScreen = Server.GameScreen.ViewComposed;
						}

						if (GUI.Button(primitiveRect, a.Name, compActStyle))
							Server.CurrentPrimitive = a;
						#endregion
					}
					#endregion
					#region Ad-Hoc Activity
					else if (prim is AdHocActivity)
					{
						AdHocActivity a = prim as AdHocActivity;
						string adHocActStyle;
						if (Server.CurrentPrimitive != null && prim.PrID == Server.CurrentPrimitive.PrID)
							adHocActStyle = "AdHocActivitySel";
						else
							adHocActStyle = "AdHocActivity";
						
						#region Open ad-hoc activity
						if (Input.GetKeyUp(KeyCode.Space) &&
							Server.CurrentPrimitive != null &&
							prim.PrID == Server.CurrentPrimitive.PrID)
						{
							Server.CurrentPrimitive = prim;
							Server.CurrentScreen = Server.GameScreen.ViewAdHoc;
						}
						#endregion
						#region Select ad-hoc activity
						else if (GUI.Button(primitiveRect, a.Name, adHocActStyle))
							Server.CurrentPrimitive = a;
						#endregion
					}
					#endregion
					#region Work Activity
					else if (prim is Activity)
					{
						Activity a = prim as Activity;
						string actStyle;
						if (Server.CurrentPrimitive != null && prim.PrID == Server.CurrentPrimitive.PrID)
							actStyle = "ActivitySel";
						else
							actStyle = "Activity";
						
						#region Select activity
						if (GUI.Button(primitiveRect, a.Name, actStyle))
							Server.CurrentPrimitive = a;
						#endregion
					}
					#endregion
				}
				#endregion
			}
			#endregion
		}
		#endregion
	}
	
	public void DrawAdHocSubActivities(AdHocActivity activity)
	{
		GUI.BeginGroup(new Rect(15, 80, Server.SCREEN_WIDTH, Server.SCREEN_HEIGHT-100));
		Matrix4x4 backupMatrix = GUI.matrix;
		Rect poolRect = new Rect(Server.SCREEN_WIDTH/2-720/2, 15, 720, 500);
		GUI.Box(new Rect(poolRect), "", "Lane");
		
		GUIUtility.RotateAroundPivot(270f, new Vector2(Screen.width/2-720/2, 500+15));
		if (GUI.Button(new Rect(Screen.width/2-720/2, 500+15, 500, 30),
			"Activity " + activity.PrID + ": " + activity.Name, "PoolLabel")) { }
		GUI.matrix = backupMatrix;
		
		foreach (Primitive prim in activity.lane.Elements)
		{
			Activity a = prim as Activity;
			Rect primitiveRect = new Rect(prim.x-75, prim.y-25, 150, 50);
			string actStyle;
			if (Server.CurrentSubPrimitive != null && prim.PrID == Server.CurrentSubPrimitive.PrID)
				actStyle = "ActivitySel";
			else
				actStyle = "Activity";
			
			if (GUI.Button(primitiveRect, a.Name, actStyle))
				Server.CurrentSubPrimitive = a;
		}
		GUI.EndGroup();
	}
	
	public void DrawComposedSubActivities(ComposedActivity activity)
	{
		int PVID = (Server.CurrentScreen == Server.GameScreen.ViewProcess || Server.CurrentVersion == null) ?
			-1 : Server.CurrentVersion.PVID;
		
		GUI.BeginGroup(new Rect(15, 80, Server.SCREEN_WIDTH, Server.SCREEN_HEIGHT-100));

		#region DRAWS LANE
		Matrix4x4 backupMatrix = GUI.matrix;
		Rect poolRect = new Rect(0, 15, Server.SCREEN_WIDTH-40, 300);
		GUI.Box(poolRect, "", "Lane");
		GUIUtility.RotateAroundPivot(270f, new Vector2(0, 300+15));
		GUI.Button(new Rect(0, 300+15, 300, 30), "Activity " + activity.PrID + ": " + activity.Name, "PoolLabel");			
		GUI.matrix = backupMatrix;
		#endregion

		#region DRAWS EDGE FLOWS
		foreach (Flow flow in activity.Connections)
		{
			int LaneID = Server.State.GetLaneID(Server.CurrentProcess.PID, PVID, activity.PrID);
			Primitive source = Server.State.GetSubPrimitive(Server.CurrentProcess.PID, PVID, LaneID,
				Server.CurrentPrimitive.PrID, flow.SourceID);
			Primitive target = Server.State.GetSubPrimitive(Server.CurrentProcess.PID, PVID, LaneID,
				Server.CurrentPrimitive.PrID, flow.TargetID);
			
			if (source is Event && ((Event)source).categ.Equals(Event.Categ.Start))
				DrawLine(flow, Color.black, 3, true, false, activity.PrID, activity.PrID);
			
			else if (target is Event && ((Event)target).categ.Equals(Event.Categ.End))
				DrawLine(flow, Color.black, 3, false, true, activity.PrID, activity.PrID);
			
			else if (source is Event && ((Event)source).categ.Equals(Event.Categ.Merge))
				DrawLine(flow, Color.black, 3, true, false, activity.PrID, activity.PrID);
			else if (target is Event && ((Event)target).categ.Equals(Event.Categ.Merge))
				DrawLine(flow, Color.black, 3, false, true, activity.PrID, activity.PrID);
			else
				DrawLine(flow, Color.black, 3, false, false, activity.PrID, activity.PrID);
		}
		#endregion

		#region DRAWS LANGUAGE PRIMITIVES
		foreach (Primitive prim in activity.lane.Elements)
		{
			Rect primitiveRect = new Rect(prim.x-75, prim.y-25, 150, 50);
			
			string primStyle;
			
			if (prim is Activity)
			{
				Activity a = prim as Activity;
				
				if (Server.CurrentSubPrimitive != null && prim.PrID == Server.CurrentSubPrimitive.PrID)
					primStyle = "ActivitySel";
				else
					primStyle = "Activity";
				
				if (GUI.Button(primitiveRect, a.Name, primStyle))
					Server.CurrentSubPrimitive = a;
			}
			else if (prim is Event)
			{
				Event e = prim as Event;
				
				if (e.categ.Equals(Event.Categ.Start))
				{
					if (Server.CurrentSubPrimitive != null && prim.PrID == Server.CurrentSubPrimitive.PrID)
						primStyle = "StartSel";
					else
						primStyle = "StartEvent";
				}
				else if (e.categ.Equals(Event.Categ.End))
				{
					if (Server.CurrentSubPrimitive != null && prim.PrID == Server.CurrentSubPrimitive.PrID)
						primStyle = "EndSel";
					else
						primStyle = "EndEvent";
				}
				else if (e.categ.Equals(Event.Categ.Merge))
				{
					if (Server.CurrentSubPrimitive != null && prim.PrID == Server.CurrentSubPrimitive.PrID)
						primStyle = "MergeSel";
					else
						primStyle = "Merge";
				}
				else primStyle = "";
				
				if (GUI.Button(new Rect(e.x-20, e.y-20, 40, 40), "", primStyle))
					Server.CurrentSubPrimitive = e;
			}
		}
		#endregion

		GUI.EndGroup();
	}
	
	public void DrawTopContentFrame()
	{
		Rect nextProcButton = new Rect(0, 50, 200, 90);
		int drafts = 0;
		
		GUI.BeginGroup(new Rect(170, 0, 660, Server.SCREEN_HEIGHT), "");
		ScrollContent = GUI.BeginScrollView(new Rect(0, 20, 650, Server.SCREEN_HEIGHT-30),
						ScrollContent,
						new Rect(0, 0, Server.SCREEN_WIDTH - 180 - Server.SCREEN_WIDTH*0.3f-50, (Server.State.LocalProcesses.Count/3+1)*120),
						GUIStyle.none, new GUIStyle(GUI.skin.verticalScrollbar));
		
		#region Unpublished Processes
		GUI.Label(new Rect(5, 0, 300, 40), "<size=23>Unpublished Processes</size>");
		
		foreach (Process unpub in Server.State.LocalProcesses)
			if (!unpub.published)
			{
				if (GUI.Button(nextProcButton, "P" + unpub.PID, "ProcessBtn"))
				{
					Server.CurrentProcess = unpub;
					Server.CurrentScreen = Server.GameScreen.ViewProcess;
				}
				GUI.Label(new Rect(nextProcButton.x + 90, nextProcButton.y, 110, 90), unpub.Name, "ProcessButtonName");
				if (nextProcButton.x >= 420)
				{
					nextProcButton.x = 0;
					nextProcButton.y += 100;
				}
				else
					nextProcButton.x += 210;
				drafts++;
			}
		
		if (drafts == 0)
			GUI.Label(new Rect(5, 70, 580, 40), "<size=15>No processes to display.</size>", "Center");
		
		nextProcButton.x = 0;
		nextProcButton.y += 115;
		#endregion

		#region Published Processes
		GUI.Label(new Rect(5, nextProcButton.y, 300, 40), "<size=23>Published Processes</size>");
		nextProcButton.y += 35;
		
		foreach (Process pub in Server.State.LocalProcesses)
			if (pub.published)
			{
				if (GUI.Button(nextProcButton, "P" + pub.PID, "ProcessBtn"))
				{
					Server.CurrentProcess = pub;
					Server.CurrentScreen = Server.GameScreen.ViewProcess;
				}
				GUI.Label(new Rect(nextProcButton.x + 90, nextProcButton.y, 110, 90), pub.Name, "ProcessButtonName");
				GUI.Label(new Rect(nextProcButton.x + 90, nextProcButton.y+50, 110, 90),
					"<size=12>" + pub.Versions.Count + (pub.Versions.Count == 1 ? " Version</size>" : " Versions</size>"), "ProcessButtonName");
				if (pub.finalConsensus)
					GUI.Label(new Rect(nextProcButton.x + 170, nextProcButton.y+50, 110, 90),
						"<size=14>FC</size>", "ProcessButtonName");
				if (nextProcButton.x >= 300)
				{
					nextProcButton.x = 0;
					nextProcButton.y += 100;
				}
				else
					nextProcButton.x += 210;
			}
		
		if (Server.State.LocalProcesses.Count-drafts == 0)
			GUI.Label(new Rect(5, nextProcButton.y+30, 580, 40), "<size=15>No processes to display.</size>", "Center");

		nextProcButton.y += 115;
		#endregion

		#region Top Players
		GUI.Label(new Rect(5, nextProcButton.y, 300, 40), "<size=23>Top Players</size>");
		nextProcButton.y += 45;
		nextProcButton.x = 0;
		nextProcButton.width = 300;

		List<Player> actPlayers = Server.State.GetActivePlayers();
		List<Player> topPlayers = new List<Player>();
		Player top;

		while (actPlayers.Count != 0)
		{
			top = actPlayers[0];

			foreach (Player p in actPlayers)
				if (p.Score > top.Score)
					top = p;

			topPlayers.Add(top);
			actPlayers.Remove(top);
		}
		
		int i = 1;
		foreach (Player topPlayer in topPlayers)
		{
			GUI.Label(nextProcButton, "<size=20><b>" + i + ". " + topPlayer.Username + "</b>: " + topPlayer.Score + " points</size>");
			nextProcButton.y += 30;
			i++;
		}

		#endregion
		
		GUI.EndScrollView();
		GUI.EndGroup();
	}
	
	private void DrawToolBar()
	{
		for (int i = 0; i < Screen.width; i += 245)
			GUI.DrawTexture(new Rect(i, 0, 245, 43), toolbarBackground);
		
		#region Back Button
		if (GUI.Button(new Rect(0, 0, 43, 37), "", "BackButton"))
		{
			if (Server.CurrentScreen == Server.GameScreen.ViewAdHoc ||
				Server.CurrentScreen == Server.GameScreen.ViewComposed)
				Server.CurrentScreen = Server.GameScreen.ViewProcess;
			else
				Server.CurrentScreen = Server.GameScreen.Home;
		}
		#endregion

		#region Duplication Voting Buttons
		if (Server.CurrentProcess != null && Server.CurrentProcess.markedDuplication &&
			(Server.CurrentScreen == Server.GameScreen.ViewProcess ||
			Server.CurrentScreen == Server.GameScreen.ViewComposed ||
			Server.CurrentScreen == Server.GameScreen.ViewAdHoc))
		{
			if (GUI.Button(new Rect(Screen.width-185, 0, 78, 37), "", "VotedDuplicateButton"))
				PaintServerPanels.ShowDuplicationWindow = true;
			GUI.Label(new Rect(Screen.width-173, 5, 30, 20), "" + Server.CurrentProcess.posDuplicationVotes, "VoteText");
			GUI.Label(new Rect(Screen.width-133, 5, 30, 20), "" + Server.CurrentProcess.negDuplicationVotes, "VoteText");
		}
		else if (Server.CurrentVersion != null && Server.CurrentVersion.markedDuplication &&
			(Server.CurrentScreen == Server.GameScreen.ViewVersion ||
			Server.CurrentScreen == Server.GameScreen.ViewVersionComposed ||
			Server.CurrentScreen == Server.GameScreen.ViewVersionAdHoc))
		{
			if (GUI.Button(new Rect(Screen.width-185, 0, 78, 37), "", "VotedDuplicateButton"))
				PaintServerPanels.ShowDuplicationWindow = true;
			GUI.Label(new Rect(Screen.width-173, 5, 30, 20), "" + Server.CurrentVersion.posDuplicationVotes, "VoteText");
			GUI.Label(new Rect(Screen.width-133, 5, 30, 20), "" + Server.CurrentVersion.negDuplicationVotes, "VoteText");
		}
		#endregion

		#region Quality Voting Buttons
		if (Server.CurrentProcess != null && 
			(Server.CurrentScreen == Server.GameScreen.ViewProcess ||
			Server.CurrentScreen == Server.GameScreen.ViewComposed ||
			Server.CurrentScreen == Server.GameScreen.ViewAdHoc))
		{
			GUI.Button(new Rect(Screen.width-85, 0, 43, 37), "" + Server.CurrentProcess.posVotes, "LikeButtonVoted");
			GUI.Button(new Rect(Screen.width-43, 0, 43, 37), "" + Server.CurrentProcess.negVotes, "DislikeButtonVoted");
		}
		else if (Server.CurrentVersion != null)
		{
			GUI.Button(new Rect(Screen.width-85, 0, 43, 37), "" + Server.CurrentVersion.posVotes, "LikeButtonVoted");
			GUI.Button(new Rect(Screen.width-43, 0, 43, 37), "" + Server.CurrentVersion.negVotes, "DislikeButtonVoted");
		}
		#endregion
	}
	
	public static void DrawOverlay()
	{
		for (int x = 0; x < Screen.width; x += 400)
			for (int y = 0; y < Screen.height; y += 400)
				GUI.DrawTexture(new Rect(x, y, 400, 400), Overlay);
	}
	
	public static void DrawOverlayDarker()
	{
		for (int x = 0; x < Screen.width; x += 400)
			for (int y = 0; y < Screen.height; y += 400)
				GUI.DrawTexture(new Rect(x, y, 400, 400), OverlayDarker);
	}
	
	public void DrawLine(Flow flow, Color color, float width, bool anticipateArrow, bool retardArrow, int APrID, int PrID)
	{
		Primitive source, target;
		int PVID = (Server.CurrentScreen == Server.GameScreen.ViewProcess || Server.CurrentVersion == null) ? -1 : Server.CurrentVersion.PVID;
		
		if (APrID == -1)
		{
			source = Server.State.GetPrimitive(Server.CurrentProcess.PID, PVID, flow.SourceID);
			target = Server.State.GetPrimitive(Server.CurrentProcess.PID, PVID, flow.TargetID);
		}
		else
		{
			Activity act = Server.State.GetActivity(Server.CurrentProcess.PID, PVID, PrID);
			
			int LaneID = Server.State.GetLaneID(Server.CurrentProcess.PID, PVID, act.PrID);
			source = Server.State.GetSubPrimitive(Server.CurrentProcess.PID, PVID, LaneID,
				PrID, flow.SourceID);
			target = Server.State.GetSubPrimitive(Server.CurrentProcess.PID, PVID, LaneID,
				PrID, flow.TargetID);
		}
		Vector2 pointA = new Vector2(source.x, source.y);
		Vector2 pointB = new Vector2(target.x, target.y);
		
		pointA.x = (int)pointA.x; pointA.y = (int)pointA.y;
		pointB.x = (int)pointB.x; pointB.y = (int)pointB.y;
 
		Matrix4x4 matrixBackup = GUI.matrix;
 
		float angle = Mathf.Atan2(pointB.y-pointA.y, pointB.x-pointA.x)*180f/Mathf.PI;
		float length = (pointA-pointB).magnitude;
		
		if (flow.SourceID != flow.TargetID)
		{
			GUIUtility.RotateAroundPivot(angle, pointA);
			Rect lineRect = new Rect(pointA.x, pointA.y, length, width);
			lineRect.height = 3;
			if (flow.categ.Equals(Flow.Categ.Sequence))
				GUI.DrawTexture(lineRect, lineTex);
			else
				GUI.DrawTexture(lineRect, lineInfoTex);
			
			Rect arrowRect;
			if (anticipateArrow)
				arrowRect = new Rect(pointA.x+length/4, pointA.y+width/4-6, 9, 13);
			else if (retardArrow)
				arrowRect = new Rect(pointA.x + 2*length/3, pointA.y + 3*width/4-7, 9, 13);
			else
				arrowRect = new Rect(pointA.x+length/2, pointA.y+width/2-6, 9, 13);
			
			if (flow.categ.Equals(Flow.Categ.Sequence))
				GUI.DrawTexture(arrowRect, ArrowHead);
			else
				GUI.DrawTexture(arrowRect, ArrowInfoHead);
			
			Rect condRect = new Rect(arrowRect.x-50, arrowRect.y-15, 100, 15);
			GUI.Label(condRect, flow.Condition, "FlowCondLabel");
		}
		else
		{
			Rect loopRect = new Rect(source.x-70, source.y-50, 137, 30);
			GUI.DrawTexture(loopRect, LoopArrow);
			GUI.Label(new Rect(loopRect.x+20, loopRect.y-15, 100, 15),flow.Condition, "FlowCondLabel");
		}
		
		GUI.matrix = matrixBackup;
    }
	
}