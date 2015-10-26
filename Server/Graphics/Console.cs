using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Console : MonoBehaviour
{	
	private GUISkin MyStyle;
	public static List<string> Log;
	static Vector2 scrollPos = Vector2.zero;
	
	void Start()
	{
		Log = new List<string>();
		Log.Add("The Organix <b>server started</b>.");
	}
	
	public static void DrawConsole()
	{
		float width = PainterServer.SCREEN_WIDTH*0.35f;
		float height = PainterServer.SCREEN_HEIGHT;
		Rect nextLine = new Rect(15, 0, width-15, 23);
		
		GUI.BeginGroup(new Rect(PainterServer.SCREEN_WIDTH-width, -3, width+5, height+10), "", "box");
		GUI.Label(new Rect(width/2-50, 10, 100, 20), "<size=30>CONSOLE</size>", "GameTitle");
		
		scrollPos = GUI.BeginScrollView(new Rect(15, 60, width-25, height-100), scrollPos,
			new Rect(0, 0, width-20, Log.Count*23),
			GUIStyle.none, new GUIStyle(GUI.skin.verticalScrollbar));
		for (int i = Log.Count-1; i >= 0; i--)
		{
			GUI.Label(nextLine, "<size=14>" + (i+1) + ". " + Log[i] + "</size>", "ConsoleText");
			nextLine.y += 23;
		}
		GUI.EndScrollView();
		
		if (GUI.Button(new Rect(4, height-25-5, 444, 33), "Clear", "ClearButtonServer"))
		{
			string first = Log[0];
			Log.Clear();
			Log.Add(first);
		}
		
		GUI.EndGroup();
	}

}
