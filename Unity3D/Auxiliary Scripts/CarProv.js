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
private var lap : int = 0;
private var dragVector : Vector3 = new Vector3(0,0,0);
private var velocityVector : Vector3 = new Vector3(0,0,0);
private var angularVelocity : Vector3 = new Vector3(0,0,0);

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
	
	InvokeRepeating("Prov_Driving", 2, 2);
}

//==========================================================
// Provenance
//==========================================================

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
	
	angularVelocity = rigidbody.angularVelocity;
	velocityVector.Normalize();
	angularVelocity.Normalize();
	dragVector.Normalize();
	
	prov.AddAttribute("Throttle", throttle.ToString());
	prov.AddAttribute("Speed", currentSpeed.ToString());
	prov.AddAttribute("CurrentGear", currentGear.ToString());
	prov.AddAttribute("CurrentEnginePower", currentEnginePower.ToString());
	prov.AddAttribute("TurnRate", currentTurn.ToString());
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
}

var control : boolean = true;

function Prov_Braking(brake : boolean)
{
	if(brake && control)
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("Brake");
		prov.HasInfluence("Player");
		control = false;
	}
	else
	{
		if(!brake)
		{
			control = true;
		}
	}
}

var handControl : boolean = true;

function Prov_HandBrake()
{
	if(handControl)
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("HandBrake");
		prov.HasInfluence("Player");
		handControl = false;
	}
}

function Prov_ReleaseHandBrake()
{
	//Influences();
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("ReleasedHandBrake");
	prov.HasInfluence("Player");

	handControl = true;
}

function Prov_Crash()
{
	if(deltaSpeed < -1)
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("Crash");
		prov.HasInfluence("Player");
		if((isFlying)||(deltaSpeed < -5))
		{
			prov.HasInfluence("Crash");
		}
	}
}

function Prov_Flip()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("Flipped");
	prov.HasInfluence("Player");
}

function Prov_ChangeGear()
{
	Prov_GetPlayerAttributes();
	prov.NewActivityVertex("ChangedGear");
	prov.HasInfluence("Player");
}

var oldFlyTime : float = 0;
var isFlying : boolean = false;
function Prov_Flying()
{
	if((!isFlying) && (Time.time - oldFlyTime > 2))
	{
		Prov_GetPlayerAttributes();
		prov.NewActivityVertex("Flying");
		prov.HasInfluence("Player");
		FlyInfluence();
		oldFlyTime = Time.time;
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
	}
}

function ProvMissebleInfluence(type : String, infType : String, infID : String, value : float)
{
	prov.GenerateInfluenceMC("Player", infID, infType, value.ToString(), 1, this.gameObject);
}
var countInf : int = 1;

function DragInfluence(value : float)
{
	ProvInfluence("Drag", "Drag", countInf.ToString(), value);
	countInf++;
}


function TurnInfluence()
{
	ProvInfluence("TurnRate", "TurnRate", countInf.ToString(), deltaTurn);
	countInf++;
}
function FlyInfluence()
{
	ProvInfluence2("Flying", "Flying", countInf.ToString(), 0, Time.time + 2);
	countInf++;
	ProvInfluence3("Landing", "Landing", countInf.ToString(), 0);
	countInf++;
}

function Influences()
{
	ProvInfluence("TurnRate", "TurnRate", countInf.ToString(), deltaTurn);
	countInf++;
	ProvInfluence("Time", "Time", countInf.ToString(), deltaTime);
	countInf++;
	ProvInfluence("Speed", "Speed", countInf.ToString(), currentSpeed - oldSpeed);
	countInf++;
}

function ProvInfluence(type : String, infType : String, infID : String, value : float)
{
	prov.GenerateInfluenceC("Player", infID, infType, value.ToString(), 1);
}

function ProvInfluence2(type : String, infType : String, infID : String, value : float, expirationTime : float)
{
	prov.GenerateInfluenceCE("Crash", infID, infType, value.ToString(), 1, expirationTime);
}

function ProvInfluence3(type : String, infType : String, infID : String, value : float)
{
	prov.GenerateInfluenceC("Landing", infID, infType, value.ToString(), 1);
}

//==========================================================
// Utilities
//==========================================================

function EvaluateSpeedToTurn(speed : float)
{
	if(speed > topSpeed / 2)
		return minimumTurn;
	
	var speedIndex : float = 1 - (speed / (topSpeed / 2));
	return minimumTurn + speedIndex * (maximumTurn - minimumTurn);
}


function Prov_Export()
{
	var ProvObj : GameObject = GameObject.Find("Provenance");
	var provController : ProvenanceController = ProvObj.GetComponent(ProvenanceController); 
	provController.Save("Car_Tutorial" + lap);
	lap++;
}
