#pragma strict
public var agentName : String;
public var prov : ExtractProvenance = null;

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
private var car : Car;

function Awake()
{
	prov = GetComponent(ExtractProvenance); 
	
	if(prov == null){
		prov = GetComponentInParent(ExtractProvenance); 
	}
	
	Prov_Agent(agentName);
	
	//InvokeRepeating("Prov_Driving", 2, 2);
	
	lastFwd = transform.forward;
	
	car = GetComponentInParent(Car); 
}

function Update()
{
	Check_If_Car_Lost_Control();
}
// Necessary to update these values before making any Provenance Call	
function UpdateAttributes(throttle_ : float, currentGear_ : int, currentEnginePower_ : float)
{
	currentGear = currentGear_;
	currentEnginePower = currentEnginePower_;
}

	
private function Prov_GetAgentAttributes()
{
	car = GetComponentInParent(Car); 
	currentSpeed = rigidbody.velocity.magnitude;
	currentTurn = EvaluateSpeedToTurn(currentSpeed);
	deltaTime = Time.time - oldTime;
	deltaSpeed = currentSpeed - oldSpeed;
	velocityVector = rigidbody.velocity;
	angularVelocity = rigidbody.angularVelocity;
	velocityVector.Normalize();
	angularVelocity.Normalize();
	dragVector.Normalize();
	
	prov.AddAttribute("Throttle", car.throttle.ToString());
	prov.AddAttribute("Top Speed", car.topSpeed.ToString());
	prov.AddAttribute("Minimum Turn", car.minimumTurn.ToString());
	prov.AddAttribute("Maximum Turn", car.maximumTurn.ToString());
	prov.AddAttribute("Speed", currentSpeed.ToString());
	prov.AddAttribute("SuspensionSpringFront", car.suspensionSpringFront.ToString());
	prov.AddAttribute("SuspensionSpringRear", car.suspensionSpringRear.ToString());
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

private function Prov_Agent(pName : String)
{
	prov.NewAgentVertex(pName);
}

public function Prov_Driving()
{
	Prov_GetAgentAttributes();
	prov.NewActivityVertex("Driving");
	prov.HasInfluence(agentName);
	lastFwd = curFwd; // and update lastFwd
	
	if(currentTurn < (car.minimumTurn + ((car.maximumTurn - car.minimumTurn) * 0.4)))
	{
		Debug.Log ("Too FAST!!!");
		ProvInfluence("Crash", "TurnRate (Crash)", countInf.ToString(), (currentTurn - car.maximumTurn), Time.time + 2);
	}
	//Debug.Log ("Driving");
}

public function Prov_Braking(brake : boolean)
{
	if(brake && control)
	{
		Prov_GetAgentAttributes();
		prov.NewActivityVertex("Brake");
		prov.HasInfluence(agentName);
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

public function Prov_HandBrake()
{
	if(handControl)
	{
		Prov_GetAgentAttributes();
		prov.NewActivityVertex("HandBrake");
		prov.HasInfluence(agentName);
		handControl = false;
		
		ProvInfluence("LostControl", "LostControl (Crash)", countInf.ToString(), 0, Time.time + 2);
		
		//DragInfluence(dragMultiplier.x);
		//Debug.Log ("HandBrake");
	}
}

public function Prov_ReleaseHandBrake()
{
	Prov_GetAgentAttributes();
	prov.NewActivityVertex("ReleasedHandBrake");
	prov.HasInfluence(agentName);

	//Debug.Log ("Release HandBrake");
	handControl = true;
}

public function Prov_Crash()
{
	if(deltaSpeed < -5)
	{
		Prov_GetAgentAttributes();
		prov.NewActivityVertex("Crash");
		prov.HasInfluence(agentName);
		prov.HasInfluence("Crash");
		ProvInfluence("Flip", "Flip (Crash)", countInf.ToString(), 0, Time.time + 2);
		ProvInfluence("LostControl", "LostControl (Crash)", countInf.ToString(), 0, Time.time + 2);
		ProvInfluence("Crash", "Crash", countInf.ToString(), 0, Time.time + 6);
		Debug.Log ("Crash");
		
		lastFwd = curFwd;
	}
	else if (deltaSpeed < -1)
	{
		Prov_GetAgentAttributes();
		prov.NewActivityVertex("Scraped");
		prov.HasInfluence(agentName);
		ProvInfluence("Crash", "Bumbing (Crash)", countInf.ToString(), 0, Time.time + 6);
		Debug.Log ("Scraping");
	}
	
	if((isFlying)&&(deltaSpeed < -1))
	{
		prov.HasInfluence("Landing Crash");
		prov.HasInfluence(agentName);
		Debug.Log ("FlyCrash");
	}
}

public function Prov_Flip()
{
	Prov_GetAgentAttributes();
	prov.NewActivityVertex("Flipped");
	prov.HasInfluence(agentName);
	prov.HasInfluence("Flip");
}

function Prov_ChangeGear()
{
	Prov_GetAgentAttributes();
	prov.NewActivityVertex("ChangedGear");
	prov.HasInfluence(agentName);
	//Debug.Log ("Gear Changed");
}

public function Prov_Flying()
{
	if((!isFlying) && (Time.time - oldFlyTime > 2))
	{
		Prov_GetAgentAttributes();
		prov.NewActivityVertex("Flying");
		prov.HasInfluence(agentName);
		FlyInfluence();
		oldFlyTime = Time.time;
		Debug.Log ("Flying");
		isFlying = true;
	}
}

public function Prov_Landing()
{
	if(isFlying)
	{
		Prov_GetAgentAttributes();
		prov.NewActivityVertex("Landing");
		prov.HasInfluence(agentName);
		prov.HasInfluence("Landing");
		isFlying = false;
		Debug.Log ("Landing");
	}
}

public function Prov_LostControl()
{
	Prov_GetAgentAttributes();
	prov.NewActivityVertex("LostControl");
	prov.HasInfluence("LostControl");
	prov.HasInfluence(agentName);
}


public function DragInfluence(value : float)
{
	ProvInfluence(agentName, "Drag", countInf.ToString(), value);
}

public function FlyInfluence()
{
	ProvInfluence("Crash", "Flying  (Crash)", countInf.ToString(), 0, Time.time + 2);
	ProvInfluence("Landing", "Landing", countInf.ToString(), 0);
	ProvInfluence("LostControl", "LostControl  (Crash)", countInf.ToString(), 0, Time.time + 2);
}

public function Influences()
{
	ProvInfluence(agentName, "TurnRate", countInf.ToString(), deltaTurn);
	ProvInfluence(agentName, "Time", countInf.ToString(), deltaTime);
	ProvInfluence(agentName, "Speed", countInf.ToString(), currentSpeed - oldSpeed);
}

public function ProvInfluence(type : String, infType : String, infID : String, value : float)
{
	prov.GenerateInfluenceC(type, infID, infType, value.ToString(), 1);
	countInf++;
}

public function ProvInfluence(type : String, infType : String, infID : String, value : float, expirationTime : float)
{
	prov.GenerateInfluenceCE(type, infID, infType, value.ToString(), 1, expirationTime);
	countInf++;
}

public function ProvMissebleInfluence(type : String, infType : String, infID : String, value : float)
{
	prov.GenerateInfluenceMC(agentName, infID, infType, value.ToString(), 1, this.gameObject);
}

//==========================================================
// Utilities
//==========================================================

private var ResetTimeR : float = 3;
private var oldAngleTime : float = 0.0;
private function EvaluateSpeedToTurn(speed : float)
{
	if(speed > car.topSpeed / 2)
		return car.minimumTurn;
	
	var speedIndex : float = 1 - (speed / (car.topSpeed / 2));
	return car.minimumTurn + speedIndex * (car.maximumTurn - car.minimumTurn);
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