using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Painter : MonoBehaviour
{
	
	public static int SCREEN_WIDTH = 1280;
	public static int SCREEN_HEIGHT = 700;
	int bottomLayerNumber = 10;
	
	private GUISkin MyStyle;
	private Texture LoopArrow, LoopArrowSel;
	private static Texture Overlay, OverlayDarker;
	private Texture Crosshair, ArrowHead, ArrowInfoHead, ArrowHeadSel, toolbarBackground;
	private Texture2D lineTex, lineInfoTex, lineTexSel;
	
	public static GameManager Manager { get; set; }
	public static Client ClientManager;
	private bool showEventTools, showActTools, showFlowTools;
	private bool flowMode, determinedSource, determinedFlow;
	private string flowType;
	public static bool invalidPos, removeWindow, laneParticWindow;
	private Vector2 oldPrimitivePos;
	public Primitive clickSource, clickTarget;
	public static Vector2 scrollPos = Vector2.zero;
	
	public static string activityName, description, subPrimitiveName, condition, lanePartic, ProcessName;
	public static bool UpdateMode, LoadedState, EnableProcessEdition = true, GameOverRequest;
	
	public void Start()
	{
		MyStyle = (GUISkin)Resources.Load("OrganixGUI");
		lineTex = (Texture2D)Resources.Load("Images/LineTex");
		lineTexSel = (Texture2D)Resources.Load("Images/LineTexSel");
		lineInfoTex = (Texture2D)Resources.Load("Images/LineInfoTex");
		ArrowHead = (Texture)Resources.Load("Images/Meta-Language/ArrowHead");
		ArrowHeadSel = (Texture)Resources.Load("Images/Meta-Language/ArrowHeadSel");
		ArrowInfoHead = (Texture)Resources.Load("Images/Meta-Language/ArrowHeadTex");
		toolbarBackground = (Texture)Resources.Load("Images/Toolbar/toolbarBackground");
		LoopArrow = (Texture)Resources.Load("Images/Meta-Language/LoopArrow");
		LoopArrowSel = (Texture)Resources.Load("Images/Meta-Language/LoopArrowSel");
		Overlay = (Texture)Resources.Load("Images/Overlay");
		OverlayDarker = (Texture)Resources.Load("Images/OverlayDarker");
		Crosshair = (Texture)Resources.Load("Images/crossCursor");		
		ClientManager = GameObject.Find("Main").GetComponent<Client>();
		
		activityName = description = subPrimitiveName = condition = lanePartic = ProcessName = flowType = "";
		Manager = new GameManager();
	}
	
	public void Update()
	{
		if (Manager.CurrentPlayer != null)
		{
			if (Manager.GameOn && Manager.RemainingSeconds > 0)
				Manager.RemainingSeconds = Manager.GameLength - Time.time;
			else
			{
				PaintPanels.HidePanels();
				Mechanics.SignalGameTimeout(Manager.CurrentPlayer.Username);
				Manager.CurrentPlayer.GameOver = true;
				if (Manager.GameOn)
					PaintPanels.ShowGameOver = true;
				EnableProcessEdition = false;
				Manager.GameOn = false;
			}
		}
	}

	public void OnGUI()
	{
		GUI.depth = bottomLayerNumber;
		GUI.skin = MyStyle;
		
		DrawBackground();
		
		switch (Manager.CurrentScreen)
		{
			case GameManager.GameScreen.Intro :
				DrawIntro();
				break;
				
			case GameManager.GameScreen.Login :
				if (UnityEngine.Event.current.Equals(UnityEngine.Event.KeyboardEvent("return")))
				{
					ClientManager.AttemptedLogIn = ClientManager.Username;
					networkView.RPC("Login", RPCMode.Server, ClientManager.Username, ClientManager.Password);
				}
				DrawLogin();
				if (ClientManager.LoggedIn == true)
					Manager.CurrentScreen = GameManager.GameScreen.Home;
				break;
				
			case GameManager.GameScreen.Home :
				DrawProcesses();
				DrawToolbarHome();
				break;
				
			case GameManager.GameScreen.ProcessCreation :
				DrawBoard(Manager.CurrentProcess);
				DrawToolBar();
				if (removeWindow) DrawRemoveWindow();
				else if (laneParticWindow) DrawLaneNameWindow();
				break;
				
			case GameManager.GameScreen.ComposedCreation :
				DrawComposedBoard();
				DrawToolBar();
				break;
				
			case GameManager.GameScreen.AdHocCreation :
				DrawAdHocBoard();
				DrawToolBar();
				break;
				
			case GameManager.GameScreen.VersionComposedCreation :
				DrawComposedBoard();
				DrawToolBar();
				break;
				
			case GameManager.GameScreen.VersionAdHocCreation :
				DrawAdHocBoard();
				DrawToolBar();
				break;
				
			case GameManager.GameScreen.ViewProcess :
				EnableProcessEdition = false;
				DrawBoard(Manager.CurrentProcess);
				DrawToolBar();
				if (!PaintPanels.ShowVersionsPanel) PaintPanels.ShowVersionsPanel = true;
				break;
				
			case GameManager.GameScreen.ViewComposed :
				DrawComposedBoard();
				DrawToolBar();
				break;
				
			case GameManager.GameScreen.ViewAdHoc :
				DrawAdHocBoard();
				DrawToolBar();
				break;
				
			case GameManager.GameScreen.ProcessVersionCreate :
				DrawBoard(Manager.CurrentVersion);
				DrawToolBar();
				if (!PaintPanels.ShowVersionsPanel) PaintPanels.ShowVersionsPanel = true;
				
				else if (removeWindow) DrawRemoveWindow();
				else if (laneParticWindow) DrawLaneNameWindow();
				break;
				
			case GameManager.GameScreen.GameOver :
				break;
				
			case GameManager.GameScreen.Instructions:
				break;
		}
				
		if (Manager.CurrentProcess != null && Manager.CurrentProcess.published &&
			Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation &&
			!PaintPanels.ShowVersionsPanel)
			PaintPanels.ShowVersionsPanel = true;
		
		if (UnityEngine.Event.current.type == EventType.KeyUp &&
			UnityEngine.Event.current.keyCode == KeyCode.F1)
			PaintPanels.ShowHelpPanel = !PaintPanels.ShowHelpPanel;
		
		if (UnityEngine.Event.current.type == EventType.KeyUp &&
			UnityEngine.Event.current.keyCode == KeyCode.Escape)
			PaintPanels.PlayerEscaped = true;
		
		if (UnityEngine.Event.current.type == EventType.KeyUp &&
			UnityEngine.Event.current.keyCode == KeyCode.F2)
			PaintPanels.ShowScoresPanel = !PaintPanels.ShowScoresPanel;
		
		if (UpdateMode) DrawLoadingWindow();
				
		if (flowMode)
		{
			GUI.DrawTexture(new Rect(Input.mousePosition.x-11, Screen.height-Input.mousePosition.y-11, 22, 22), Crosshair);
			Screen.showCursor = false;
		}
		else
			Screen.showCursor = true;
		
		if (PaintPanels.processNameWindow && UnityEngine.Event.current.type == EventType.KeyUp &&
			UnityEngine.Event.current.keyCode == KeyCode.Return)
		{
			LanguageConstructor.EditProcess(Manager.CurrentProcess.PID, ProcessName, Manager.CurrentProcess.Description);
			PaintPanels.processNameWindow = false;
		}
	}
	
	private void DrawLogin()
	{
		float width = 270, height = 250;
		
		GUI.BeginGroup(new Rect((SCREEN_WIDTH-width)/2, (SCREEN_HEIGHT-height)/2-20, width, height), "", "box");
		GUI.Label(new Rect(width/2-45, 15, 90, 30), "Username", "BlackBoldLeftText");
		ClientManager.Username = GUI.TextField(new Rect(25, 50, width-50, 30), ClientManager.Username, "TextFieldWindow");
		
		GUI.Label(new Rect(width/2-45, 85, width, 30), "Password", "BlackBoldLeftText");
		ClientManager.Password = GUI.PasswordField(new Rect(25, 120, width-50, 30), ClientManager.Password, "*"[0], "TextFieldWindow");
		
		if (GUI.Button(new Rect(width/2-65, height - 75, 132, 50), "Login", "GreenButton"))
		{
			ClientManager.AttemptedLogIn = ClientManager.Username;
			networkView.RPC("Login", RPCMode.Server, ClientManager.Username, ClientManager.Password);
		}
		
		if (!ClientManager.LoginStatus.Equals(""))
			GUI.Button(new Rect(0, height-24, width, 10), "<size=12>" + ClientManager.LoginStatus + "</size>", "Center");
		GUI.EndGroup();
	}
	
	private void DrawIntro()
	{
		GUILayout.BeginArea(new Rect(SCREEN_WIDTH/2-135, SCREEN_HEIGHT/2-160, 270, 280), "", "box");
		GUI.Label(new Rect(0, -55, 270, 200), "ORGANIX", "GameTitle");
		
		if (GUI.Button(new Rect(70, 90, 132, 50), "Play!", "GreenButton"))
			Manager.CurrentScreen = GameManager.GameScreen.Login;
		if (GUI.Button(new Rect(70, 145, 132, 50), "Tips", "GreyButton"))
		{
			Manager.CurrentScreen = GameManager.GameScreen.Instructions;
			PaintPanels.ShowInstructionsPanel = true;
		}
		if (GUI.Button(new Rect(70, 200, 132, 50), "Quit", "GreyButton"))
		{
			Application.Quit();
		}
		GUILayout.EndArea();
	}

	private void DrawBackground()
	{
		Texture2D background = (Texture2D)Resources.Load("Images/background");
		int x, y;
		int dim = 200;
		for (x = 0; x < SCREEN_WIDTH; x += dim)
			for (y = 0; y < SCREEN_HEIGHT; y += dim)
				GUI.DrawTexture(new Rect(x, y, dim, dim), background);
	}
	
	private void DrawToolbarHome()
	{
		string username = Manager.CurrentPlayer.Username;
		
		for (int i = 0; i < SCREEN_WIDTH; i += 245)
			GUI.DrawTexture(new Rect(i, 0, 245, 43), toolbarBackground);
		
		if (GUI.Button(new Rect(0, 0, 43, 37), "", "PlayerInfoButton"))
			PaintPanels.ShowAggregator = !PaintPanels.ShowAggregator;
		
		GUI.Label(new Rect(30, 0, 100, 30), "<size=13>Logged as</size>", "NormalWhiteText");
		GUI.Label(new Rect(114, 0, 200, 30), "<size=15>" + username + "</size>", "LeftBoldWhiteText");
		
		if (GUI.Button(new Rect(SCREEN_WIDTH-43, 0, 43, 37), "", "LogoutButton"))
		{
			networkView.RPC("LogoutAck", RPCMode.Server, Manager.CurrentPlayer.Username);
			Manager.CurrentPlayer = null;
			ClientManager.Username = "";
			ClientManager.LoggedIn = false;
			ClientManager.AttemptedLogIn = "";
			Manager.CurrentScreen = GameManager.GameScreen.Intro;
		}
	}
	
	private void DrawToolBar()
	{
		string username = Painter.Manager.CurrentPlayer.Username;
		
		for (int i = 0; i < SCREEN_WIDTH; i += 245)
			GUI.DrawTexture(new Rect(i, 0, 245, 43), toolbarBackground);
		
		// Manage primitive
		if (GUI.Button(new Rect(0, 0, 43, 37), "", "BackButton"))
		{
			if (Manager.CurrentScreen == GameManager.GameScreen.AdHocCreation ||
				Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation)
			{
				ResetToolbarView();
				Manager.CurrentScreen = GameManager.GameScreen.ProcessCreation;
			}
			else if (Manager.CurrentScreen == GameManager.GameScreen.ViewComposed ||
				Manager.CurrentScreen == GameManager.GameScreen.ViewAdHoc)
			{
				ResetToolbarView();
				Manager.CurrentScreen = GameManager.GameScreen.ViewProcess;
			}
			else if (Manager.CurrentScreen == GameManager.GameScreen.VersionAdHocCreation ||
				Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
			{
				ResetToolbarView();
				Manager.CurrentScreen = GameManager.GameScreen.ProcessVersionCreate;
			}
			else
			{
				ResetToolbarView();
				Manager.CurrentScreen = GameManager.GameScreen.Home;
				flowMode = false;
			}
		}
		
		if (Manager.GameOn && flowMode)
		{
			if (GUI.Button(new Rect(42, 0, 51, 37), "", "EndFlowButton"))
			{
				ResetToolbarView();
				flowMode = false;
			}
		}
		
		else if (Manager.GameOn && EnableProcessEdition &&
			(Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate ||
			Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation) && GUI.Button(new Rect(42, 0, 51, 37), "", "RemoveButton"))
			removeWindow = true;
		
		// Voting processes
		Process process;
		if ((Manager.CurrentScreen == GameManager.GameScreen.ViewProcess ||
			Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation) &&
			Manager.CurrentProcess != null)
			process = Manager.GameState.GetProcess(Manager.CurrentProcess.PID);
		else if (Manager.CurrentProcess != null && Manager.CurrentVersion != null)
			process = Manager.GameState.GetTargetProcess(Manager.CurrentProcess.PID, Manager.CurrentVersion.PVID);
		else process = null;
		
		if (Manager.GameOn && (Manager.CurrentScreen == GameManager.GameScreen.ViewProcess ||
			Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate) &&
			process != null && process.published && !EnableProcessEdition)
		{			
			// Voting for quality
			if (Manager.CurrentScreen == GameManager.GameScreen.ViewProcess &&
				(Manager.CurrentProcess.PlayerAlreadyVoted(username, "Quality") ||
				Manager.CurrentProcess.Author.Equals(Manager.CurrentPlayer.Username)))
			{
				GUI.Button(new Rect(Screen.width-85, 0, 43, 37), "" + Manager.CurrentProcess.posVotes, "LikeButtonVoted");
				GUI.Button(new Rect(Screen.width-43, 0, 43, 37), "" + Manager.CurrentProcess.negVotes, "DislikeButtonVoted");
			}
			else if (Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate &&
				(Manager.CurrentVersion.PlayerAlreadyVoted(username, "Quality") ||
				Manager.CurrentVersion.Author.Equals(Manager.CurrentPlayer.Username)))
			{
				GUI.Button(new Rect(Screen.width-85, 0, 43, 37), "" + Manager.CurrentVersion.posVotes, "LikeButtonVoted");
				GUI.Button(new Rect(Screen.width-43, 0, 43, 37), "" + Manager.CurrentVersion.negVotes, "DislikeButtonVoted");
			}
			else
			{
				if (GUI.Button(new Rect(SCREEN_WIDTH-85, 0, 43, 37), "", "LikeButton"))
				{
					if (Manager.CurrentScreen == GameManager.GameScreen.ViewProcess)
						Mechanics.VoteQualityProcess(Manager.CurrentProcess.PID, true, username);
					else
						Mechanics.VoteQualityVersion(username, Manager.CurrentProcess.PID, Manager.CurrentVersion.PVID, true);
				}
				if (GUI.Button(new Rect(SCREEN_WIDTH-43, 0, 43, 37), "", "DislikeButton"))
				{
					if (Manager.CurrentScreen == GameManager.GameScreen.ViewProcess)
						Mechanics.VoteQualityProcess(Manager.CurrentProcess.PID, false, username);
					else
						Mechanics.VoteQualityVersion(username, Manager.CurrentProcess.PID, Manager.CurrentVersion.PVID, false);
				}
			}
			
			// Voting for duplication
			if (Manager.GameOn && !process.markedDuplication)
			{
				if (GUI.Button(new Rect(Screen.width-185, 0, 78, 37), "", "MarkDuplicateButton"))
					PaintPanels.duplicationWindow = true;
			}
			else if (Manager.GameOn && !process.PlayerAlreadyVoted(username, "Duplication") &&
				!process.markAuthor.Equals(username))
			{
				if (GUI.Button(new Rect(Screen.width-185, 0, 78, 37), "", "VoteDuplicateButton"))
					PaintPanels.duplicationWindow = true;
			}
			else if (Manager.GameOn)
			{
				if (GUI.Button(new Rect(Screen.width-185, 0, 78, 37), "", "VotedDuplicateButton"))
					PaintPanels.duplicationWindow = true;
				GUI.Label(new Rect(Screen.width-173, 5, 30, 20), "" + process.posDuplicationVotes, "VoteText");
				GUI.Label(new Rect(Screen.width-133, 5, 30, 20), "" + process.negDuplicationVotes, "VoteText");
			}
		}
		
		if (Manager.GameOn && Manager.CurrentProcess != null && !Manager.CurrentProcess.published &&
		    Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation)
		{
			if (GUI.Button (new Rect (SCREEN_WIDTH - 78, 0, 50, 37), "", "PublishButton"))
			{
				Mechanics.PublishProcess (username, Manager.CurrentProcess.PID, -1);
				ResetToolbarView ();
			}
		}
		
		if (Manager.GameOn && Manager.CurrentVersion != null && !Manager.CurrentVersion.published &&
		    Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
		{
			if (GUI.Button (new Rect (SCREEN_WIDTH - 78, 0, 50, 37), "", "PublishButton"))
			{
				Mechanics.PublishProcess (username, Manager.CurrentProcess.PID, Manager.CurrentVersion.PVID);
				ResetToolbarView ();
			}
		}
		
		// + LANE
		if (Manager.GameOn && EnableProcessEdition && (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
			Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate))
		{
			if (GUI.Button(new Rect(Screen.width/2-202, 0, 101, 37), "", "AddLaneButton"))
			{
				ResetToolbarView();
				string participant = "Participant";
				LanguageConstructor.AddLane(Manager.CurrentProcess.PID,
					Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
					participant);
			}
		}
		
		// + EVENT
		Rect eventButtonRect;
		if (Manager.GameOn && EnableProcessEdition && Manager.CurrentScreen != GameManager.GameScreen.AdHocCreation &&
			Manager.CurrentScreen != GameManager.GameScreen.VersionAdHocCreation)
		{
			if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
				Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
				eventButtonRect = new Rect(Screen.width/2-102, 0, 101, 37);
			else
				eventButtonRect = new Rect(Screen.width/2-150, 0, 101, 37);
			
			if (GUI.Button(eventButtonRect, "", "AddEventButton"))
			{
				ResetToolbarView();
				showEventTools = true;
			}
			if (showEventTools)
			{
				if (GUI.Button(new Rect(eventButtonRect.x, 36, 101, 37), "", "AddMergeButton"))
				{
					showEventTools = false;
					if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
						Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
						LanguageConstructor.AddMerge(Manager.CurrentProcess.PID,
							Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
							Manager.CurrentLane.PrID);
					else if (Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
						Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
						LanguageConstructor.AddEventComposed(Manager.CurrentProcess.PID,
							Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ? -1 : Manager.CurrentVersion.PVID,
							Manager.CurrentLane.PrID, Manager.InspectorPrimitive.PrID, "Merge");
				}
			}
		}
		
		// + ACTIVITY
		Rect activityButtonRect;
		// Screen: Process Creation
		if (Manager.GameOn && EnableProcessEdition && (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
			Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate))
		{
			activityButtonRect = new Rect(Screen.width/2-2, 0, 101, 37);
			
			if (GUI.Button(activityButtonRect, "", "AddActivityButton"))
			{
				ResetToolbarView();
				showActTools = true;
			}
			
			if (showActTools)
			{
				if (GUI.Button(new Rect(activityButtonRect.x, 36, 101, 37), "", "AddWorkButton"))
				{
					showActTools = false;
					LanguageConstructor.AddActivity(Manager.CurrentProcess.PID,
						Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
						Manager.CurrentLane.PrID, "");
				}
				if (GUI.Button(new Rect(activityButtonRect.x, 71, 101, 37), "", "AddComposedButton"))
				{
					showActTools = false;
					LanguageConstructor.AddComposedActivity(Manager.CurrentProcess.PID,
						Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
						Manager.CurrentLane.PrID, "");
				}
				if (GUI.Button(new Rect(activityButtonRect.x, 106, 101, 37), "", "AddAdHocButton"))
				{
					showActTools = false;
					LanguageConstructor.AddAdHocActivity(Manager.CurrentProcess.PID,
						Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
						Manager.CurrentLane.PrID);
				}
			}
		}
		// Screen: Composed Activity Creation
		else if (Manager.GameOn && EnableProcessEdition && (Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
			Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation))
		{
			activityButtonRect = new Rect(Screen.width/2-50, 0, 101, 37);
			
			if (GUI.Button(activityButtonRect, "", "AddActivityButton"))
			{
				LanguageConstructor.AddComposedSubActivity(Manager.CurrentProcess.PID,
					Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ? -1 : Manager.CurrentVersion.PVID,
					Manager.CurrentLane.PrID, Manager.InspectorPrimitive.PrID, "");
			}
		}
		// Screen: Ad-Hoc Activity Creation
		else if (Manager.GameOn && EnableProcessEdition && (Manager.CurrentScreen == GameManager.GameScreen.AdHocCreation ||
			Manager.CurrentScreen == GameManager.GameScreen.VersionAdHocCreation))
		{
			activityButtonRect = new Rect(Screen.width/2-50, 0, 101, 37);
			
			if (GUI.Button(activityButtonRect, "", "AddActivityButton"))
			{
				LanguageConstructor.AddAdHocSubActivity(Manager.CurrentProcess.PID,
					Manager.CurrentScreen == GameManager.GameScreen.AdHocCreation ? -1 : Manager.CurrentVersion.PVID,
					Manager.CurrentLane.PrID, Manager.InspectorPrimitive.PrID);
			}
		}
		
		
		// + FLOW
		Rect flowButtonRect;
		if (Manager.GameOn && EnableProcessEdition && Manager.CurrentScreen != GameManager.GameScreen.AdHocCreation &&
			Manager.CurrentScreen != GameManager.GameScreen.VersionAdHocCreation)
		{
			if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
				Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
				flowButtonRect = new Rect(Screen.width/2 + 98, 0, 101, 37);
			else
				flowButtonRect = new Rect(Screen.width/2 + 50, 0, 101, 37);
			
			if (EnableProcessEdition && GUI.Button(flowButtonRect, "", "AddFlowButton"))
			{
				ResetToolbarView();
				
				if (Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
				{
					flowMode = true;
					flowType = "Sequence";
				}
				else
					showFlowTools = true;
			}
			
			if (showFlowTools && (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation || 
				Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate))
			{
				if (GUI.Button(new Rect(flowButtonRect.x, 36, 101, 37), "", "AddSequenceButton"))
				{
					showFlowTools = false;
					flowType = "Sequence";
					flowMode = true;
				}
				if (GUI.Button(new Rect(flowButtonRect.x, 71, 101, 37), "", "AddInformationButton"))
				{
					showFlowTools = false;
					flowType = "Information";
					flowMode = true;
				}
			}
			
			if (determinedFlow)
			{
				if (Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
					LanguageConstructor.AddConnectionComposed(Manager.CurrentProcess.PID,
						Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ? -1 : Manager.CurrentVersion.PVID,
						Manager.CurrentLane.PrID, Manager.InspectorPrimitive.PrID, clickSource.PrID, clickTarget.PrID, "");
				else
					LanguageConstructor.AddConnection(Manager.CurrentProcess.PID,
						Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate ? Manager.CurrentVersion.PVID : -1,
						clickSource.PrID, clickTarget.PrID, "",
						flowType.Equals("Sequence") ? "Sequence" : "Information");
				
				determinedFlow = false;
			}
		}
	}

	private void ResetToolbarView()
	{
		showEventTools = showActTools = showFlowTools = flowMode = determinedFlow = determinedSource = false;
		clickSource = clickTarget = null;
		removeWindow = PaintPanels.duplicationWindow = false;
		PaintEditName.changeName = PaintEditName.changeSubName = PaintEditName.changeCondition = false;
	}
	
	private void DrawBoard(Process process)
	{
		if (process != null)
		{
			if (process.published && process.Author.Equals(Manager.CurrentPlayer.Username))
				GUI.Label(new Rect(15, 45, 200, 20), "Author: " +
					(process.Author.Equals(Manager.CurrentPlayer.Username) ?
					process.Author + " <size=16>(You)</size>" : process.Author),
					"SectionTitle");
			
			if (process.markedDuplication)
			{
				int x;
				if (process.published && process.Author.Equals(Manager.CurrentPlayer.Username))
					x = 170;
				else
					x = 15;
				GUI.Label (new Rect (x, 45, 200, 20), "Marked as duplicate of P" +
				process.duplicationPID + ((process.duplicationPVID != -1) ? " V" + process.duplicationPVID : ""),
					"SectionTitle");
			}
			
			
			scrollPos = GUI.BeginScrollView(new Rect(15, 80, SCREEN_WIDTH-30, SCREEN_HEIGHT-100),
							scrollPos,
							new Rect(0, 0, SCREEN_WIDTH-50, process.Pool.Count*180+30));
			DrawProcess(process);
			
			GUI.EndScrollView();
			
			if (process.Pool.Count > 3)
			{
				Matrix4x4 backupMatrix = GUI.matrix;
				GUIUtility.RotateAroundPivot(270f, new Vector2(15, SCREEN_HEIGHT));
				Rect poolLabelRect = new Rect(15, SCREEN_HEIGHT, SCREEN_HEIGHT, 30);
				GUI.Label(poolLabelRect, "Process " + process.PID + ": " + process.Name, "PoolLabelText");
				GUI.matrix = backupMatrix;
			}
		}
	}
	
	private void DrawComposedBoard()
	{
		GUI.BeginGroup(new Rect(15, 80, SCREEN_WIDTH, SCREEN_HEIGHT-100));
		DrawComposedSubActivities(Manager.InspectorPrimitive as ComposedActivity);
		GUI.EndGroup();
	}
	
	private void DrawAdHocBoard()
	{
		GUI.BeginGroup(new Rect(15, 80, SCREEN_WIDTH, SCREEN_HEIGHT-100));
		DrawAdHocSubActivities(Manager.InspectorPrimitive as AdHocActivity);
		GUI.EndGroup();
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
	
	private void DrawSettingsWindow() {	DrawOverlay(); }
	
	private void DrawLoadingWindow()
	{
		DrawOverlay();
		GUI.BeginGroup(new Rect(Screen.width/2-202, Screen.height/2-200, 404, 279), "", "DuplicationWindow");
		GUI.Label(new Rect(0, 80, 404, 60), "<size=50><b>Loading...</b></size>", "Center");
		GUI.Label(new Rect(0, 135, 404, 60), "<size=20><b>Please wait.</b></size>", "Center");
		GUI.EndGroup();
	}
	
	private void DrawLaneNameWindow()
	{
		DrawOverlay();
		GUI.BeginGroup(new Rect(Screen.width/2-202, Screen.height/2-200, 404, 279), "", "DuplicationWindow");
		if (GUI.Button(new Rect(360, 12, 30, 30), "", "CloseButtonWindow"))
			laneParticWindow = false;
		GUI.Label(new Rect(0, 70, 404, 30),
			"<size=25><b>Business participant of lane</b></size>", "Center");
		lanePartic = GUI.TextField(new Rect(65, 120, 270, 50), lanePartic, "TextFieldWindow");
		if (GUI.Button(new Rect(160, 170, 90, 40), "Save", "GreenButton"))
		{
			LanguageConstructor.EditLane(Manager.CurrentProcess.PID,
				Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
				Manager.CurrentLane.PrID, lanePartic);
			laneParticWindow = false;
		}
		GUI.EndGroup();
	}
	
	private void DrawRemoveWindow()
	{
		DrawOverlay();
		GUI.BeginGroup(new Rect(Screen.width/2-202, Screen.height/2-200, 404, 279), "", "DuplicationWindow");
		if (GUI.Button(new Rect(360, 12, 30, 30), "", "CloseButtonWindow"))
			removeWindow = false;
		
		if (Manager.CurrentScreen == GameManager.GameScreen.ViewProcess ||
			Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation)
			GUI.Label(new Rect(0, 80, 404, 30),
				"<size=25><b>Remove process " + Manager.CurrentProcess.PID + "?</b></size>", "Center");
		else
			GUI.Label(new Rect(0, 80, 404, 30),
				"<size=25><b>Remove version " + Manager.CurrentVersion.PVID + "?</b></size>", "Center");
		if (GUI.Button(new Rect(65, 150, 132, 50), "Yes", "GreenButton"))
		{
			if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation)
				LanguageConstructor.RemoveProcess(Painter.Manager.CurrentProcess.PID, -1);
			else if (Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
				LanguageConstructor.RemoveProcess(Painter.Manager.CurrentProcess.PID, Painter.Manager.CurrentVersion.PVID);
			
			Manager.CurrentScreen = GameManager.GameScreen.Home;
			ResetToolbarView();
		}
		if (GUI.Button(new Rect(205, 150, 132, 50), "No", "RedButton"))
			removeWindow = false;
		GUI.EndGroup();
	}
	
	private void DrawProcesses()
	{
		GUI.BeginGroup(new Rect(15, 50, SCREEN_WIDTH-30, SCREEN_HEIGHT-60));

		Rect nextDraftButton = new Rect(0, 50, 200, 90);
		Rect nextProcButton = new Rect(0, nextDraftButton.y+160, 200, 90);
	
		GUI.Label(new Rect(0, 10, 100, 20), "Unpublished", "SectionTitle");
		GUI.Box(new Rect(0, 35, SCREEN_WIDTH-30, 1), "", "Separator");
				
		foreach (Process unpublished in Manager.GameState.LocalProcesses)
		{
			if (!unpublished.published)
			{
				if (unpublished.Author.Equals(Manager.CurrentPlayer.Username))
				{
					if (GUI.Button(nextDraftButton, "P" + unpublished.PID, "ProcessBtn"))
					{
						Manager.CurrentProcess = unpublished;
						Manager.CurrentScreen = GameManager.GameScreen.ProcessCreation;
						ProcessName = unpublished.Name;
						if (unpublished.Pool.Count != 0)
							Manager.CurrentLane = unpublished.Pool[0];
					}
					GUI.Label(new Rect(nextDraftButton.x+90, nextDraftButton.y, 110, 90), unpublished.Name, "ProcessButtonName");
					nextDraftButton.x += 210;
					if (nextDraftButton.x > SCREEN_WIDTH-200)
					{
						nextDraftButton.x = 0;
						nextDraftButton.y += 110;
					}
				}
			}
		}
		
		if (Manager.GameOn && GUI.Button(new Rect(nextDraftButton.x, nextDraftButton.y, 91, 90), "+", "AddProcessBtn"))
			LanguageConstructor.AddProcess(Manager.CurrentPlayer.Username, "", "");
		
		nextProcButton.y = nextDraftButton.y+120;
		
		GUI.Label(new Rect(0, nextProcButton.y, 100, 20), "Published", "SectionTitle");
		GUI.Box(new Rect(0, nextProcButton.y+25, SCREEN_WIDTH-30, 1), "", "Separator");
		
		nextProcButton.y += 45;
		
		foreach (Process process in Manager.GameState.LocalProcesses)
		{
			if (process.published && Manager.CurrentPlayer.NumPubProcesses > 0)
			{
				if (GUI.Button(nextProcButton, "P" + process.PID, "ProcessBtn"))
				{
					Manager.CurrentProcess = process;
					Manager.CurrentScreen = GameManager.GameScreen.ViewProcess;
					ProcessName = process.Name;
					if (process.Pool.Count != 0)
						Manager.CurrentLane = process.Pool[0];
				}
				GUI.Label(new Rect(nextProcButton.x + 90, nextProcButton.y, 110, 90), process.Name, "ProcessButtonName");
				GUI.Label(new Rect(nextProcButton.x + 90, nextProcButton.y+50, 110, 90),
					"<size=12>" + process.Versions.Count + (process.Versions.Count == 1 ? " Version</size>" : " Versions</size>"), "ProcessButtonName");
				if (process.finalConsensus) 
					GUI.Label(new Rect(nextProcButton.x + 170, nextProcButton.y+50, 110, 90),
						"<size=14>FC</size>", "ProcessButtonName");
				nextProcButton.x += 210;
				if (nextProcButton.x > SCREEN_WIDTH-200)
				{
					nextProcButton.x = 0;
					nextProcButton.y += 110;
				}
			}
		}
		
		GUI.EndGroup();
	}
	
	private void DrawProcess(Process process)
	{
		int laneHeight = 180;
		Rect nextPoolRect = new Rect(0, 0, SCREEN_WIDTH-60, laneHeight);
		
		// DRAWS LANES
		Matrix4x4 backupMatrix = GUI.matrix;
		if (process != null)
		{
			EnableProcessEdition = process.published ? false : true;
			
			foreach (Lane lane in process.Pool)
			{
				GUI.Box(new Rect(0, lane.y-laneHeight/2, SCREEN_WIDTH-60, laneHeight), "", "Lane");
								
				if (process.Pool.Count != 1)
				{
					Rect laneButtonRect = new Rect(29, lane.y-laneHeight/2, 25, laneHeight);
					string laneLabelStyle;
					
					if (lane.PrID == Manager.InspectorPrimitive.PrID)
						laneLabelStyle = "LaneLabelSel";
					else
						laneLabelStyle = "LaneLabel";
					
					if (GUI.Button(laneButtonRect, "", laneLabelStyle))
					{
						lanePartic = lane.Participant;
						Manager.CurrentLane = lane;
						Manager.InspectorPrimitive = lane;
					}
					else if (laneButtonRect.Contains(UnityEngine.Event.current.mousePosition) &&
						(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
						Input.GetMouseButtonUp(0) && EnableProcessEdition)
						laneParticWindow = true;
					
					GUIUtility.RotateAroundPivot(270f, new Vector2(29, lane.y+laneHeight/2));
					Rect laneRect = new Rect(29, lane.y+laneHeight/2, laneHeight, 25);
					GUI.Label(laneRect, lane.Participant, "Center");
					GUI.matrix = backupMatrix;
					
					if (UnityEngine.Event.current.type == EventType.KeyUp &&
						UnityEngine.Event.current.keyCode == KeyCode.Delete &&
						Manager.InspectorPrimitive is Lane &&
						lane.PrID == Manager.InspectorPrimitive.PrID && EnableProcessEdition)
					{
						LanguageConstructor.RemoveLane(Manager.CurrentProcess.PID,
							Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
							Manager.CurrentLane.PrID);
					}
				}
				
				nextPoolRect.y += laneHeight-1;
			}

			Rect poolLabelButtonRect = new Rect(0, 0, 30, laneHeight*process.Pool.Count-process.Pool.Count+1);
			GUI.Button(poolLabelButtonRect, "", "PoolLabel");
			if (poolLabelButtonRect.Contains(UnityEngine.Event.current.mousePosition) &&
				(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
				Input.GetMouseButtonUp(0) && EnableProcessEdition &&
				(Manager.CurrentScreen != GameManager.GameScreen.ProcessVersionCreate))
				PaintPanels.processNameWindow = true;
			
			if (process.Pool.Count <= 3)
			{
				GUIUtility.RotateAroundPivot(270f, new Vector2(0, laneHeight*process.Pool.Count));
				Rect poolLabelRect = new Rect(laneHeight*process.Pool.Count > SCREEN_HEIGHT-100 ? laneHeight*process.Pool.Count/4 : 0,
					laneHeight*process.Pool.Count,
					laneHeight*process.Pool.Count > SCREEN_HEIGHT-100 ? laneHeight*process.Pool.Count/2 : laneHeight*process.Pool.Count ,
					30);
				if (Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate ||
					Manager.CurrentScreen == GameManager.GameScreen.VersionAdHocCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
					GUI.Label(poolLabelRect, "Version " + Manager.CurrentVersion.PVID + ": " + process.Name, "PoolLabelText");
				else
					GUI.Label(poolLabelRect, "P" + process.PID + ": " + process.Name, "PoolLabelText");
				GUI.matrix = backupMatrix;
			}
			
			// DRAWS EDGE FLOWS
			foreach(Flow flow in process.Connections)
			{
				Primitive source, target;

				if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation || Manager.CurrentScreen == GameManager.GameScreen.ViewProcess)
				{
					source = Manager.GameState.GetPrimitive(process.PID, -1, flow.SourceID);
					target = Manager.GameState.GetPrimitive(process.PID, -1, flow.TargetID);
				}
				else
				{
					source = Manager.GameState.GetPrimitive((process as ProcessVersion).OriginalPID, (process as ProcessVersion).PVID, flow.SourceID);
					target = Manager.GameState.GetPrimitive((process as ProcessVersion).OriginalPID, (process as ProcessVersion).PVID, flow.TargetID);
				}
				
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
			
			// DRAWS LANGUAGE PRIMITIVES
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
					
					// Drop
					if (prim.dragging && Input.GetMouseButtonUp(1) && EnableProcessEdition)
					{
						prim.dragging = false;
						Rect laneRect = new Rect(0, lane.y-laneHeight/2, Screen.width-40, laneHeight);
						
						if (!laneRect.Contains(new Vector2(prim.x, prim.y)))
						{
							foreach (Lane l in process.Pool)
							{
								Rect rect = new Rect(0, l.y-laneHeight/2, Screen.width-40, laneHeight);
								if (rect.Contains(new Vector2(prim.x, prim.y)))
								{
									LanguageConstructor.ChangeLane(process.PID,
										Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
										prim.PrID, l.PrID);
									break;
								}
							}
						}
						
						Rect poolRect = new Rect(0, 0, Screen.width-40, process.Pool.Count*laneHeight);
						if (!poolRect.Contains(new Vector2(prim.x, prim.y)))
						{
							prim.x = oldPrimitivePos.x;
							prim.y = oldPrimitivePos.y;
							Manager.Notifications.Items.Add(
								new Notification(Notification.Type.Exception, "You cannot reposition primitives to outside the process pool."));
						}
						else
						{
							if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation)
								Mechanics.RepositionPrimitive(process.PID, -1, prim.PrID, prim.x, prim.y);
							else if (Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
								Mechanics.RepositionPrimitive((process as ProcessVersion).OriginalPID, (process as ProcessVersion).PVID, prim.PrID, prim.x, prim.y);
						}
					}
					// Drag
					else if (primitiveRect.Contains(UnityEngine.Event.current.mousePosition) &&
						Input.GetMouseButtonDown(1) && !flowMode && EnableProcessEdition)
					{
						oldPrimitivePos = new Vector2(prim.x, prim.y);
						prim.dragging = true;
					}
					// Moving position
					if (prim.dragging && EnableProcessEdition)
					{
						Vector3 dropPosition = Input.mousePosition;
						dropPosition.y -= scrollPos.y;
						prim.x = dropPosition.x-15;
						prim.y = Screen.height-dropPosition.y-80;
					}
					
					// EVENT
					if (prim is Event)
					{
						// Start Event
						if (((Event)prim).categ.Equals(Event.Categ.Start))
						{
							Event e = (Event)prim;
							string startStyle = "";
							
							if (prim.PrID == Manager.InspectorPrimitive.PrID ||
								(clickSource != null && prim.PrID == clickSource.PrID) || 
								(clickTarget != null && prim.PrID == clickTarget.PrID && EnableProcessEdition))
								startStyle = "StartSel";
							else
								startStyle = "StartEvent";
							
							if (GUI.Button(primitiveRect, "", startStyle))
							{
								PaintEditName.changeName = false;
								Manager.InspectorPrimitive = e;
								Manager.CurrentLane = lane;
								
								if (flowMode && !determinedSource)
								{
									clickSource = e;
									determinedSource = true;
								}
								else if (flowMode && determinedSource)
								{
									clickTarget = e;
									determinedFlow = true;
									determinedSource = false;
								}
							}
						}
						// End Event
						else if (((Event)prim).categ.Equals(Event.Categ.End))
						{
							Event e = (Event)prim;
							string endStyle = "";
							
							if (prim.PrID == Manager.InspectorPrimitive.PrID ||
								(clickSource != null && prim.PrID == clickSource.PrID) || 
								(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
								endStyle = "EndSel";
							else
								endStyle = "EndEvent";
							
							if (GUI.Button(primitiveRect, "", endStyle))
							{
								PaintEditName.changeName = false;
								Manager.InspectorPrimitive = e;
								Manager.CurrentLane = lane;
								if (flowMode && !determinedSource)
								{
									clickSource = e;
									determinedSource = true;
								}
								else if (flowMode && determinedSource)
								{
									clickTarget = e;
									determinedFlow = true;
									determinedSource = false;
								}
							}
						}
						// Merge Event
						else if (((Event)prim).categ.Equals(Event.Categ.Merge))
						{
							Event e = (Event)prim;
							string mergeStyle = "";
							
							if (prim.PrID == Manager.InspectorPrimitive.PrID ||
								(clickSource != null && prim.PrID == clickSource.PrID) || 
								(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
								mergeStyle = "MergeSel";
							else
								mergeStyle = "Merge";
							
							if (GUI.Button(primitiveRect, "", mergeStyle))
							{
								PaintEditName.changeName = false;
								Manager.InspectorPrimitive = e;
								Manager.CurrentLane = lane;
								if (flowMode && !determinedSource)
								{
									clickSource = e;
									determinedSource = true;
								}
								else if (flowMode && determinedSource)
								{
									clickTarget = e;
									determinedFlow = true;
									determinedSource = false;
								}
							}
						}
						
						if (UnityEngine.Event.current.type == EventType.KeyUp &&
							UnityEngine.Event.current.keyCode == KeyCode.Delete &&
							prim.PrID == Manager.InspectorPrimitive.PrID && EnableProcessEdition)
						{
							if ((prim as Event).categ.Equals(Event.Categ.Start))
								Manager.Notifications.Items.Add(
									new Notification(Notification.Type.Exception, "You cannot remove a start event."));
							else if ((prim as Event).categ.Equals(Event.Categ.End))
								Manager.Notifications.Items.Add(
									new Notification(Notification.Type.Exception, "You cannot remove an end event."));
							else if ((prim as Event).categ.Equals(Event.Categ.Merge))
								LanguageConstructor.RemovePrimitive(Manager.CurrentProcess.PID,
									Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
									Manager.CurrentLane.PrID,
									prim.PrID);
						}
					}
					// ACTIVITY
					// Composed Activity
					else if (prim is ComposedActivity)
					{
						ComposedActivity a = prim as ComposedActivity;
						string compActStyle = "";
						
						if (prim.PrID == Manager.InspectorPrimitive.PrID ||
							(clickSource != null && prim.PrID == clickSource.PrID) ||
							(clickTarget != null && prim.PrID == clickTarget.PrID))
							compActStyle = "ComposedActivitySel";
						else
							compActStyle = "ComposedActivity";
						
						// Open composed activity
						if (Input.GetKeyUp(KeyCode.Space) &&
							prim.PrID == Manager.InspectorPrimitive.PrID)
						{
							Manager.InspectorPrimitive = prim;
							if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation)
								Manager.CurrentScreen = GameManager.GameScreen.ComposedCreation;
							else if (Manager.CurrentScreen == GameManager.GameScreen.ViewProcess)
								Manager.CurrentScreen = GameManager.GameScreen.ViewComposed;
							else
								Manager.CurrentScreen = GameManager.GameScreen.VersionComposedCreation;
							ResetToolbarView();
						}
						
						// Select composed activity
						else if (GUI.Button(primitiveRect, a.Name, compActStyle))
						{
							PaintEditName.changeName = false;
							Manager.InspectorPrimitive = a;
							Manager.CurrentLane = lane;
							activityName = a.Name;
							if (flowMode && !determinedSource)
							{
								clickSource = a;
								clickTarget = null;
								determinedSource = true;
							}
							else if (flowMode && determinedSource)
							{
								clickTarget = a;
								determinedFlow = true;
								determinedSource = false;
							}
						}
						
						// Change composed activity name
						if (primitiveRect.Contains(UnityEngine.Event.current.mousePosition) &&
							(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
							Input.GetMouseButtonUp(0) && EnableProcessEdition)
						{
							Manager.InspectorPrimitive = prim;
							activityName = (prim as Activity).Name;
							PaintEditName.changeName = true;
							PaintEditName.primitiveRect = primitiveRect;
						}
						
						// Delete composed activity
						if (prim.PrID == Manager.InspectorPrimitive.PrID &&
							UnityEngine.Event.current.type == EventType.KeyUp &&
							UnityEngine.Event.current.keyCode == KeyCode.Delete && EnableProcessEdition)
						{
							LanguageConstructor.RemovePrimitive(Manager.CurrentProcess.PID,
								Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
								Manager.CurrentLane.PrID,
								a.PrID);
						}
					}
					// Ad-Hoc Activity
					else if (prim is AdHocActivity)
					{
						AdHocActivity a = prim as AdHocActivity;
						string adHocActStyle = "";
						
						if (prim.PrID == Manager.InspectorPrimitive.PrID ||
							(clickSource != null && prim.PrID == clickSource.PrID) ||
							(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
							adHocActStyle = "AdHocActivitySel";
						else
							adHocActStyle = "AdHocActivity";
						
						// Open ad-hoc activity
						if (Input.GetKeyUp(KeyCode.Space) &&
							prim.PrID == Manager.InspectorPrimitive.PrID)
						{
							Manager.InspectorPrimitive = prim;
							if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation)
								Manager.CurrentScreen = GameManager.GameScreen.AdHocCreation;
							else if (Manager.CurrentScreen == GameManager.GameScreen.ViewProcess)
								Manager.CurrentScreen = GameManager.GameScreen.ViewAdHoc;
							else
								Manager.CurrentScreen = GameManager.GameScreen.VersionAdHocCreation;
							ResetToolbarView();
						}
						
						// Select ad-hoc activity
						else if (GUI.Button(primitiveRect, a.Name, adHocActStyle))
						{
							PaintEditName.changeName = false;
							Manager.InspectorPrimitive = a;
							Manager.CurrentLane = lane;
							activityName = a.Name;
							if (flowMode && !determinedSource)
							{
								clickSource = a;
								clickTarget = null;
								determinedSource = true;
							}
							else if (flowMode && determinedSource)
							{
								clickTarget = a;
								determinedFlow = true;
								determinedSource = false;
							}
						}
						
						// Change ad-hoc activity name
						if (primitiveRect.Contains(UnityEngine.Event.current.mousePosition) &&
							(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
							Input.GetMouseButtonUp(0) && EnableProcessEdition)
						{
							Manager.InspectorPrimitive = prim;
							activityName = (prim as Activity).Name;
							PaintEditName.changeName = true;
							PaintEditName.primitiveRect = primitiveRect;
						}
						
						// Delete ad-hoc activity
						if (prim.PrID == Manager.InspectorPrimitive.PrID &&
							UnityEngine.Event.current.type == EventType.KeyUp &&
							UnityEngine.Event.current.keyCode == KeyCode.Delete && EnableProcessEdition)
						{
							LanguageConstructor.RemovePrimitive(Manager.CurrentProcess.PID,
								Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
								Manager.CurrentLane.PrID,
								a.PrID);
						}
					}
					// Work Activity
					else if (prim is Activity)
					{
						Activity a = prim as Activity;
						string actStyle = "";
						
						if (prim.PrID == Manager.InspectorPrimitive.PrID ||
							(clickSource != null && prim.PrID == clickSource.PrID) || 
							(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
							actStyle = "ActivitySel";
						else
							actStyle = "Activity";
						
						// Select activity
						if (GUI.Button(primitiveRect, a.Name, actStyle))
						{
							PaintEditName.changeName = false;
							Manager.InspectorPrimitive = a;
							Manager.CurrentLane = lane;
							activityName = a.Name;
							description = a.Description;
							if (flowMode && !determinedSource)
							{
								clickSource = a;
								clickTarget = null;
								determinedSource = true;
							}
							else if (flowMode && determinedSource)
							{
								clickTarget = a;
								determinedFlow = true;
								determinedSource = false;
							}
						}
						
						// Change activity name
						if (primitiveRect.Contains(UnityEngine.Event.current.mousePosition) &&
							(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
							Input.GetMouseButtonUp(0) && EnableProcessEdition)
						{
							Manager.InspectorPrimitive = prim;
							activityName = (prim as Activity).Name;
							PaintEditName.changeName = true;
							PaintEditName.primitiveRect = primitiveRect;
						}
						
						// Delete activity
						if (prim.PrID == Manager.InspectorPrimitive.PrID &&
							UnityEngine.Event.current.type == EventType.KeyUp &&
							UnityEngine.Event.current.keyCode == KeyCode.Delete && EnableProcessEdition)
						{
							LanguageConstructor.RemovePrimitive(Manager.CurrentProcess.PID,
								Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
								Manager.CurrentLane.PrID,
								a.PrID);
						}
					}
				}
			}
		}
	}
	
	public void DrawComposedSubActivities(ComposedActivity activity)
	{
		// DRAWS LANE
		Matrix4x4 backupMatrix = GUI.matrix;
		Rect poolRect = new Rect(0, 15, Screen.width-40, 300);
		GUI.Box(poolRect, "", "Lane");
		
		GUIUtility.RotateAroundPivot(270f, new Vector2(0, 300+15));
		GUI.Button(new Rect(0, 300+15, 300, 30), "Activity " + activity.PrID + ": " + activity.Name, "PoolLabel");			
		GUI.matrix = backupMatrix;
		
		// DRAWS EDGE FLOWS
		foreach (Flow flow in activity.Connections)
		{
			Primitive source = Manager.GameState.GetSubPrimitive(Manager.CurrentProcess.PID,
				(Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
				Manager.CurrentScreen == GameManager.GameScreen.ViewComposed) ? -1 : Manager.CurrentVersion.PVID,
				Manager.CurrentLane.PrID,
				Manager.InspectorPrimitive.PrID, flow.SourceID);
			Primitive target = Manager.GameState.GetSubPrimitive(Manager.CurrentProcess.PID,
				(Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
				Manager.CurrentScreen == GameManager.GameScreen.ViewComposed) ? -1 : Manager.CurrentVersion.PVID,
				Manager.CurrentLane.PrID,
				Manager.InspectorPrimitive.PrID, flow.TargetID);
			
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
		
		// DRAWS LANGUAGE PRIMITIVES
		foreach (Primitive prim in activity.lane.Elements)
		{
			Rect primitiveRect = new Rect(prim.x-75, prim.y-25, 150, 50);
			
			// Drop
			if (prim.dragging && Input.GetMouseButtonUp(1) && EnableProcessEdition)
			{
				prim.dragging = false;
				
				if (!poolRect.Contains(new Vector2(prim.x, prim.y)))
				{
					prim.x = oldPrimitivePos.x;
					prim.y = oldPrimitivePos.y;
				}
				else
					Mechanics.RepositionSubPrimitive(Manager.CurrentProcess.PID,
						Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ? -1 : Manager.CurrentVersion.PVID,
						activity.PrID, prim.PrID, prim.x, prim.y);
			}
			// Drag
			else if (primitiveRect.Contains(UnityEngine.Event.current.mousePosition) &&
				Input.GetMouseButtonDown(1) && !flowMode && EnableProcessEdition)
			{
				oldPrimitivePos = new Vector2(prim.x, prim.y);
				prim.dragging = true;
			}
			// Moving position
			if (prim.dragging && EnableProcessEdition)
			{
				Vector3 dropPosition = Input.mousePosition;
				prim.x = dropPosition.x-15;
				prim.y = Screen.height-dropPosition.y-80;
			}
			
			string primStyle = "";
			
			if (prim is Activity)
			{
				Activity a = prim as Activity;
				
				if (prim.PrID == Manager.InspectorSubPrimitive.PrID ||
					(clickSource != null && prim.PrID == clickSource.PrID) || 
					(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
					primStyle = "ActivitySel";
				else
					primStyle = "Activity";
				
				if (GUI.Button(primitiveRect, a.Name, primStyle))
				{
					PaintEditName.changeCondition = PaintEditName.changeName = PaintEditName.changeSubName = false;
					Manager.InspectorSubPrimitive = a;
					subPrimitiveName = a.Name;
					
					if (flowMode && !determinedSource)
					{
						clickSource = a;
						determinedSource = true;
					}
					else if (flowMode && determinedSource)
					{
						clickTarget = a;
						determinedFlow = true;
						determinedSource = false;
					}
				}
				
				if (primitiveRect.Contains(UnityEngine.Event.current.mousePosition) &&
					(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
					Input.GetMouseButtonUp(0) && EnableProcessEdition)
				{
					if (Manager.InspectorSubPrimitive is Activity)
					{
						PaintEditName.changeSubName = false;
						Manager.InspectorSubPrimitive = a;
						Activity subActivity = Manager.InspectorSubPrimitive as Activity;
						subPrimitiveName = subActivity.Name;
						PaintEditName.changeSubName = true;
						PaintEditName.primitiveRect = primitiveRect;
					}
				}
				
				if (UnityEngine.Event.current.type == EventType.KeyUp &&
					UnityEngine.Event.current.keyCode == KeyCode.Delete &&
					prim.PrID == Manager.InspectorSubPrimitive.PrID && EnableProcessEdition)
				{
					LanguageConstructor.RemoveSubPrimitive(Manager.CurrentProcess.PID,
						Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ? -1 : Manager.CurrentVersion.PVID,
						Manager.CurrentLane.PrID,
						Manager.InspectorPrimitive.PrID, Manager.InspectorSubPrimitive.PrID);
				}
			}
			else if (prim is Event)
			{
				Event e = prim as Event;
				
				if (e.categ.Equals(Event.Categ.Start))
				{
					if (prim.PrID == Manager.InspectorSubPrimitive.PrID ||
						(clickSource != null && prim.PrID == clickSource.PrID) || 
						(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
						primStyle = "StartSel";
					else
						primStyle = "StartEvent";
				}
				else if (e.categ.Equals(Event.Categ.End))
				{
					if (prim.PrID == Manager.InspectorSubPrimitive.PrID ||
						(clickSource != null && prim.PrID == clickSource.PrID) || 
						(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
						primStyle = "EndSel";
					else
						primStyle = "EndEvent";
				}
				else if (e.categ.Equals(Event.Categ.Merge))
				{
					if (prim.PrID == Manager.InspectorSubPrimitive.PrID ||
						(clickSource != null && prim.PrID == clickSource.PrID) || 
						(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
						primStyle = "MergeSel";
					else
						primStyle = "Merge";
				}
				
				if (GUI.Button(new Rect(e.x-20, e.y-20, 40, 40), "", primStyle))
				{
					Manager.InspectorSubPrimitive = e;
					
					if (flowMode && !determinedSource)
					{
						clickSource = e;
						determinedSource = true;
					}
					else if (flowMode && determinedSource)
					{
						clickTarget = e;
						determinedFlow = true;
						determinedSource = false;
					}
				}
				
				if (UnityEngine.Event.current.type == EventType.KeyUp &&
					UnityEngine.Event.current.keyCode == KeyCode.Delete &&
					prim.PrID == Manager.InspectorSubPrimitive.PrID && EnableProcessEdition)
				{
					if (e.categ.Equals(Event.Categ.Merge))
						LanguageConstructor.RemoveSubPrimitive(Manager.CurrentProcess.PID,
							Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
							Manager.CurrentLane.PrID,
							Manager.InspectorPrimitive.PrID, Manager.InspectorSubPrimitive.PrID);
					else if (e.categ.Equals(Event.Categ.Start))
						Manager.Notifications.Items.Add(
							new Notification(Notification.Type.Exception, "You cannot delete a start event."));
					else if (e.categ.Equals(Event.Categ.End))
						Manager.Notifications.Items.Add(
							new Notification(Notification.Type.Exception, "You cannot delete an end event."));
				}
			}
		}
	}
	
	public void DrawAdHocSubActivities(AdHocActivity activity)
	{
		Matrix4x4 backupMatrix = GUI.matrix;
		Rect poolRect = new Rect(SCREEN_WIDTH/2-720/2, 15, 720, 500);
		GUI.Box(new Rect(poolRect), "", "Lane");
		
		GUIUtility.RotateAroundPivot(270f, new Vector2(Screen.width/2-720/2, 500+15));
		if (GUI.Button(new Rect(Screen.width/2-720/2, 500+15, 500, 30),
			"Activity " + activity.PrID + ": " + activity.Name, "PoolLabel")) { }
		GUI.matrix = backupMatrix;
		
		foreach (Primitive prim in activity.lane.Elements)
		{
			Activity a = prim as Activity;
			Rect primitiveRect = new Rect(prim.x-75, prim.y-25, 150, 50);
			string actStyle = "";
			
			if (prim.PrID == Manager.InspectorSubPrimitive.PrID ||
				(clickSource != null && prim.PrID == clickSource.PrID) || 
				(clickTarget != null && prim.PrID == clickTarget.PrID) && EnableProcessEdition)
				actStyle = "ActivitySel";
			else
				actStyle = "Activity";
			
			if (GUI.Button(primitiveRect, a.Name, actStyle))
			{
				PaintEditName.changeCondition = PaintEditName.changeName = PaintEditName.changeSubName = false;
				Manager.InspectorSubPrimitive = a;
				subPrimitiveName = a.Name;
			}
			
			if (primitiveRect.Contains(UnityEngine.Event.current.mousePosition) &&
				(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
				Input.GetMouseButtonUp(0) && EnableProcessEdition)
			{
				Activity subActivity = Manager.InspectorSubPrimitive as Activity;
				subPrimitiveName = subActivity.Name;
				PaintEditName.primitiveRect = primitiveRect;
				PaintEditName.changeSubName = true;
			}
			
			if (UnityEngine.Event.current.type == EventType.KeyUp &&
				UnityEngine.Event.current.keyCode == KeyCode.Delete &&
				prim.PrID == Manager.InspectorSubPrimitive.PrID && EnableProcessEdition)
			{
				LanguageConstructor.RemoveSubPrimitive(Manager.CurrentProcess.PID,
					Manager.CurrentScreen == GameManager.GameScreen.AdHocCreation ? -1 : Manager.CurrentVersion.PVID,
					Manager.CurrentLane.PrID,
					Manager.InspectorPrimitive.PrID, Manager.InspectorSubPrimitive.PrID);
			}
		}
	}
	
	public void DrawLine(Flow flow, Color color, float width, bool anticipateArrow, bool retardArrow, int APrID, int PrID)
	{
		Primitive source, target;
		if (APrID == -1)
		{
			if (Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
			{
				source = Manager.GameState.GetPrimitive(Manager.CurrentProcess.PID, Manager.CurrentVersion.PVID, flow.SourceID);
				target = Manager.GameState.GetPrimitive(Manager.CurrentProcess.PID, Manager.CurrentVersion.PVID, flow.TargetID);
			}
			else if (Manager.CurrentProcess != null)
			{
				source = Manager.GameState.GetPrimitive(Manager.CurrentProcess.PID, -1, flow.SourceID);
				target = Manager.GameState.GetPrimitive(Manager.CurrentProcess.PID, -1, flow.TargetID);
			}
			else
				source = target = null;
		}
		else
		{
			if (Manager.CurrentProcess != null && Manager.CurrentLane != null)
			{
				source = Manager.GameState.GetSubPrimitive(Manager.CurrentProcess.PID,
						(Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
						Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
						Manager.CurrentScreen == GameManager.GameScreen.ViewComposed) ? -1 : Manager.CurrentVersion.PVID,
						Manager.CurrentLane.PrID, PrID, flow.SourceID);
				target = Manager.GameState.GetSubPrimitive(Manager.CurrentProcess.PID,
						(Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
						Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
						Manager.CurrentScreen == GameManager.GameScreen.ViewComposed) ? -1 : Manager.CurrentVersion.PVID,
						Manager.CurrentLane.PrID, PrID, flow.TargetID);
			}
			else
				source = target = null;
		}
		
		Vector2 pointA, pointB;
		if (source == null || target == null)
		{
			pointA = new Vector2(0, 0);
			pointB = new Vector2(0, 0);
		}
		else
		{
			pointA = new Vector2(source.x, source.y);
			pointB = new Vector2(target.x, target.y);
		}
		
		
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
			if ((Manager.InspectorPrimitive != null && flow.PrID == Manager.InspectorPrimitive.PrID) ||
				(Manager.InspectorSubPrimitive != null && flow.PrID == Manager.InspectorSubPrimitive.PrID))
				GUI.DrawTexture(lineRect, lineTexSel);
			else
			{
				if (flow.categ.Equals(Flow.Categ.Sequence))
					GUI.DrawTexture(lineRect, lineTex);
				else
					GUI.DrawTexture(lineRect, lineInfoTex);
			}
			if (lineRect.Contains(UnityEngine.Event.current.mousePosition) && Input.GetMouseButtonUp(0))
			{
				PaintEditName.changeCondition = PaintEditName.changeName = PaintEditName.changeSubName = false;
				if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
					Manager.InspectorPrimitive = flow;
				else if (Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
					Manager.InspectorSubPrimitive = flow;
				condition = flow.Condition;
			}
			if (lineRect.Contains(UnityEngine.Event.current.mousePosition) && Input.GetMouseButtonUp(0) &&
				(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && EnableProcessEdition)
			{
				PaintEditName.changeCondition = PaintEditName.changeName = PaintEditName.changeSubName = false;
				PaintEditName.changeCondition = true;
			}
			
			Rect arrowRect;
			if (anticipateArrow)
				arrowRect = new Rect(pointA.x+length/4, pointA.y+width/4-6, 9, 13);
			else if (retardArrow)
				arrowRect = new Rect(pointA.x + 2*length/3, pointA.y + 3*width/4-7, 9, 13);
			else
				arrowRect = new Rect(pointA.x+length/2, pointA.y+width/2-6, 9, 13);
			
			if ((Manager.InspectorPrimitive != null && flow.PrID == Manager.InspectorPrimitive.PrID) ||
				(Manager.InspectorSubPrimitive != null && flow.PrID == Manager.InspectorSubPrimitive.PrID))
				GUI.DrawTexture(arrowRect, ArrowHeadSel);
			else
			{
				if (flow.categ.Equals(Flow.Categ.Sequence))
					GUI.DrawTexture(arrowRect, ArrowHead);
				else
					GUI.DrawTexture(arrowRect, ArrowInfoHead);
			}
			
			Rect condRect = new Rect(arrowRect.x-50, arrowRect.y-15, 100, 15);
			GUI.Label(condRect, flow.Condition, "FlowCondLabel");
			
			if (arrowRect.Contains(UnityEngine.Event.current.mousePosition) && Input.GetMouseButtonUp(0))
			{
				PaintEditName.changeCondition = PaintEditName.changeName = PaintEditName.changeSubName = false;
				if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
					Manager.InspectorPrimitive = flow;
				else if (Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
					Manager.InspectorSubPrimitive = flow;
				
				condition = flow.Condition;
			}
			
			if (arrowRect.Contains(UnityEngine.Event.current.mousePosition) && Input.GetMouseButtonUp(0) &&
				(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && EnableProcessEdition)
			{
				PaintEditName.changeCondition = PaintEditName.changeName = PaintEditName.changeSubName = false;
				PaintEditName.changeCondition = true;
			}
			
			if (PaintEditName.changeCondition && EnableProcessEdition &&
				((Manager.InspectorPrimitive != null && flow.PrID == Manager.InspectorPrimitive.PrID) ||
				(Manager.InspectorSubPrimitive != null && flow.PrID == Manager.InspectorSubPrimitive.PrID)))
				condition = GUI.TextField(new Rect(arrowRect.x-45, arrowRect.y-25, 90, 25), condition);
		}
		else
		{
			Rect loopRect = new Rect(source.x-70, source.y-50, 137, 30);
			if ((Manager.InspectorPrimitive != null && flow.PrID == Manager.InspectorPrimitive.PrID) ||
				(Manager.InspectorSubPrimitive != null && flow.PrID == Manager.InspectorSubPrimitive.PrID))
				GUI.DrawTexture(loopRect, LoopArrowSel);
			else
				GUI.DrawTexture(loopRect, LoopArrow);
			if (loopRect.Contains(UnityEngine.Event.current.mousePosition) && Input.GetMouseButtonUp(0))
			{
				PaintEditName.changeCondition = PaintEditName.changeName = PaintEditName.changeSubName = false;
				if (Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.ProcessVersionCreate)
					Manager.InspectorPrimitive = flow;
				else if (Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation ||
					Manager.CurrentScreen == GameManager.GameScreen.VersionComposedCreation)
					Manager.InspectorSubPrimitive = flow;
				
				condition = flow.Condition;
			}
			
			GUI.Label(new Rect(loopRect.x+20, loopRect.y-15, 100, 15),flow.Condition, "FlowCondLabel");
			
			if (loopRect.Contains(UnityEngine.Event.current.mousePosition) && Input.GetMouseButtonUp(0) &&
				(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && EnableProcessEdition)
			{
				PaintEditName.changeCondition = PaintEditName.changeName = PaintEditName.changeSubName = false;
				PaintEditName.changeCondition = true;
			}
			
			if (PaintEditName.changeCondition && EnableProcessEdition &&
				((Manager.InspectorPrimitive != null && flow.PrID == Manager.InspectorPrimitive.PrID) ||
				(Manager.InspectorSubPrimitive != null && flow.PrID == Manager.InspectorSubPrimitive.PrID)))
				condition = GUI.TextField(new Rect(loopRect.x+25, loopRect.y-25, 90, 25), condition);
		}
		
		// Delete flow
		if (APrID == -1 && Manager.InspectorPrimitive != null && flow.PrID == Manager.InspectorPrimitive.PrID &&
			UnityEngine.Event.current.type == EventType.KeyUp &&
			UnityEngine.Event.current.keyCode == KeyCode.Delete && EnableProcessEdition)
			
			LanguageConstructor.RemoveFlow(Manager.CurrentProcess.PID,
				Manager.CurrentScreen == GameManager.GameScreen.ProcessCreation ? -1 : Manager.CurrentVersion.PVID,
				flow.PrID);
		
		else if (Manager.InspectorSubPrimitive != null && flow.PrID == Manager.InspectorSubPrimitive.PrID &&
			UnityEngine.Event.current.type == EventType.KeyUp &&
			UnityEngine.Event.current.keyCode == KeyCode.Delete && EnableProcessEdition)
			
			LanguageConstructor.RemoveSubFlow(Manager.CurrentProcess.PID,
				(Manager.CurrentScreen == GameManager.GameScreen.AdHocCreation ||
				Manager.CurrentScreen == GameManager.GameScreen.ComposedCreation) ? -1 : Manager.CurrentVersion.PVID,
				Manager.CurrentLane.PrID,
				PrID, flow.PrID);
		
		
		GUI.matrix = matrixBackup;
    }
	
}
