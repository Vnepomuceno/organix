using System;
using UnityEngine;

public class PaintPanels : MonoBehaviour
{	
	private GUISkin MySkin;
	private int topLayerNumber = 5;
	private Vector2 ScrollVersions = Vector2.zero, ScrollDuplicates = Vector2.zero;
	
	public static bool PlayerEscaped;
	public static bool ShowVersionsPanel, ShowScoresPanel, ShowInstructionsPanel,
		ShowAggregator,	ShowGameOver, ShowHelpPanel, HidePanelsRequest;
	public static bool processNameWindow, duplicationWindow;
	private Texture HelpWindow, Aggregator;
	private Texture Instructions1, Instructions2, Instructions3, Instructions4,
		Instructions5; 
	private bool FillCache;
	private int originalPID = -1, originalPVID = -1;
	private string DuplicateListStyle = "";
	private int SlideInstruction = 0;
	
	private int PosVersionsPanel = Painter.SCREEN_HEIGHT-109-60;
	
	public static string markDupMessage = "";
	
	public void Start()
	{
		MySkin = (GUISkin)Resources.Load("OrganixGUI");
		HelpWindow = (Texture)Resources.Load("Images/HelpInfo");
		Aggregator = (Texture)Resources.Load("Images/Aggregator");
		Instructions1 = (Texture)Resources.Load("Images/Instructions1");
		Instructions2 = (Texture)Resources.Load("Images/Instructions2");
		Instructions3 = (Texture)Resources.Load("Images/Instructions3");
		Instructions4 = (Texture)Resources.Load("Images/Instructions4");
		Instructions5 = (Texture)Resources.Load("Images/Instructions5");
	}
	
	public void Update()
	{
		if (PlayerEscaped)
		{
			HidePanels();
			PlayerEscaped = false;
		}
		
	}
	
	public static void HidePanels()
	{
		if (HidePanelsRequest)
		{ 
			ShowAggregator = ShowScoresPanel = ShowHelpPanel = duplicationWindow = false;
			HidePanelsRequest = false;
		}
	}
	
	public void OnGUI()
	{
		GUI.depth = topLayerNumber;
		GUI.skin = MySkin;
		
		#region Process Name
		if (processNameWindow) DrawProcessNameWindow();
		#endregion

		#region Process Versions List Panel
		if (ShowVersionsPanel &&
			(Painter.Manager.CurrentScreen == GameManager.GameScreen.ViewProcess ||
			Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate))
			DrawProcessVersionsPanel();
		#endregion

		#region Mark duplication Window
		if (duplicationWindow) DrawDuplicationWindow();
		#endregion

		#region Help Info Window
		if (ShowHelpPanel) DrawHelpInfoWindow();
		#endregion

		#region Final Scores Panel
		if (ShowScoresPanel) DrawScoresPanel();
		#endregion

		#region Instructions Panel
		if (ShowInstructionsPanel) DrawInstructionsPanel();
		#endregion

		#region Player Info Aggregator Panel
		if (ShowAggregator) DrawAggregator();
		#endregion

		#region Game Over Window
		if (ShowGameOver) DrawGameOverWindow();
		#endregion
	}
	
	private void DrawProcessNameWindow()
	{
		Process process = Painter.Manager.GameState.GetProcess(Painter.Manager.CurrentProcess.PID);
		
		Painter.DrawOverlay();
		GUI.BeginGroup(new Rect(Painter.SCREEN_WIDTH/2-202, Painter.SCREEN_HEIGHT/2-200, 404, 279), "", "DuplicationWindow");
		if (GUI.Button(new Rect(360, 12, 30, 30), "", "CloseButtonWindow"))
			processNameWindow = false;
		GUI.Label(new Rect(0, 70, 404, 30),
			"<size=25><b>Name of process " + process.PID + "</b></size>", "Center");
		Painter.ProcessName = GUI.TextField(new Rect(65, 120, 270, 30), Painter.ProcessName, "TextFieldWindow");
		if (GUI.Button(new Rect(130, 170, 132, 50), "Save", "GreenButton"))
		{
			LanguageConstructor.EditProcess(process.PID, Painter.ProcessName, process.Description);
			processNameWindow = false;
		}
		GUI.EndGroup();
	}
	
	private void DrawHelpInfoWindow()
	{
		int windowWidth = 420;
		int windowHeight = 394;
		Painter.DrawOverlay();
		
		GUI.DrawTexture(new Rect(Painter.SCREEN_WIDTH/2-windowWidth/2,
			Painter.SCREEN_HEIGHT/2-windowHeight/2-50,
			windowWidth, windowHeight), HelpWindow);
		if (GUI.Button(new Rect(Painter.SCREEN_WIDTH/2+160,
			Painter.SCREEN_HEIGHT/2-windowHeight/2-30, 30, 30), "", "CloseButtonWindow"))
			ShowHelpPanel = false;
	}
	
	private void DrawProcessVersionsPanel()
	{
		int versionWidth = 159;
		int versionHeight = 109;
		Rect versionRect = new Rect(0, 0, versionWidth, versionHeight);
		GUI.BeginGroup(new Rect(-4, PosVersionsPanel, Painter.SCREEN_WIDTH+8, versionHeight+65), "", "box");
		if (GUI.Button(new Rect(Painter.SCREEN_WIDTH - 30, 8, 28, 28), "", "ViewButton"))
		{
			if (PosVersionsPanel == Painter.SCREEN_HEIGHT-versionHeight-60)
				PosVersionsPanel = Painter.SCREEN_HEIGHT-versionHeight-60 + 133;
			else
				PosVersionsPanel = Painter.SCREEN_HEIGHT-versionHeight-60;
		}
		
		GUI.Label(new Rect(15, 7, 100, 20), "<size=19>Process Versions</size>", "SectionTitle");
		ScrollVersions = GUI.BeginScrollView(new Rect(15, 35, Painter.SCREEN_WIDTH-20, versionHeight+20),
						ScrollVersions,
						new Rect(0, 0, (Painter.Manager.CurrentProcess.Versions.Count+2)*(versionWidth+20), versionHeight-10),
						new GUIStyle(GUI.skin.horizontalScrollbar), GUIStyle.none);
		
		// Original Process
		string processStyle = (Painter.Manager.CurrentScreen == GameManager.GameScreen.ViewProcess) ? "VersionItemSel" : "VersionItem";
		if (GUI.Button(versionRect, "", processStyle))
			Painter.Manager.CurrentScreen = GameManager.GameScreen.ViewProcess;
		GUI.Label(new Rect(versionRect.x+15, versionRect.y-30, 100, 100), "Process " + Painter.Manager.CurrentProcess.PID, "VersionPanelItemText");
		versionRect.x += versionWidth+20;
		
		// Process Versions
		foreach (ProcessVersion version in Painter.Manager.CurrentProcess.Versions)
		{
			if (version.published || version.Author == Painter.Manager.CurrentPlayer.Username)
			{
				string versionStyle = (Painter.Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate &&
					Painter.Manager.CurrentVersion.PVID == version.PVID) ? "VersionItemSel" : "VersionItem";
				if (GUI.Button(versionRect, "", versionStyle))
				{
					Painter.Manager.CurrentVersion = version;
					Painter.Manager.CurrentScreen = GameManager.GameScreen.ProcessVersionCreate;
				}
				GUI.Label(new Rect(versionRect.x+15, versionRect.y-10, 100, 100), "Version " +
					version.PVID + "\n\n" + (version.published ? "<size=15>Published</size>" : "<size=15>Unpublished</size>"), "VersionPanelItemText");
				versionRect.x += versionWidth+20;
			}
		}
		
		// Add New Version
		if (GUI.Button(versionRect, "", "AddVersion"))
			Mechanics.NewProcessVersion(Painter.Manager.CurrentProcess.PID, Painter.Manager.CurrentPlayer.Username);
		
		GUI.EndScrollView();
		GUI.EndGroup();
	}
	
	private void DrawGameOverWindow()
	{
		Painter.DrawOverlayDarker();
		
		GUI.BeginGroup(new Rect(Painter.SCREEN_WIDTH/2-250, Painter.SCREEN_HEIGHT/2-200, 500, 350), "", "box");
		GUI.Label(new Rect(120, 40, 270, 50), "<size=70>GAME OVER</size>", "GameTitle");
		GUI.Label(new Rect(0, 125, 500, 50), "<size=30>Your time is up.</size>", "Center");
		GUI.Label(new Rect(0, 165, 500, 50), "<size=30>What would you like to do?</size>", "Center");
		
		if (GUI.Button(new Rect(100, 260, 145, 50), "View Processes", "GreyButton"))
		{
			ShowGameOver = false;
		}
		if (GUI.Button(new Rect(270, 260, 132, 50), "View Scores", "GreenButton"))
		{
			ShowGameOver = false;
			ShowScoresPanel = true;
			Painter.Manager.CurrentScreen = GameManager.GameScreen.GameOver;
		}
		GUI.EndGroup();

	}
	
	private void DrawScoresPanel()
	{
		int width = 700;
		int height = 600;
		Rect playerRect = new Rect(40, 100, 100, 50);
		
		Painter.Manager.LoadPlayerScore(Painter.Manager.CurrentPlayer.Username);
		
		if (GUI.Button(new Rect(Painter.SCREEN_WIDTH/2-width/2-1, Painter.SCREEN_HEIGHT/2-height/2-44 + 10, 125, 47), "", "BackProcessesButton"))
		{
			ShowScoresPanel = false;
			Painter.Manager.CurrentScreen = GameManager.GameScreen.Home;
		}
		
		GUILayout.BeginArea(new Rect(Painter.SCREEN_WIDTH/2-width/2, Painter.SCREEN_HEIGHT/2-height/2 + 10, width, height), "", "box");
		
		// Window Title
		GUI.Label(new Rect(0, -45, width, 200), "<size=60>FINAL SCORES</size>", "GameTitle");
		
		// Processes
		GUI.Label(new Rect(playerRect.x-5, playerRect.y, 300, 50), "<size=22>Process Authorship</size>", "BoldText");
		
		GUI.Label(new Rect(playerRect.x-5, playerRect.y+40, 190, 100), "<size=80>+" + (int)(Painter.Manager.CurrentPlayer.Score + 
			Painter.Manager.CurrentPlayer.bonusMalus[0]) + "</size>", "ScoreNumber");
		GUI.Label(new Rect(playerRect.x+15, playerRect.y+120, 190, 20), "<size=25>points</size>", "ScoreNumber");

		
		// Process Versions
		GUI.Label(new Rect(playerRect.x + 230, playerRect.y, 350, 50), "<size=22>Process Voting</size>", "BoldText");
		
		GUI.Label(new Rect(playerRect.x+220, playerRect.y+40, 170, 100), "<size=80>" + 
			(Painter.Manager.CurrentPlayer.bonusMalus[1] < 0 ? "" : "+") +
			(int)Painter.Manager.CurrentPlayer.bonusMalus[1] + "</size>", "ScoreNumber");
		GUI.Label(new Rect(playerRect.x+240, playerRect.y+120, 170, 20), "<size=25>points</size>", "ScoreNumber");
		
		GUI.Label(new Rect(playerRect.x + 415, playerRect.y, 350, 50), "<size=22>Duplication Detection</size>", "BoldText");
		
		GUI.Label(new Rect(playerRect.x+420, playerRect.y+40, 210, 100), "<size=80>" +
			(Painter.Manager.CurrentPlayer.bonusMalus[2] < 0 ? "" : "+") +
			(int)Painter.Manager.CurrentPlayer.bonusMalus[2] + "</size>", "ScoreNumber");
		GUI.Label(new Rect(playerRect.x+440, playerRect.y+120, 210, 20), "<size=25>points</size>", "ScoreNumber");
		
		
		GUI.Label(new Rect(playerRect.x + 115, playerRect.y+190, 350, 50), "<size=28>Achievements</size>", "BoldText");
		
		GUI.Label(new Rect(playerRect.x+90, playerRect.y+230, 185, 100), "<size=80>+" + Painter.Manager.CurrentPlayer.GetAchievementsScore() + "</size>", "ScoreNumber");
		GUI.Label(new Rect(playerRect.x+110, playerRect.y+310, 185, 20), "<size=25>points</size>", "ScoreNumber");
		
		GUI.Label(new Rect(playerRect.x + 385, playerRect.y+190, 350, 50), "<size=28>Medals</size>", "BoldText");
		
		GUI.Label(new Rect(playerRect.x+340, playerRect.y+230, 180, 100), "<size=80>+" + Painter.Manager.CurrentPlayer.GetMedalsScore() + "</size>", "ScoreNumber");
		GUI.Label(new Rect(playerRect.x+360, playerRect.y+310, 180, 20), "<size=25>points</size>", "ScoreNumber");

		
		// Total Score
		GUI.Button(new Rect(4, height-118, 692, 114), "", "ScoreBackground");
		GUI.Label(new Rect(0, playerRect.y+380, width, 50), "<size=30>Total Score</size>", "BoldWhiteText");
		GUI.Label(new Rect(0, playerRect.y+420, width, 50), "<size=62>+" +
			(int)(Painter.Manager.CurrentPlayer.Score +
			Painter.Manager.CurrentPlayer.bonusMalus[0] +
			Painter.Manager.CurrentPlayer.bonusMalus[1] +
			Painter.Manager.CurrentPlayer.bonusMalus[2] +
			Painter.Manager.CurrentPlayer.GetAchievementsScore() +
			Painter.Manager.CurrentPlayer.GetMedalsScore()) + " points</size>", "ScoreNumberWhite");
		
		GUILayout.EndArea();
	}
	
	private void DrawInstructionsPanel()
	{
		int width = 700;
		int height = 600;
		Rect SliderRect = new Rect(width/2-35*2-20, height-70, 34, 34);
		string SliderStyle = "";
		Texture Instruction = Instructions1;
		
		GUI.BeginGroup(new Rect(Painter.SCREEN_WIDTH/2-width/2, Painter.SCREEN_HEIGHT/2-height/2, width, height), "", "box");
		GUI.Label(new Rect(0, -45, width, 200), "<size=60>TIPS & INSTRUCTIONS</size>", "GameTitle");
		if (GUI.Button(new Rect(width - 45, 12, 30, 30), "", "CloseButtonWindow"))
		{
			ShowInstructionsPanel = false;
			SlideInstruction = 0;
			Painter.Manager.CurrentScreen = GameManager.GameScreen.Intro;
		}
		
		switch (SlideInstruction)
		{
			case 0:
				Instruction = Instructions1;
				break;
			case 1:
				Instruction = Instructions2;
				break;
			case 2:
				Instruction = Instructions3;
				break;
			case 3:
				Instruction = Instructions4;
				break;
			case 4:
				Instruction = Instructions5;
				break;
		}
		
		GUI.DrawTexture(new Rect(30, 105, 639, 408), Instruction);
			
		for (int i = 0; i < 5; i++)
		{
			if (i == SlideInstruction)
				SliderStyle = "SlideButtonSel";
			else
				SliderStyle = "SlideButton";
			
			if (GUI.Button(new Rect(SliderRect.x+35*i, SliderRect.y, SliderRect.width, SliderRect.height), "", SliderStyle))
				SlideInstruction = i;
		}

		GUI.EndGroup();
	}
	
	private void DrawAggregator()
	{
		int width = 517, height = 539, i;
		Rect AchievRect = new Rect(45, 265, 350, 25);
		Rect medalRect = new Rect(45, 405, 350, 25);
		Player player = Painter.Manager.CurrentPlayer;
		
		GUI.BeginGroup(new Rect(0, 37, width, height));
		
		GUI.DrawTexture(new Rect(0, 0, width, height), Aggregator);
		
		// Player Info
		GUI.Label(new Rect(35, 15, width, 25), "<size=13>Time left: " + Mathf.CeilToInt(Painter.Manager.RemainingSeconds) / 60 +
			"min and " + Mathf.CeilToInt(Painter.Manager.RemainingSeconds) % 60 + "sec.</size>", "LeftWhiteText");
		GUI.Label(new Rect(35, 35, width, 25), "<size=13>Target process: " + Painter.Manager.ToElicitProcessName + ".</size>", "LeftWhiteText");
		GUI.Label(new Rect(35, 85, width, 25), "<size=18>SCORE: " + Painter.Manager.CurrentPlayer.Score + " points</size>", "LeftBoldWhiteText");
		
		// Statistics
		GUI.Label(new Rect(35, 132, width, 25), "<size=13>- " + (player.NumDraftProcesses + player.NumPubProcesses) +
			((player.NumDraftProcesses + player.NumPubProcesses) == 1 ? " process and " : " processes and ") + (player.NumDraftVersions + player.NumPubVersions) +
			((player.NumDraftVersions + player.NumPubVersions) == 1 ? " version created.</size>" : " versions created.</size>"), "LeftWhiteText");
		GUI.Label(new Rect(35, 152, width, 25), "<size=13>- " + (player.NumVotesProcesses + player.NumVotesVersions) +
			" quality and duplication votes placed.</size>", "LeftWhiteText");
		GUI.Label(new Rect(35, 172, width, 25), "<size=13>- " +
			((int)((player.ConvRateProcesses + player.ConvRateVersions)/((player.ConvRateProcesses == 0 || player.ConvRateVersions == 0) ? 1 : 2))*100) +
			"% of convergence rate of your placed votes.</size>", "LeftWhiteText");
		GUI.Label(new Rect(35, 192, width, 25), "<size=13>- 0 of your processes are marked as duplicate.</size>", "LeftWhiteText");
		GUI.Label(new Rect(35, 212, width, 25), "<size=13>- 0 processes you marked as duplicate.</size>", "LeftWhiteText");
		
		//Achievements
		if (player.Achievements.Count == 0)
			GUI.Label(new Rect(0, 310, width-50, 25), "<size=17>You have no achievements yet.</size>", "NormalWhiteText");
		else
		{
			i = 1;
			foreach (Achievement achiev in player.Achievements)
			{
				GUI.Label(new Rect(AchievRect.x, AchievRect.y, AchievRect.width, AchievRect.height),
					"<size=14>" + i++ + ". " + achiev.Name + "</size>", "LeftWhiteText");
				AchievRect.y += 23;
			}
		}
		
		// Medals
		if (player.Medals.Count == 0)
			GUI.Label(new Rect(0, 450, width-50, 25), "<size=17>You have no medals yet.</size>", "NormalWhiteText");
		else
		{
			i = 1;
			foreach (Medal medal in player.Medals)
			{
				GUI.Label(new Rect(medalRect.x, medalRect.y, medalRect.width, medalRect.height),
					"<size=14>" + i++ + ". " + medal.Name + "</size>", "LeftWhiteText");
				medalRect.y += 23;
			}
		}
		
		GUI.EndGroup();
	}
	
	private void DrawDuplicationWindow()
	{
		string username = Painter.Manager.CurrentPlayer.Username;
		int listElems = 0;
		Process process;
		if (Painter.Manager.CurrentScreen == GameManager.GameScreen.ViewProcess)
			process = Painter.Manager.GameState.GetProcess(Painter.Manager.CurrentProcess.PID);
		else
			process = Painter.Manager.GameState.GetTargetProcess(Painter.Manager.CurrentProcess.PID, Painter.Manager.CurrentVersion.PVID);
		int width = 400, height = (!process.markedDuplication) ? 550 : 270;
		
		foreach (Process countProcess in Painter.Manager.GameState.LocalProcesses)
		{
			if (countProcess.published)
				listElems++;
			
			foreach (ProcessVersion countVersion in countProcess.Versions)
			{
				if (countVersion.published)
					listElems++;
			}
		}
		
		Painter.DrawOverlay();
		GUI.BeginGroup(new Rect(Painter.SCREEN_WIDTH/2-width/2, Painter.SCREEN_HEIGHT/2-height/2-20, width, height), "", "DuplicationWindow");
		if (GUI.Button(new Rect(360, 12, 30, 30), "", "CloseButtonWindow"))
		{
			duplicationWindow = false;
			markDupMessage = "";
		}
		
		if (process.markedDuplication)
		{
			// Player marked as duplicate
			if (process.markAuthor.Equals(username))
			{
				GUI.Label(new Rect(0, 50, 404, 60),
					"<size=25><b>You marked this process\n as a duplication of " +
					((process.duplicationPVID == -1) ?
						("process " + process.duplicationPID) :
						("version " + process.duplicationPVID +" (P" + process.duplicationPID + ")")) + ".</b></size>", "Center");
				GUI.Label(new Rect(0, 120, 404, 30),
					"<size=18>Yes: " + process.posDuplicationVotes + " , " +
					"No: " + process.negDuplicationVotes + "</size>", "Center");
				if (GUI.Button(new Rect(135, 180, 132, 50), "Okay", "GreenButton"))
					duplicationWindow = false;
			}
			else
			{
				// Player already voted
				if (process.PlayerAlreadyVoted(username, "Duplication"))
				{
					if (process.GetPlayerVote(username, "Duplication"))
						GUI.Label(new Rect(0, 60, 404, 60),
							"<size=25><b>You voted\n process " + process.PID + " as a duplication.</b></size>", "Center");
					else
						GUI.Label(new Rect(0, 60, 404, 60),
							"<size=25><b>You voted\n process " + process.PID + " as not a duplication.</b></size>", "Center");
					
					GUI.Label(new Rect(0, 125, 404, 30),
						"<size=18>Yes: " + process.posDuplicationVotes + " , " +
						"No: " + process.negDuplicationVotes + "</size>", "Center");
					if (GUI.Button(new Rect(135, 170, 132, 50), "Okay", "GreenButton"))
						duplicationWindow = false;
				}
				// Player did not vote
				else
				{
					GUI.Label(new Rect(0, 70, 404, 30),
						"<size=25><b>Is process " + process.PID + " a duplication?</b></size>", "Center");
					if (GUI.Button(new Rect(65, 170, 132, 50), "Yes", "GreenButton"))
						Mechanics.VoteDuplicationProcess(process.PID,
							Painter.Manager.CurrentScreen == GameManager.GameScreen.ViewProcess ? -1 : Painter.Manager.CurrentVersion.PVID,
							true, username);
					if (GUI.Button(new Rect(205, 170, 132, 50), "No", "RedButton"))
						Mechanics.VoteDuplicationProcess(process.PID,
							Painter.Manager.CurrentScreen == GameManager.GameScreen.ViewProcess ? -1 : Painter.Manager.CurrentVersion.PVID,
							false, username);
				}
			}
		}
		else
		{
			GUI.Label(new Rect(0, 10, width, 40), "<size=40>MARK AS DUPLICATION</size>", "GameTitle");
			GUI.Label(new Rect(0, 65, width, 30), "<size=18>Which of the following is the " +
				"<b>original process</b>?</size>", "Center");
			
			ScrollDuplicates = GUI.BeginScrollView(new Rect(20, 110, width-30, height-210),
						ScrollDuplicates,
						new Rect(0, 0, width-35, listElems*30+120),
						GUIStyle.none, new GUIStyle(GUI.skin.verticalScrollbar));
			Rect nextListItem = new Rect(0, 0, width-35-15, 41);
			
			foreach (Process listProcess in Painter.Manager.GameState.LocalProcesses)
			{
				if (Painter.Manager.CurrentScreen == GameManager.GameScreen.ViewProcess &&
					listProcess.PID == Painter.Manager.CurrentProcess.PID) continue;
				
				if (originalPID == listProcess.PID && originalPVID == -1)
					DuplicateListStyle = "ListDuplicateItemSel";
				else
					DuplicateListStyle = "ListDuplicateItem";
				
				if (GUI.Button(nextListItem, "Process " + listProcess.PID + ": " + listProcess.Name, DuplicateListStyle))
				{
					originalPID = listProcess.PID;
					originalPVID = -1;
				}
				nextListItem.y += 41;
				
				foreach (ProcessVersion listVersion in listProcess.Versions)
				{
					if ((listProcess.PID == Painter.Manager.CurrentProcess.PID &&
						Painter.Manager.CurrentVersion != null && listVersion.PVID == Painter.Manager.CurrentVersion.PVID) || !listVersion.published)
						continue;
					
					if (originalPID == listProcess.PID && originalPVID == listVersion.PVID)
						DuplicateListStyle = "ListDuplicateItemSel";
					else
						DuplicateListStyle = "ListDuplicateItem";
					
					if (GUI.Button(new Rect(35, nextListItem.y, nextListItem.width-35, nextListItem.height), "Version " + listVersion.PVID, DuplicateListStyle))
					{
						originalPID = listProcess.PID;
						originalPVID = listVersion.PVID;
					}
					nextListItem.y += 41;
				}
			}
			
			GUI.EndScrollView();
			
			if (GUI.Button(new Rect(width/2-105, height-52, 100, 40), "Mark", "GreenButton"))
			{
				Mechanics.MarkAsDuplicated(process.PID,
					Painter.Manager.CurrentScreen == GameManager.GameScreen.ViewProcess ? -1 : Painter.Manager.CurrentVersion.PVID,
					originalPID, originalPVID, Painter.Manager.CurrentPlayer.Username);
				if (markDupMessage.Equals(""))
					duplicationWindow = false;
			}
			if (GUI.Button(new Rect(width/2+5, height-52, 100, 40), "Cancel", "RedButton"))
			{
				duplicationWindow = false;
				markDupMessage = "";
			}
			
			GUI.Label(new Rect(0, height-105, width, 50), markDupMessage, "Center");
		}
		GUI.EndGroup();
	}
	
}