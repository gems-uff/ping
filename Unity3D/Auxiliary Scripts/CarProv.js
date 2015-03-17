#pragma strict
public var playerName : String;
public var minimumTurn : float;
public var maximumTurn : float;
public var topSpeed : float;
public var prov : ExtractProvenance = null;

private var throttle : float = 0;
private var currentGear : int = 0;
private var currentEnginePower : float = 0;

private var oldEnginePowerHand : float = 0;
private var oldEnginePowerBrake : float = 0;
private var oldSpeed : float = 0;
private var deltaSpeed : float = 0;
private var oldTime : float = 0;
private var deltaTime : float = 0;
private var currentSpeed : float = 0;
private var currentTurn : float = 0;
private var deltaTurn : float = 0;
private var oldTurn : float = 0;
private var dragVector : Vector3 = new Vector3(0,0,0);
private var velocityVector : Vector3 = new Vector3(0,0,0);
private var angularVelocity : Vector3 = new Vector3(0,0,0);
private var control : boolean = true;
private var handControl : boolean = true;
private var oldFlyTime : float = 0;
private var isFlying : boolean = false;
private var countInf : int = 1;
private var lastFwd : Vector3;
private var curAngleX : float = 0;
private var curFwd : Vector3;

function Awake()
{
	var provObj : GameObject = GameObject.Find("Provenance");
	prov = GetComponent(ExtractProvenance); 
	
	if(prov == null)
	{
		prov = GetComponentInParent(ExtractProvenance); 
	}
	
	prov.influenceContainer = provObj.GetComponent(InfluenceController); 
	prov.provenance = provObj.GetComponent(ProvenanceController); 
	
	Prov_Player(playerName);
	
	//InvokeRepeating("Prov_Driving", 2, 2);
	
	lastFwd = transform.forward;
}

function Update()
{
	Check_If_Car_Lost_Control();
}
// Necessary to update these values before making any Provenance Call	
function UpdateAttributes(throttle_ : float, currentGear_ : int, currentEnginePower_ : float)
{
	throttle = throttle_;
	currentGear = currentGear_;
	currentEnginePower = currentEnginePower_;
}

	
function Prov_GetPlayerAttributes()
{
	currentSpeed = rigidbody.velocity.magnitude;
	currentTurn = EvaluateSpeedToTurn(currentSpeed);
	deltaTime = Time.time - oldTime;
	deltaSpeed = currentSpeed - oldSpeed;
	velocityVector = rigidbody.velocity;
	
	//dragVector = rigidbody.angularDrag;
	angularVelocity = rigidbody.angularVelocity;
	velocityVector.Normalize();
	angularVelocity.Normalize();
	dragVector.Normalize();
	
	prov.AddAttribute("Throttle", throttle.ToString());
	prov.AddAttribute("Speed", currentSpeed.ToString());
	prov.AddAttribute("CurrentGear", currentGear.ToString());
	prov.AddAttribute("CurrentEnginePower", currentEnginePower.ToString());
	prov.AddAttribute("TurnRate", currentTurn.ToString());
	//prov.AddAttribute("Velocity_X", rigidbody.velocity.x.ToString());
	//prov.AddAttribute("Velocity_Y", rigidbody.velocity.y.ToString());
	//prov.AddAttribute("Velocity_Z", rigidbody.velocity.z.ToString());
	//prov.AddAttribute("AngularVelocity_X", rigidbody.angularVelocity.x.ToString());
	//prov.AddAttribute("AngularVelocity_Y", rigidbody.angularVelocity.y.ToString());
	//prov.AddAttribute("AngularVelocity_Z", rigidbody.angularVelocity.z.ToString());
	//prov.AddAttribute("AngularDrag", rigidbody.angularDrag.ToString());
	prov.AddAttribute("CarMass", rigidbody.mass.ToString());
	prov.AddAttribute("DeltaTime", deltaTime.ToString());
	prov.AddAttribute("VelocityVector", velocityVector.ToString());
	prov.AddAttribute("AngularVelocity", angularVelocity.ToString());
	prov.AddAttribute("DragVector", dragVector.ToString());
	Influences();
	
	oldSpeed = currentSpeed;
	oldTime = Time.time;
	
	deltaTurn = currentTurn  - oldTurn;
	oldTurn = currentTurn;
}

function Prov_Player(pName : String)
{
	prov.NewAgentVertex(pName);
}

function Prov_Driving()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Driving");
	prov.HasInfluence("Player");
	lastFwd = curFwd; // and update lastFwd
	
	if(currentTurn < (minimumTurn + ((maximumTurn - minimumTurn) * 0.4)))
	{
		Debug.Log ("Too FAST!!!");
		ProvInfluence("Crash", "TurnRate (Crash)", countInf.ToString(), (currentTurn - maximumTurn), Time.time + 2);
	}
	//Debug.Log ("Driving");
}

function Prov_Braking(brake : boolean)
{
	if(brake && control)
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("Brake");
		prov.HasInfluence("Player");
		//DragInfluence(dragMultiplier.x);
		control = false;
		
		ProvInfluence("LostControl", "LostControl (Crash)", countInf.ToString(), 0, Time.time + 2);
		
		//Debug.Log ("Brake");
	}
	else
	{
		if(!brake)
		{
			control = true;
		}
	}
}

function Prov_HandBrake()
{
	if(handControl)
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("HandBrake");
		prov.HasInfluence("Player");
		handControl = false;
		
		ProvInfluence("LostControl", "LostControl (Crash)", countInf.ToString(), 0, Time.time + 2);
		
		//DragInfluence(dragMultiplier.x);
		//Debug.Log ("HandBrake");
	}
}

function Prov_ReleaseHandBrake()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("ReleasedHandBrake");
	prov.HasInfluence("Player");

	//Debug.Log ("Release HandBrake");
	handControl = true;
}

function Prov_Crash()
{
	if(deltaSpeed < -5)
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("Crash");
		prov.HasInfluence("Player");
		prov.HasInfluence("Crash");
		ProvInfluence("Flip", "Flip (Crash)", countInf.ToString(), 0, Time.time + 2);
		ProvInfluence("LostControl", "LostControl (Crash)", countInf.ToString(), 0, Time.time + 2);
		ProvInfluence("Crash", "Crash", countInf.ToString(), 0, Time.time + 6);
		Debug.Log ("Crash");
		
		lastFwd = curFwd;
	}
	else if (deltaSpeed < -1)
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("Scraped");
		prov.HasInfluence("Player");
		ProvInfluence("Crash", "Bumbing (Crash)", countInf.ToString(), 0, Time.time + 6);
		Debug.Log ("Scraping");
	}
	
	if((isFlying)&&(deltaSpeed < -1))
	{
		prov.HasInfluence("Crash");
		prov.HasInfluence("Player");
		Debug.Log ("FlyCrash");
	}
}

function Prov_Flip()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Flipped");
	prov.HasInfluence("Player");
	prov.HasInfluence("Flip");
	//Debug.Log ("Flipped");
}

function Prov_ChangeGear()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("ChangedGear");
	prov.HasInfluence("Player");
	//Debug.Log ("Gear Changed");
}

function Prov_Flying()
{
	if((!isFlying) && (Time.time - oldFlyTime > 2))
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("Flying");
		prov.HasInfluence("Player");
		FlyInfluence();
		oldFlyTime = Time.time;
		Debug.Log ("Flying");
		isFlying = true;
	}
}

function Prov_Landing()
{
	if(isFlying)
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("Landing");
		prov.HasInfluence("Player");
		prov.HasInfluence("Landing");
		isFlying = false;
		Debug.Log ("Landing");
	}
}

function Prov_LostControl()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("LostControl");
	prov.HasInfluence("LostControl");
	prov.HasInfluence("Player");
}


function DragInfluence(value : float)
{
	ProvInfluence("Player", "Drag", countInf.ToString(), value);
}

function FlyInfluence()
{
	ProvInfluence("Crash", "Flying  (Crash)", countInf.ToString(), 0, Time.time + 2);
	ProvInfluence("Landing", "Landing", countInf.ToString(), 0);
	ProvInfluence("LostControl", "LostControl  (Crash)", countInf.ToString(), 0, Time.time + 2);
}

function Influences()
{
	ProvInfluence("Player", "TurnRate", countInf.ToString(), deltaTurn);
	ProvInfluence("Player", "Time", countInf.ToString(), deltaTime);
	ProvInfluence("Player", "Speed", countInf.ToString(), currentSpeed - oldSpeed);
}

function ProvInfluence(type : String, infType : String, infID : String, value : float)
{
	prov.GenerateInfluenceC(type, infID, infType, value.ToString(), 1);
	countInf++;
}

function ProvInfluence(type : String, infType : String, infID : String, value : float, expirationTime : float)
{
	prov.GenerateInfluenceCE(type, infID, infType, value.ToString(), 1, expirationTime);
	countInf++;
}

function ProvMissebleInfluence(type : String, infType : String, infID : String, value : float)
{
	prov.GenerateInfluenceMC("Player", infID, infType, value.ToString(), 1, this.gameObject);
}

//==========================================================
// Utilities
//==========================================================

private var ResetTimeR : float = 3;
private var oldAngleTime : float = 0.0;
function EvaluateSpeedToTurn(speed : float)
{
	if(speed > topSpeed / 2)
		return minimumTurn;
	
	var speedIndex : float = 1 - (speed / (topSpeed / 2));
	return minimumTurn + speedIndex * (maximumTurn - minimumTurn);
}

function Check_If_Car_Lost_Control(){
	curFwd = transform.forward;
	// measure the angle rotated since last frame:
	var ang = Vector3.Angle(curFwd, lastFwd);
	if (ang > 0.01)
	{ 	// if rotated a significant angle...
		// fix angle sign...
		if (Vector3.Cross(curFwd, lastFwd).x < 0) 
			ang = -ang;
		curAngleX = ang; // accumulate in curAngleX...
		if(Mathf.Abs(curAngleX) > 150)
		{
			if(Time.time - oldAngleTime > 5)
			{
				oldAngleTime = Time.time;
				Prov_LostControl();
				Debug.Log("Lost Control");
			}
		}
	}
 }