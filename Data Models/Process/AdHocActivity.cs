using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class AdHocActivity : Activity
{	
	public Lane lane;

	#region SERVER
	public AdHocActivity() { lane = new Lane(); }
	public AdHocActivity(int PrID, string name) : base(PrID, name)
	{ lane = new Lane(); }
	public AdHocActivity(int PrID, string name, float x, float y) : base(PrID, name, x, y)
	{ lane = new Lane(); }
	#endregion
}

